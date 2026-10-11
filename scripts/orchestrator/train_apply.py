#!/usr/bin/env python3
"""train_apply.py — BRING A TRAIN'S CLUSTERS IN, ONE COMMIT PER CLUSTER, IN THE MANIFEST'S ORDER (kb/Work PB2885;
lander-train-brief step 1).

    python scripts/orchestrator/train_apply.py <train manifest.json> [--trailer "Co-Authored-By: …"]… [--no-audits]
    python scripts/orchestrator/train_apply.py --self-test

Run it in the lander's own worktree, on its train branch, with a clean tree. For each member of the manifest (the JSON
list wf_rolling_wave.js or the land unit writes: cluster, lead, notes, report, branch, base, head, …), in order:
  1. take the branch's change as ONE diff, `git diff --binary <base> <head>` (STATUS.md and the harness's
     settings.local.json excluded; a base that is not an ancestor of the head is replaced by their merge-base, and
     said so);
  2. `git apply -3` it onto the index and the tree;
  3. ASSERT the result: no unmerged path, no conflict-marker line in any path it touched, and NO SILENT LOSS — every
     path no earlier cluster or main touched since the cluster's base is now byte-identical to the branch's head;
  4. commit it, the subject naming the cluster and its PB ids (from the report's title) and the lead note's title,
     ending with a `Train-Cluster: <cluster>` trailer and the caller's --trailer lines; then restamp STATUS.md.
It STOPS at the first cluster that does not apply cleanly, naming the cluster, its branch and the conflicted paths,
and leaves that cluster's merge in the tree for the lander to resolve against the spec and the report (the brief's
rules for JSON and inventory conflicts). The lander commits the resolution with the message file it names and runs
this script again: a cluster whose `Train-Cluster:` trailer is already on the train branch is skipped, so a re-run
resumes. When every cluster is in, it runs the citation and register audits ONCE for the whole train, in parallel
(audit_code_citations.py and audit_doc_citations.py check every spec citation in the tree, which batches the
per-cluster citation re-check; work.py check is the brief's "once, at the end"), and attributes each finding to the
cluster whose diff touched its file.

WHY. Train 1052 (2026-10-10, the land unit's stream 20261010-132245-land.jsonl) brought 15 branches in by hand: 201 tool
calls in 18 minutes for 11 clusters (read the report, git apply, check markers twice, commit, rewrite STATUS.md,
re-check citations) while the host sat near 15 % CPU and no build ran until every cluster was in. All of it but the
conflicts, the review and the judgment of a citation is mechanical; this is that part, in seconds.

Exit 0: every cluster applied (or already in) and the audits passed. 1: STOPPED at a cluster (conflict, marker or
loss), or an audit found something. 2: usage, an unreadable manifest, or a dirty tree.
"""
from __future__ import annotations

import argparse
import concurrent.futures
import contextlib
import io
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
import time

EXCLUDE = (":!.claude/settings.local.json", ":!STATUS.md")
TRAILER = "Train-Cluster"
SUBJECT_MAX = 72
CHUNK = 200          # paths per git call: far below any OS command-line limit
AUDITS = (("citations in code", ["scripts/spec/audit_code_citations.py", "--check"]),
          ("citations in docs", ["scripts/spec/audit_doc_citations.py", "--check"]),
          ("the work register", ["scripts/spec/work.py", "check"]))


class Stop(Exception):
    """A cluster could not be brought in: the message names it, its branch and its paths."""


def git(repo: pathlib.Path, *args: str, check: bool = True, binary: bool = False) -> subprocess.CompletedProcess:
    r = subprocess.run(["git", "-C", str(repo), *args], capture_output=True,
                       **({} if binary else {"text": True, "encoding": "utf-8", "errors": "replace"}))
    if check and r.returncode != 0:
        err = r.stderr if not binary else r.stderr.decode("utf-8", "replace")
        raise RuntimeError(f"git {' '.join(args)} failed ({r.returncode}): {err.strip()}")
    return r


def commit_of(repo: pathlib.Path, rev: str | None) -> str | None:
    if not rev:
        return None
    r = git(repo, "rev-parse", "--verify", "--quiet", f"{rev}^{{commit}}", check=False)
    return r.stdout.strip() if r.returncode == 0 else None


def blob(repo: pathlib.Path, rev: str, path: str) -> bytes | None:
    """`path`'s content at `rev`, or None when it is absent there."""
    r = git(repo, "show", f"{rev}:{path}", check=False, binary=True)
    return r.stdout if r.returncode == 0 else None


