#!/usr/bin/env python3
"""ruleset.py — THE RULESET ON `main` THAT MAKES GITHUB'S MERGE QUEUE THE LANDING, kept as data in the tree
(scripts/orchestrator/ruleset-main.json) and compared with the live repository (kb/Work PB3024; DESIGN-test-build-ci.md
§3.17.6).

    python scripts/orchestrator/ruleset.py check [--json]
    python scripts/orchestrator/ruleset.py apply --owner-go "<the owner's go, verbatim, with its time>" [--dry-run]
    python scripts/orchestrator/ruleset.py --self-test

WHY A FILE AND A CHECK. Classic branch protection has no merge queue, so `main` needs a RULESET: require a pull
request, require the merge queue (merge method MERGE: a merge commit, so the branch's commits stay ancestors of main),
require `ci-gate` (evaluated on the merge group), no bypass (enforced for administrators, as the classic protection's
enforce_admins was), no force push, no deletion; and three repository settings the queue needs (`allow_auto_merge`,
so a pull request whose checks are still running can be queued by auto-merge; `allow_merge_commit`;
`delete_branch_on_merge`).
A setting clicked into GitHub's UI is invisible to the tree and drifts unseen; this file IS the setting, and `check`
says whether the live repository still matches it.

`check` — one line, `ruleset: <STATE> — <detail>`, and the exit code (`--json` adds the state and the differences):
    0 ACTIVE   the committed ruleset is live on main, the merge queue exists, the repository settings match and the
               classic protection is gone. The ONLY state in which a landing may open or queue a pull request
               (scripts/orchestrator/enqueue.py refuses otherwise, exit 8 — the operator's review 2026-10-11: a PR
               opened before the queue exists runs only the cheap jobs, its `ci-gate` passes on them, and auto-merge
               would land it with no whole-population run). ACTIVE also prints the queue's depth.
    1 DRIFT    a ruleset of this name exists but differs (each difference named), or the classic protection is still
               on main beside it, or a repository setting differs.
    3 ABSENT   no ruleset of this name on main: the queue is not applied yet (landings go through push-main.sh).
    4 SKIPPED  the live state could not be read (no gh, not authenticated, no network, a timeout): NOT RUN, never a pass.
    2          usage, or the committed file is unreadable.
Only the keys the committed file names are compared (GitHub adds ids, timestamps and defaults to the live ruleset);
keys starting with `$` are notes.

`apply` — run ONLY on the owner's explicit go (the attended session asks, one bare question: from that instant main
accepts only queued pull requests and `push-main.sh` can no longer push it). It sets the repository settings, creates
the ruleset or updates the one of the same name, deletes the classic protection (a 404 is "already gone"), then runs
`check` and exits with its code. `--owner-go` is required and printed with the result, so the transcript names the go.
`--dry-run` prints the calls and makes none.
"""
from __future__ import annotations

import argparse
import copy
import json
import os
import pathlib
import shutil
import subprocess
import sys
import tempfile
from typing import Callable

HERE = pathlib.Path(__file__).resolve().parent
COMMITTED = HERE / "ruleset-main.json"
BRANCH = "main"
TIMEOUT_S = 120   # a hang guard on one gh call: a call that never answers is SKIPPED (not observed), never a verdict

ACTIVE, DRIFT, ABSENT, SKIPPED = "ACTIVE", "DRIFT", "ABSENT", "SKIPPED"
EXIT = {ACTIVE: 0, DRIFT: 1, ABSENT: 3, SKIPPED: 4}

# A gh runner: argv (after `gh`) -> (exit code, stdout, stderr). The self-test passes a fake one.
Gh = Callable[[list[str]], tuple[int, str, str]]


class Unobserved(Exception):
    """The live state could not be read: SKIPPED, never a pass."""


def real_gh(args: list[str]) -> tuple[int, str, str]:
    exe = shutil.which("gh")
    if not exe:
        raise Unobserved("the GitHub CLI (gh) is not on PATH")
    try:
        p = subprocess.run([exe, *args], capture_output=True, text=True, encoding="utf-8", errors="replace",
                           timeout=TIMEOUT_S)
    except subprocess.TimeoutExpired:
        raise Unobserved(f"`gh {' '.join(args[:2])}` gave no answer in {TIMEOUT_S} s") from None
    return p.returncode, p.stdout, p.stderr