def ids(repo: pathlib.Path, rev: str, paths: list[str]) -> dict[str, str]:
    """path → "<mode> <object id>" at `rev` (":" for the index) for those of `paths` present there: one git call per
    CHUNK paths. Never a per-path `git show` (about 70 s on train 1052's 123-path cluster) and never the whole tree
    (`ls-tree -r` of 16,000 entries took 1.7 s under a gate's load, three times per cluster)."""
    out = "".join(git(repo, *(["ls-files", "-s", "-z", "--"] if rev == ":" else ["ls-tree", "-z", rev, "--"]),
                      *paths[i:i + CHUNK]).stdout for i in range(0, len(paths), CHUNK))
    rows = [r.split("\t", 1) for r in out.split("\0") if r]
    if rev == ":":                                                            # mode sha stage\tpath
        return {path: " ".join(meta.split()[:2]) for meta, path in rows}
    return {path: f"{meta.split()[0]} {meta.split()[2]}" for meta, path in rows}  # mode type sha\tpath


def applied_clusters(repo: pathlib.Path, upstream: str) -> set[str]:
    """The clusters whose `Train-Cluster:` trailer is on this branch since `upstream` (a re-run resumes)."""
    rng = f"{upstream}..HEAD" if commit_of(repo, upstream) else "HEAD"
    out = git(repo, "log", "--format=%B", rng).stdout
    return set(re.findall(rf"^{TRAILER}: (\S+)\s*$", out, re.M))


def subject(repo: pathlib.Path, member: dict, tip: str) -> str:
    """`<cluster> <ids>: <lead note's title>`, at most SUBJECT_MAX characters. The ids come from the report's title
    (`# PB2914 (+ PB2897, PB2882): implementer report`), else the lead; the description from the lead note's title
    on the branch head, else the manifest's notes."""
    cluster, lead = str(member.get("cluster") or ""), str(member.get("lead") or "")
    ids = lead
    report = pathlib.Path(member.get("report") or "")
    if member.get("report") and report.is_file():
        for line in report.read_text(encoding="utf-8", errors="replace").splitlines():
            if line.startswith("# "):
                ids = re.sub(r"[:\s—-]*implementer report.*$", "", line[2:], flags=re.I).strip() or lead
                break
    desc = ""
    note = blob(repo, tip, f"kb/Work/{lead}.md") if lead else None
    if note and (m := re.search(r'^title:\s*"?(.*?)"?\s*$', note.decode("utf-8", "replace"), re.M)):
        desc = re.sub(r"^Defect( lead)?:\s*", "", re.sub(rf"^{re.escape(lead)}\s*[—-]\s*", "", m.group(1)))
    desc = desc or str(member.get("notes") or "")
    full = f"{cluster} {ids}".strip() + ": " + desc
    return full if len(full) <= SUBJECT_MAX else full[:SUBJECT_MAX - 1].rstrip() + "…"   # a long title is cut too


def message(repo: pathlib.Path, member: dict, tip: str, base: str, trailers: list[str]) -> str:
    body = [f"Cluster {member.get('cluster')} brought in by scripts/orchestrator/train_apply.py: the diff of branch "
            f"{member.get('branch') or '(none)'} from {base[:12]} to {tip[:12]}.",
            f"Notes: {member.get('notes') or '(none named)'}",
            f"Report: {member.get('report') or '(none named)'}"]
    return "\n".join([subject(repo, member, tip), "", *body, "", f"{TRAILER}: {member.get('cluster')}", *trailers]) + "\n"


def stamp_status(repo: pathlib.Path, line: str) -> None:
    """Keep STATUS.md describing HEAD (MANDATORY-PRACTICES P4): restamp its first line, record what this did."""
    path = repo / "STATUS.md"
    text = path.read_text(encoding="utf-8") if path.is_file() else "STATUS-AT: \n"
    lines = text.splitlines()
    head = git(repo, "rev-parse", "HEAD").stdout.strip()
    lines[0] = f"STATUS-AT: {head}"
    if "## train_apply" not in lines:
        lines += ["", "## train_apply"]
    lines.append(f"- {line}")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def apply_member(repo: pathlib.Path, member: dict, scratch: pathlib.Path, trailers: list[str]) -> str:
    """Bring one cluster in as one commit; returns a one-line account. Raises Stop."""
    cluster = str(member.get("cluster") or member.get("lead") or "?")
    branch = member.get("branch")
    tip = commit_of(repo, member.get("head")) or commit_of(repo, branch)
    if not tip:
        raise Stop(f"cluster {cluster}: neither head {member.get('head')!r} nor branch {branch!r} is a commit here")
    if member.get("head") and branch and (bt := commit_of(repo, branch)) and bt != tip:
        print(f"  ⚠ {cluster}: branch {branch} is at {bt[:12]}, the report's head is {tip[:12]}: bringing in the "
              "REPORTED head (commits after it were never reported)")
    base = commit_of(repo, member.get("base"))
    if not base or git(repo, "merge-base", "--is-ancestor", base, tip, check=False).returncode != 0:
        mb = git(repo, "merge-base", tip, "HEAD").stdout.strip()
        print(f"  ⚠ {cluster}: base {str(member.get('base'))[:12]!r} is not an ancestor of {tip[:12]}; diffing from "
              f"the merge-base with the train, {mb[:12]}")
        base = mb
    paths = [p for p in git(repo, "diff", "--name-only", "--no-renames", base, tip, "--", ".", *EXCLUDE)
             .stdout.splitlines() if p]
    if not paths:
        return f"{cluster}: EMPTY (the branch changes nothing from {base[:12]}); no commit"
    patch = scratch / f"{cluster}.patch"
    patch.write_bytes(git(repo, "diff", "--binary", "--full-index", "--no-renames", base, tip, "--", ".", *EXCLUDE,
                          binary=True).stdout)
    before = git(repo, "rev-parse", "HEAD").stdout.strip()
    msg = scratch / f"{cluster}.msg"
    msg.write_text(message(repo, member, tip, base, trailers), encoding="utf-8")
    r = git(repo, "apply", "-3", "--whitespace=nowarn", str(patch), check=False)
    unmerged = sorted(set(git(repo, "diff", "--name-only", "--diff-filter=U").stdout.split()))
    if r.returncode != 0 or unmerged:
        failed = sorted(set(re.findall(r"(?:patch failed|does not exist in index|already exists in (?:index|working "
                                       r"directory)|with conflicts)[^\n]*?([\w./-]+\.[\w]+)", r.stderr)) - set(unmerged))
        raise Stop(f"cluster {cluster} (branch {branch}, {tip[:12]}) does not apply cleanly onto {before[:12]}:\n"
                   + "".join(f"    CONFLICT {p}\n" for p in unmerged)
                   + "".join(f"    FAILED   {p}\n" for p in failed)
                   + (f"    git apply said:\n" + "".join(f"      {x}\n" for x in r.stderr.strip().splitlines()[:20])
                      if r.stderr.strip() else "")
                   + f"  Resolve each against the spec and the report (lander-train-brief step 1), `git add` them, "
                     f"then `git commit -F {msg}` and run this script again: it resumes after {cluster}.")
    marked = sorted({p for i in range(0, len(paths), CHUNK) for p in git(
        repo, "grep", "--cached", "-lzI", "-e", "^<<<<<<< ", "-e", "^||||||| ", "-e", "^>>>>>>> ", "--",
        *paths[i:i + CHUNK], check=False).stdout.split("\0") if p})
    if marked:
        raise Stop(f"cluster {cluster} (branch {branch}): conflict-marker lines in " + ", ".join(marked)
                   + " after a clean apply: the branch itself carries them (commit c6460d0f's shape). Fix them on the "
                   f"branch or drop the cluster; the apply is staged, `git reset -q --hard {before[:12]}` undoes it.")
    # Object ids, one git call per side (a per-path `git show` cost ~70 s on a 123-path cluster, measured on train 1052).
    was, at_base, at_tip, staged = (ids(repo, before, paths), ids(repo, base, paths), ids(repo, tip, paths),
                                    ids(repo, ":", paths))
    lost = [p for p in paths if was.get(p) == at_base.get(p) and staged.get(p) != at_tip.get(p)]
    if lost:
        raise Stop(f"cluster {cluster} (branch {branch}): SILENT LOSS: " + ", ".join(lost) + f" changed only by this "
                   f"cluster, yet the index does not hold the branch's version; `git reset -q --hard {before[:12]}`.")
    merged = sum(1 for p in paths if staged.get(p) != at_tip.get(p))
    git(repo, "commit", "-q", "--no-verify", "-F", str(msg))
    sha = git(repo, "rev-parse", "HEAD").stdout.strip()
    line = (f"{cluster}: {sha[:12]} ← {branch or '(no branch)'} {tip[:12]}: {len(paths)} path(s), "
            f"{len(paths) - merged} as on the branch, {merged} merged with earlier changes")
    stamp_status(repo, line)
    return line