def load_committed(path: pathlib.Path = COMMITTED) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def strip_notes(value):
    """The committed value with every `$`-prefixed note key removed, at any depth: what is sent and compared."""
    if isinstance(value, dict):
        return {k: strip_notes(v) for k, v in value.items() if not k.startswith("$")}
    if isinstance(value, list):
        return [strip_notes(v) for v in value]
    return value


def differences(want, live, where: str = "") -> list[str]:
    """Each place the LIVE value disagrees with the committed one, comparing only what the committed value names: a
    dict compares its own keys; the ruleset's `rules` list is matched by `type` (GitHub returns it in its own order);
    any other list must be equal element by element under the same projection."""
    if isinstance(want, dict):
        if not isinstance(live, dict):
            return [f"{where or '<root>'}: expected an object, live has {json.dumps(live)}"]
        out: list[str] = []
        for k, v in want.items():
            here = f"{where}.{k}" if where else k
            if k not in live:
                out.append(f"{here}: missing on the live side (committed {json.dumps(v)})")
            else:
                out += differences(v, live[k], here)
        return out
    if isinstance(want, list):
        if not isinstance(live, list):
            return [f"{where}: expected a list, live has {json.dumps(live)}"]
        if where.endswith("rules") and all(isinstance(r, dict) and "type" in r for r in want):
            by_type = {r.get("type"): r for r in live if isinstance(r, dict)}
            out = []
            for r in want:
                if r["type"] not in by_type:
                    out.append(f"{where}[{r['type']}]: rule missing on the live side")
                else:
                    out += differences(r, by_type[r["type"]], f"{where}[{r['type']}]")
            extra = sorted(set(by_type) - {r["type"] for r in want})
            if extra:
                out.append(f"{where}: live rules the committed file does not name: {', '.join(map(str, extra))}")
            return out
        if len(want) != len(live):
            return [f"{where}: committed {json.dumps(want)}, live {json.dumps(live)}"]
        out = []
        for i, (w, l) in enumerate(zip(want, live)):
            out += differences(w, l, f"{where}[{i}]")
        return out
    return [] if want == live else [f"{where}: committed {json.dumps(want)}, live {json.dumps(live)}"]


def _api(gh: Gh, path: str) -> tuple[int, object]:
    """GET one API path; (404-ness, parsed JSON). A failure other than a 404 is Unobserved."""
    rc, out, err = gh(["api", path])
    if rc != 0:
        text = (out + err)
        if "HTTP 404" in text or '"status":"404"' in text.replace(" ", "") or "Not Found" in text \
                or "Branch not protected" in text:
            return 404, None
        raise Unobserved(f"`gh api {path}` failed (exit {rc}): {text.strip()[:200]}")
    try:
        return 200, json.loads(out or "null")
    except json.JSONDecodeError as e:
        raise Unobserved(f"`gh api {path}` returned no JSON: {e}") from None


def repo_name(gh: Gh) -> str:
    rc, out, err = gh(["repo", "view", "--json", "nameWithOwner", "--jq", ".nameWithOwner"])
    if rc != 0 or not out.strip():
        raise Unobserved(f"could not resolve the repository with gh (exit {rc}): {(out + err).strip()[:200]}")
    return out.strip()


def find_ruleset(gh: Gh, repo: str, name: str):
    """The live ruleset of this name, in full, or None."""
    code, listing = _api(gh, f"repos/{repo}/rulesets")
    if code == 404 or not isinstance(listing, list):
        return None
    for r in listing:
        if isinstance(r, dict) and r.get("name") == name:
            code, full = _api(gh, f"repos/{repo}/rulesets/{r.get('id')}")
            return full if code == 200 else None
    return None


def queue_depth(gh: Gh, repo: str):
    """The merge queue's depth on main, or None when GitHub has no queue there (GraphQL `mergeQueue` is null)."""
    owner, name = repo.split("/", 1)
    query = ("query($o:String!,$n:String!,$b:String!){repository(owner:$o,name:$n)"
             "{mergeQueue(branch:$b){entries(first:1){totalCount}}}}")
    rc, out, err = gh(["api", "graphql", "-f", f"query={query}", "-f", f"o={owner}", "-f", f"n={name}",
                       "-f", f"b={BRANCH}", "--jq", ".data.repository.mergeQueue.entries.totalCount"])
    if rc != 0:
        raise Unobserved(f"the merge-queue query failed (exit {rc}): {(out + err).strip()[:200]}")
    text = out.strip()
    return int(text) if text.isdigit() else None