def run_audits(repo: pathlib.Path, touched: dict[str, set[str]]) -> int:
    """The batched citation and register audits, in parallel; each finding attributed to the clusters touching it."""
    env = dict(os.environ, PYTHONIOENCODING="utf-8")

    def one(audit: tuple[str, list[str]]) -> tuple[str, int, str]:
        r = subprocess.run([sys.executable, *audit[1]], cwd=repo, capture_output=True, text=True, encoding="utf-8",
                           errors="replace", env=env)
        return audit[0], r.returncode, (r.stdout + r.stderr).strip()

    bad = 0
    with concurrent.futures.ThreadPoolExecutor(len(AUDITS)) as pool:
        for name, rc, out in pool.map(one, AUDITS):
            if rc == 0:
                print(f"  audit {name}: PASS")
                continue
            bad += 1
            print(f"  ⛔ audit {name}: FAILED (exit {rc})")
            for line in out.splitlines()[-40:]:
                owners = sorted(c for c, ps in touched.items() if any(p in line.replace("\\", "/") for p in ps))
                print(f"      {line}" + (f"   ← cluster {', '.join(owners)}" if owners else ""))
    return bad


def run(repo: pathlib.Path, manifest_path: pathlib.Path, trailers: list[str], audits: bool, upstream: str,
        drop: frozenset[str] = frozenset()) -> int:
    try:
        members = json.loads(manifest_path.read_text(encoding="utf-8"))
        assert isinstance(members, list) and all(isinstance(m, dict) for m in members)
    except (OSError, ValueError, AssertionError) as e:
        print(f"=== TRAIN APPLY: NOT RUN (the manifest {manifest_path} is not a JSON list of members: {e}) ===")
        return 2
    dirty = [line for line in git(repo, "status", "--porcelain", "--", ".", *EXCLUDE).stdout.splitlines() if line]
    if dirty:
        print("=== TRAIN APPLY: NOT RUN (the tree is not clean: commit or resolve first) ===")
        print("".join(f"    {d}\n" for d in dirty[:20]), end="")
        return 2
    scratch = pathlib.Path(tempfile.mkdtemp(prefix=f"train-apply-{manifest_path.stem}-"))
    done = applied_clusters(repo, upstream)
    started = time.monotonic()
    touched: dict[str, set[str]] = {}
    print(f"train_apply: {len(members)} cluster(s) from {manifest_path.name}; patches and messages in {scratch}")
    for m in members:
        cluster = str(m.get("cluster") or m.get("lead") or "?")
        t0 = time.monotonic()
        if cluster in done:
            print(f"  {cluster}: already on the train branch (Train-Cluster trailer); skipped")
            continue
        if cluster in drop:
            print(f"  {cluster}: DROPPED by the lander (--drop); not brought in")
            continue
        try:
            line = apply_member(repo, m, scratch, trailers)
        except Stop as e:
            print(f"⛔ {e}")
            print(f"=== TRAIN APPLY: STOPPED at cluster {cluster} after {time.monotonic() - started:.1f} s ===")
            return 1
        if " EMPTY " not in line:
            touched[cluster] = set(git(repo, "diff", "--name-only", "HEAD~1", "HEAD").stdout.split())
        print(f"  {line}  [{time.monotonic() - t0:.1f} s]")
    applied = time.monotonic() - started
    bad = run_audits(repo, touched) if audits else 0
    total = time.monotonic() - started
    verdict = "GREEN" if not bad else f"AUDIT RED ({bad})"
    print(f"=== TRAIN APPLY: {verdict} — {len(members)} cluster(s), applied in {applied:.1f} s, "
          f"{total:.1f} s with the audits ===")
    return 0 if not bad else 1