def check(gh: Gh, committed: dict) -> tuple[str, str, list[str]]:
    """(state, one-line detail, differences)."""
    want = strip_notes(committed)
    try:
        repo = repo_name(gh)
        live = find_ruleset(gh, repo, want["ruleset"]["name"])
        if live is None:
            return ABSENT, (f"no ruleset '{want['ruleset']['name']}' on {repo}:{BRANCH} — the merge queue is NOT "
                            "applied; landings go through bash scripts/push-main.sh"), []
        diffs = differences(want["ruleset"], live, "ruleset")
        code, settings = _api(gh, f"repos/{repo}")
        diffs += differences(want["repository"], settings if code == 200 else {}, "repository")
        code, _ = _api(gh, f"repos/{repo}/branches/{BRANCH}/protection")
        if code != 404:
            diffs.append(f"branch protection: the classic protection is still on {BRANCH} (apply deletes it)")
        depth = queue_depth(gh, repo)
        if depth is None:
            diffs.append(f"merge queue: GitHub reports no merge queue on {BRANCH}")
    except Unobserved as e:
        return SKIPPED, f"the live state was not read ({e}) — NOT RUN, never a pass", []
    if diffs:
        return DRIFT, f"the live ruleset on {repo}:{BRANCH} differs from {COMMITTED.name} ({len(diffs)})", diffs
    return ACTIVE, f"the merge queue is live on {repo}:{BRANCH} as committed; queue depth {depth}", []


def apply(gh: Gh, committed: dict, owner_go: str, dry_run: bool, say: Callable[[str], None] = print) -> int:
    if not owner_go.strip():
        say("⛔ ruleset apply: --owner-go is required — the owner's explicit go, verbatim (PB3024): from the moment this "
            "runs main accepts only queued pull requests.")
        return 2
    want = strip_notes(committed)
    calls: list[list[str]] = []

    def call(args: list[str], body: dict | None = None) -> int:
        shown = " ".join(args) + (" <committed JSON>" if body is not None else "")
        if dry_run:
            say(f"  would run: gh {shown}")
            return 0
        if body is not None:
            fd, name = tempfile.mkstemp(suffix=".json", prefix="ruleset-apply-")
            tmp = pathlib.Path(name)
            with os.fdopen(fd, "w", encoding="utf-8") as f:
                json.dump(body, f)
            try:
                rc, out, err = gh([*args, "--input", str(tmp)])
            finally:
                tmp.unlink(missing_ok=True)
        else:
            rc, out, err = gh(args)
        calls.append(args)
        if rc != 0 and not (args[-1].endswith("/protection") and ("404" in out + err or "not protected" in out + err)):
            say(f"⛔ ruleset apply: gh {shown} failed (exit {rc}): {(out + err).strip()[:400]}")
            return rc or 1
        return 0

    try:
        repo = repo_name(gh)
        say(f"ruleset apply on {repo}:{BRANCH} — owner's go: {owner_go}")
        settings = []
        for k, v in want["repository"].items():
            # -f sends a string as typed; -F converts true/false/null/numbers (a string through -F keeps no quotes, but
            # one through json.dumps would arrive WITH them)
            settings += ["-f", f"{k}={v}"] if isinstance(v, str) else ["-F", f"{k}={json.dumps(v)}"]
        if call(["api", "--method", "PATCH", f"repos/{repo}", *settings]):
            return 1
        existing = find_ruleset(gh, repo, want["ruleset"]["name"])
        if existing is not None:
            rc = call(["api", "--method", "PUT", f"repos/{repo}/rulesets/{existing.get('id')}"], want["ruleset"])
        else:
            rc = call(["api", "--method", "POST", f"repos/{repo}/rulesets"], want["ruleset"])
        if rc:
            return 1
        if call(["api", "--method", "DELETE", f"repos/{repo}/branches/{BRANCH}/protection"]):
            return 1
    except Unobserved as e:
        say(f"⛔ ruleset apply: {e}")
        return EXIT[SKIPPED]
    if dry_run:
        return 0
    state, detail, diffs = check(gh, committed)
    say(f"ruleset: {state} — {detail}")
    for d in diffs:
        say(f"  · {d}")
    return EXIT[state]


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def _fake(routes: dict[str, tuple[int, str]], log: list[list[str]] | None = None) -> Gh:
    """A gh stand-in: the first route key that the joined argv starts with answers; anything unrouted is a failure."""
    def gh(args: list[str]) -> tuple[int, str, str]:
        if log is not None:
            log.append(list(args))
        joined = " ".join(args)
        for key, (rc, out) in routes.items():
            if joined.startswith(key):
                return rc, out, "" if rc == 0 else out
        return 1, "", f"unrouted: {joined}"
    return gh


def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, ok: bool, detail: str = "") -> None:
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f"  ({detail})" if detail and not ok else ""))
        if not ok:
            failures.append(label)

    committed = load_committed()
    want = strip_notes(committed)
    repo = "o/r"
    live_rs = copy.deepcopy(want["ruleset"])
    live_rs.update({"id": 7, "source": repo, "created_at": "x", "node_id": "n"})   # what GitHub adds
    live_rs["rules"] = list(reversed(live_rs["rules"]))                           # GitHub's own order
    live_rs["rules"][0] = {**live_rs["rules"][0]}
    for r in live_rs["rules"]:
        if r["type"] == "pull_request":
            r["parameters"] = {**r["parameters"], "allowed_merge_methods": ["merge", "squash", "rebase"]}
    settings = {**want["repository"], "visibility": "public", "allow_squash_merge": True}
    base = {
        "repo view": (0, repo),
        f"api repos/{repo}/rulesets/7": (0, json.dumps(live_rs)),
        f"api repos/{repo}/rulesets": (0, json.dumps([{"id": 7, "name": want["ruleset"]["name"]}])),
        f"api repos/{repo}/branches/main/protection": (1, "gh: Branch not protected (HTTP 404)"),
        "api graphql": (0, "2"),
        f"api repos/{repo}": (0, json.dumps(settings)),
    }

    state, detail, diffs = check(_fake(base), committed)
    expect("a live ruleset equal to the committed one, the queue present and no classic protection, is ACTIVE",
           state == ACTIVE and "depth 2" in detail and not diffs, f"{state} {detail} {diffs}")
    expect("extra fields GitHub adds to the live ruleset, and its own rule order, are not drift",
           state == ACTIVE, f"{state} {diffs}")

    absent = {**base, f"api repos/{repo}/rulesets": (0, "[]")}
    state, detail, _ = check(_fake(absent), committed)
    expect("no ruleset on main is ABSENT, never ACTIVE", state == ABSENT and "push-main.sh" in detail, state)

    drifted = copy.deepcopy(live_rs)
    for r in drifted["rules"]:
        if r["type"] == "merge_queue":
            r["parameters"]["merge_method"] = "SQUASH"
    drifted["rules"] = [r for r in drifted["rules"] if r["type"] != "required_status_checks"]
    drifted["enforcement"] = "evaluate"
    state, _, diffs = check(_fake({**base, f"api repos/{repo}/rulesets/7": (0, json.dumps(drifted))}), committed)
    expect("a live ruleset that differs (a parameter, a missing rule, enforcement off) is DRIFT with each difference named",
           state == DRIFT and any("merge_method" in d for d in diffs) and any("required_status_checks" in d for d in diffs)
           and any("enforcement" in d for d in diffs), f"{state} {diffs}")

    classic = {**base, f"api repos/{repo}/branches/main/protection": (0, json.dumps({"enforce_admins": {}}))}
    state, _, diffs = check(_fake(classic), committed)
    expect("the classic protection still on main beside the ruleset is DRIFT", state == DRIFT
           and any("classic protection" in d for d in diffs), f"{state} {diffs}")
    no_queue = {**base, "api graphql": (0, "null")}
    state, _, diffs = check(_fake(no_queue), committed)
    unset = {**base, f"api repos/{repo}": (0, json.dumps({**settings, "allow_auto_merge": False}))}
    state2, _, diffs2 = check(_fake(unset), committed)
    expect("no merge queue reported by GitHub, or a repository setting the queue needs turned off, is DRIFT",
           state == DRIFT and state2 == DRIFT and any("allow_auto_merge" in d for d in diffs2), f"{state} {state2}")

    offline = {"repo view": (1, "error connecting to api.github.com")}
    state, detail, _ = check(_fake(offline), committed)

    def raises(args: list[str]) -> tuple[int, str, str]:
        raise Unobserved("gh is not on PATH")
    state2, _, _ = check(raises, committed)
    partial = {**base, f"api repos/{repo}/rulesets": (1, "HTTP 502: bad gateway")}
    state3, _, _ = check(_fake(partial), committed)
    expect("no gh, no network or a server error is SKIPPED, never ACTIVE",
           state == SKIPPED and state2 == SKIPPED and state3 == SKIPPED and "never a pass" in detail,
           f"{state} {state2} {state3}")

    log: list[list[str]] = []
    lines: list[str] = []
    rc = apply(_fake(base, log), committed, "", dry_run=False, say=lines.append)
    refused = rc == 2 and not log
    log.clear()
    rc_create = apply(_fake({**absent, f"api --method": (0, "{}")}, log), committed, "go 2026-10-11", dry_run=False,
                      say=lines.append)
    created = [c for c in log if c[:2] == ["api", "--method"]]
    log.clear()
    apply(_fake({**base, f"api --method": (0, "{}")}, log), committed, "go", dry_run=False, say=lines.append)
    updated = [c for c in log if c[:2] == ["api", "--method"]]
    expect("apply refuses without the owner's go, and with it sets the repository, creates or updates the ruleset by "
           "name and deletes the classic protection",
           refused and rc_create == EXIT[ABSENT]   # the fake keeps answering "no ruleset" after the POST
           and [c[2] for c in created] == ["PATCH", "POST", "DELETE"]
           and [c[2] for c in updated] == ["PATCH", "PUT", "DELETE"]
           and updated[1][3].endswith("/rulesets/7") and updated[2][3].endswith("/branches/main/protection")
           # a boolean setting is typed (-F), a string one is sent as typed (-f), and no value carries JSON quotes
           and created[0][created[0].index("allow_auto_merge=true") - 1] == "-F"
           and created[0][created[0].index("merge_commit_message=PR_BODY") - 1] == "-f"
           and not any('"' in a for a in created[0]),
           f"refused={refused} rc={rc_create} created={created} updated={updated}")

    rules = {r["type"]: r.get("parameters", {}) for r in want["ruleset"]["rules"]}
    checks = rules.get("required_status_checks", {}).get("required_status_checks", [])
    expect("the committed ruleset requires a pull request, the merge queue with a merge commit and ci-gate, with no bypass",
           want["ruleset"]["enforcement"] == "active" and want["ruleset"]["bypass_actors"] == []
           and want["ruleset"]["conditions"]["ref_name"]["include"] == ["refs/heads/main"]
           and "pull_request" in rules and rules.get("merge_queue", {}).get("merge_method") == "MERGE"
           and [c.get("context") for c in checks] == ["ci-gate"]
           and want["repository"].get("allow_auto_merge") is True, json.dumps(rules))

    print(f"=== ruleset.py self-test: {'FAIL' if failures else 'PASS'} ({len(failures)} failure(s)) ===")
    return 1 if failures else 0


def main(argv: list[str] | None = None) -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument("--self-test", action="store_true")
    sub = ap.add_subparsers(dest="verb")
    c = sub.add_parser("check", help="compare the live state with the committed ruleset")
    c.add_argument("--json", action="store_true")
    a_ = sub.add_parser("apply", help="apply the committed ruleset (the owner's explicit go only)")
    a_.add_argument("--owner-go", required=True)
    a_.add_argument("--dry-run", action="store_true")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    try:
        committed = load_committed()
    except (OSError, json.JSONDecodeError) as e:
        print(f"⛔ ruleset: {COMMITTED} is unreadable: {e}")
        return 2
    if a.verb == "check":
        state, detail, diffs = check(real_gh, committed)
        if a.json:
            print(json.dumps({"state": state, "detail": detail, "differences": diffs}))
        else:
            print(f"ruleset: {state} — {detail}")
            for d in diffs:
                print(f"  · {d}")
        return EXIT[state]
    if a.verb == "apply":
        return apply(real_gh, committed, a.owner_go, a.dry_run)
    ap.print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main())