# ── self-test: a scratch repository with real branches ─────────────────────────────────────────────────────────
def self_test() -> int:
    for name in subprocess.run(["git", "rev-parse", "--local-env-vars"], check=True, capture_output=True,
                               text=True).stdout.split():
        os.environ.pop(name, None)       # hermetic: nothing reaches the caller's repository
    os.environ.update(GIT_AUTHOR_NAME="t", GIT_AUTHOR_EMAIL="t@t", GIT_COMMITTER_NAME="t", GIT_COMMITTER_EMAIL="t@t")
    root = pathlib.Path(tempfile.mkdtemp(prefix="train-apply-test-"))
    repo = root / "repo"
    repo.mkdir()
    results: list[tuple[str, bool, str]] = []

    def sh(*args: str) -> str:
        return git(repo, *args).stdout.strip()

    def write(rel: str, text: str) -> None:
        (repo / rel).parent.mkdir(parents=True, exist_ok=True)
        (repo / rel).write_text(text, encoding="utf-8", newline="")

    def branch(name: str, at: str, edits: dict[str, str]) -> str:
        sh("checkout", "-q", "-B", name, at)
        for rel, text in edits.items():
            write(rel, text)
        sh("add", "-A")
        sh("commit", "-qm", f"WIP checkpoint: {name}")
        return sh("rev-parse", "HEAD")

    def member(cluster: str, br: str, base: str, head: str, lead: str = "PB1", title: str = "") -> dict:
        report = root / f"{cluster}-report.md"
        report.write_text(f"Status: DONE\n\n# {title or lead + ' (+ PB9)'}: implementer report\n", encoding="utf-8")
        return {"cluster": cluster, "lead": lead, "notes": lead, "report": str(report), "branch": br, "base": base,
                "head": head}

    def apply(label: str, members: list[dict], *, fresh: bool = True) -> tuple[int, str, list[str]]:
        """Run the script on a train branch cut from main (or, fresh=False, on the current one, as a re-run)."""
        if fresh:
            sh("checkout", "-q", "-B", f"train-{label}", "main")
        path = root / f"{label}.json"
        path.write_text(json.dumps(members), encoding="utf-8")
        buf = io.StringIO()
        with contextlib.redirect_stdout(buf):
            rc = run(repo, path, ["Co-Authored-By: t <t@t>"], audits=False, upstream="main")
        subjects = sh("log", "--format=%s", "main..HEAD").splitlines()[::-1]
        return rc, buf.getvalue(), subjects

    sh("init", "-q", "-b", "main")
    def six(**changed: str) -> str:
        """a.txt's six lines, `aN=...` replacing line N. Hunks two lines apart merge; adjacent ones conflict in git."""
        return "".join(changed.get(f"a{i}", f"a{i}") + "\n" for i in range(1, 7))

    write("a.txt", six())
    write("b.txt", "b1\nb2\nb3\n")
    write("kb/Work/PB1.md", '---\ntitle: "PB1 — the first fix"\n---\n')
    sh("add", "-A")
    sh("commit", "-qm", "base")
    base = sh("rev-parse", "HEAD")
    x = branch("x", base, {"a.txt": six(a2="A2"), "new/x.bin": "\x00\x01binary"})
    y = branch("y", base, {"b.txt": "b1\nB2\nb3\n", "a.txt": six(a5="A5")})   # a.txt: a hunk apart from x's
    z = branch("z", base, {"a.txt": six(a2="Z2")})                            # a.txt: the SAME line as x
    sh("checkout", "-q", "main")
    (repo / "STATUS.md").write_text("STATUS-AT: x\nlander notes\n", encoding="utf-8")
    sh("config", "core.excludesFile", str(root / "ignore"))
    (root / "ignore").write_text("STATUS.md\n", encoding="utf-8")

    long_title = "PB2 group (PB2, PB3, PB4, PB5) — a report title far longer than any commit subject may be"
    clean = [member("X", "x", base, x), member("Y", "y", base, y, lead="PB2", title=long_title)]
    rc, out, subjects = apply("clean", clean)
    tree_ok = (repo / "a.txt").read_text() == six(a2="A2", a5="A5") and (repo / "new/x.bin").is_file()
    results.append(("a clean train applies with ONE commit per cluster, in order, both hunks of a shared file kept",
                    rc == 0 and len(subjects) == 2 and subjects[0].startswith("X PB1 (+ PB9): the first fix")
                    and subjects[1].startswith("Y PB2 group") and tree_ok
                    and all(len(s) <= SUBJECT_MAX for s in subjects), f"rc={rc} subjects={subjects}\n{out}"))
    body = sh("log", "-1", "--format=%B", "HEAD~1")
    results.append(("each commit carries its Train-Cluster trailer and the caller's trailer",
                    "Train-Cluster: X" in body and "Co-Authored-By: t <t@t>" in body, body))
    status = (repo / "STATUS.md").read_text(encoding="utf-8")
    results.append(("STATUS.md is restamped to HEAD and keeps the lander's lines",
                    status.startswith(f"STATUS-AT: {sh('rev-parse', 'HEAD')}") and "lander notes" in status
                    and "Y: " in status, status))
    rc, out, subjects = apply("resume", clean, fresh=False)
    results.append(("a re-run resumes: clusters already on the branch are skipped, nothing is applied twice",
                    rc == 0 and len(subjects) == 2 and out.count("already on the train branch") == 2, out))
    rc, out, subjects = apply("conflict", [member("X", "x", base, x), member("Z", "z", base, z),
                                           member("Y", "y", base, y, lead="PB2")])
    results.append(("a conflict STOPS at the right cluster, naming its branch and the conflicted path, after one commit",
                    rc == 1 and "cluster Z (branch z" in out and "CONFLICT a.txt" in out and len(subjects) == 1
                    and "STOPPED at cluster Z" in out, f"rc={rc} subjects={subjects}\n{out}"))
    results.append(("... and leaves the merge in the tree, with the message file to commit it",
                    "<<<<<<< " in (repo / "a.txt").read_text() and "git commit -F" in out, out))
    sh("reset", "-q", "--hard", "HEAD")
    path = root / "conflict.json"
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        rc = run(repo, path, [], audits=False, upstream="main", drop=frozenset({"Z"}))
    subjects = sh("log", "--format=%s", "main..HEAD").splitlines()[::-1]
    results.append(("a cluster the lander DROPS is left out and the rest of the train comes in after it",
                    rc == 0 and [s.split()[0] for s in subjects] == ["X", "Y"] and "Z: DROPPED" in buf.getvalue(),
                    f"rc={rc} subjects={subjects}\n{buf.getvalue()}"))
    m = branch("m", base, {"b.txt": "b1\n<<<<<<< ours\nb2\n=======\nB2\n>>>>>>> theirs\nb3\n"})
    rc, out, subjects = apply("marker", [member("M", "m", base, m)])
    results.append(("a branch that itself carries conflict markers STOPS (commit c6460d0f's shape)",
                    rc == 1 and "conflict-marker lines in b.txt" in out and not subjects, out))
    sh("reset", "-q", "--hard", "HEAD")
    sh("checkout", "-q", "-B", "train-dirty", "main")
    write("a.txt", "dirty\n")
    rc, out, subjects = apply("dirty", [member("X", "x", base, x)], fresh=False)
    results.append(("a dirty tree is refused before anything is applied (NOT RUN, exit 2)",
                    rc == 2 and not subjects and "not clean" in out, out))
    sh("checkout", "-q", "--", "a.txt")

    shutil.rmtree(root, ignore_errors=True)
    bad = [r for r in results if not r[1]]
    for label, ok, got in results:
        print(f"  {'ok ' if ok else 'BAD'} {label}" + ("" if ok else f":\n{got}"))
    print(f"=== TRAIN APPLY SELF-TEST: {'PASS' if not bad else 'FAIL'} ({len(results) - len(bad)}/{len(results)}) ===")
    return 1 if bad else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument("manifest", nargs="?", type=pathlib.Path, help="the train manifest (JSON list of members)")
    ap.add_argument("--trailer", action="append", default=[], help="a line to end every cluster commit with "
                    "(the session's attribution lines); repeatable")
    ap.add_argument("--no-audits", action="store_true", help="skip the batched citation and register audits")
    ap.add_argument("--drop", action="append", default=[], metavar="CLUSTER", help="leave this cluster out (a "
                    "conflict the lander could not resolve from evidence: lander-train-brief step 1); repeatable")
    ap.add_argument("--upstream", default="origin/main", help="where the train branch starts (for resuming)")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    if a.self_test:
        return self_test()
    if not a.manifest:
        ap.error("a train manifest is required")
    top = subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True)
    if top.returncode != 0:
        print("=== TRAIN APPLY: NOT RUN (not inside a git work tree) ===")
        return 2
    return run(pathlib.Path(top.stdout.strip()), a.manifest, a.trailer, not a.no_audits, a.upstream, frozenset(a.drop))


if __name__ == "__main__":
    sys.exit(main())
