#!/usr/bin/env python3
"""The branch review's record (kb/Work PB3027; DESIGN-test-build-ci.md section 3.17.2 step 2): one JSON line per
decision in `<scratch>/review-w<wave>-<letter>.jsonl`, the verdict computed from it, and the stamp in the report.

    python scripts/orchestrator/review_findings.py start   <file> --round 1 --branch B --head SHA --reviewer LABEL
    python scripts/orchestrator/review_findings.py finding <file> --round 1 '<one JSON object>'
    python scripts/orchestrator/review_findings.py verdict <file> --round 1        # CLEAN | FINDINGS n, from the file
    python scripts/orchestrator/review_findings.py stamp   <report> --findings <file>
    python scripts/orchestrator/review_findings.py show    <file> [--round N]     # the findings, for the finisher
    python scripts/orchestrator/review_findings.py summary <file or directory>... # the measurement (PB3027 item 5)
    python scripts/orchestrator/review_findings.py --self-test

WHY A RECORD AND A TOOL. Until PB3027 the review of finished work ran inside the lander, between its gates: review
shards were 23 % of the land units' subagent tokens on 2026-10-10 and sat on the landing's critical path, and every
finding fixed in the train re-gated every cluster (kb/Work PB3015, its evidence table 6). The review now runs on the
BRANCH when its implementer returns DONE: ONE read-only `cobol-reviewer` (the `review` skill's branch form), inside the
rolling wave or dispatched by hand for an attended branch. The reviewer cannot write in a git tree (its hook), and a
kill can come at any moment, so every decision is a line on disk, appended through this tool the moment it is made:

  start    {round, branch, head, reviewer}: the review of one head began (its minutes are measured from here).
  finding  {round, file, line, category, summary, failure_scenario}: ONE confirmed finding (the reviewer verified it,
           running the scenario where that is cheap; a candidate it could not confirm is not recorded). The tool gives
           it an id `R<round>-<k>`, by which the finisher answers it.
  verdict  computed here, never claimed: CLEAN when the round recorded no finding, else FINDINGS n.

`stamp` writes the verdict into the implementer's report header as the line plan_wave.REVIEW reads (CLEAN <reviewer>
<head>, or FINDINGS <n> <reviewer> <head> <file>), and reads it back through plan_wave.parse_review, so the writer and
the reader cannot drift. prune_worktrees.py lists a DONE branch WAITING TO LAND only under a CLEAN stamp at its tip;
plan_wave.py plans a finisher for a FINDINGS stamp.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import re
import shutil
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import plan_wave  # noqa: E402  (the one reader of the stamp, and the one definition of a stamp line: PB3077)

KINDS = ("start", "finding", "verdict")
FINDING_FIELDS = ("file", "line", "category", "summary", "failure_scenario")
STATUS_LINE = re.compile(r"^\**Status:", re.I)
REVIEW_LINE = plan_wave.REVIEW_LINE   # the planner skips these lines when it reads the branch (kb/Work PB3077)


class RecordError(ValueError):
    pass


def now() -> str:
    return dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds")


def load(path: pathlib.Path) -> list[dict]:
    """Every record, in order. A malformed line is an error, never skipped: a lost finding would read CLEAN."""
    if not path.is_file():
        return []
    out = []
    for i, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if not line.strip():
            continue
        try:
            rec = json.loads(line)
        except ValueError as e:
            raise RecordError(f"{path}:{i}: not JSON ({e})") from e
        if not isinstance(rec, dict) or rec.get("kind") not in KINDS:
            raise RecordError(f"{path}:{i}: not a review record (kind must be one of {', '.join(KINDS)})")
        out.append(rec)
    return out


def append(path: pathlib.Path, rec: dict) -> dict:
    path.parent.mkdir(parents=True, exist_ok=True)
    rec = {**rec, "at": now()}
    with path.open("a", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(rec, ensure_ascii=False) + "\n")
    return rec


def of_round(recs: list[dict], rnd: int, kind: str) -> list[dict]:
    return [r for r in recs if r.get("round") == rnd and r.get("kind") == kind]


def start(path: pathlib.Path, rnd: int, branch: str, head: str, reviewer: str) -> dict:
    recs = load(path)
    if of_round(recs, rnd, "start"):
        raise RecordError(f"round {rnd} already started in {path}: a re-review of a new head is the next round")
    if rnd > 1 and not of_round(recs, rnd - 1, "verdict"):
        raise RecordError(f"round {rnd - 1} has no verdict in {path}: finish it (verdict) before round {rnd}")
    if not re.fullmatch(r"[0-9a-f]{7,40}", head):
        raise RecordError(f"--head must be a commit sha, got {head!r}")
    if not re.fullmatch(r"[^\s`*]+", reviewer):
        raise RecordError(f"--reviewer must be one word (the stamp reads it as one), got {reviewer!r}")
    return append(path, {"kind": "start", "round": rnd, "branch": branch, "head": head, "reviewer": reviewer})


def finding(path: pathlib.Path, rnd: int, raw: str) -> dict:
    recs = load(path)
    if not of_round(recs, rnd, "start"):
        raise RecordError(f"round {rnd} was never started in {path} (start first)")
    if of_round(recs, rnd, "verdict"):
        raise RecordError(f"round {rnd} already has its verdict in {path}: a later finding is the next round's")
    try:
        body = json.loads(raw)
    except ValueError as e:
        raise RecordError(f"the finding is not one JSON object: {e}") from e
    if not isinstance(body, dict):
        raise RecordError("the finding is not one JSON object")
    missing = [k for k in FINDING_FIELDS if body.get(k) in (None, "")]
    if missing:
        raise RecordError(f"the finding lacks {', '.join(missing)} (no scenario, no finding: the review skill's bar)")
    if not isinstance(body["line"], int) or body["line"] < 1:
        raise RecordError("`line` is the 1-based line the finding anchors to (an integer)")
    k = len(of_round(recs, rnd, "finding")) + 1
    return append(path, {**{k2: v for k2, v in body.items() if k2 not in ("kind", "round", "id", "at")},
                         "kind": "finding", "round": rnd, "id": f"R{rnd}-{k}"})


def verdict(path: pathlib.Path, rnd: int) -> dict:
    recs = load(path)
    started = of_round(recs, rnd, "start")
    if not started:
        raise RecordError(f"round {rnd} was never started in {path}")
    if of_round(recs, rnd, "verdict"):
        raise RecordError(f"round {rnd} already has its verdict in {path}")
    n = len(of_round(recs, rnd, "finding"))
    s = started[0]
    return append(path, {"kind": "verdict", "round": rnd, "branch": s["branch"], "head": s["head"],
                         "reviewer": s["reviewer"], "verdict": "CLEAN" if n == 0 else "FINDINGS", "findings": n})


def stamp_line(v: dict, findings_file: pathlib.Path) -> str:
    if v["verdict"] == "CLEAN":
        return f"**Review:** CLEAN {v['reviewer']} {v['head']}"
    return f"**Review:** FINDINGS {v['findings']} {v['reviewer']} {v['head']} {findings_file}"


def stamp(report: pathlib.Path, findings_file: pathlib.Path) -> str:
    """Write the newest verdict of `findings_file` into the report's header: replace its Review line, else insert one
    right under the Status line (the header is everything above the first `## ` heading, as plan_wave reads it)."""
    verdicts = [r for r in load(findings_file) if r["kind"] == "verdict"]
    if not verdicts:
        raise RecordError(f"{findings_file} holds no verdict: run `verdict` first")
    if " " in str(findings_file):
        raise RecordError(f"the findings file path must have no space (the stamp reads it as one word): {findings_file}")
    line = stamp_line(verdicts[-1], findings_file)
    text = report.read_text(encoding="utf-8")
    lines = text.splitlines()
    end = next((i for i, l in enumerate(lines) if i > 0 and l.startswith("## ")), len(lines))
    at = next((i for i in range(end) if REVIEW_LINE.match(lines[i].strip())), None)
    if at is not None:
        lines[at] = line
    else:
        status = next((i for i in range(end) if STATUS_LINE.match(lines[i].strip())), 0)
        lines.insert(status + 1, line)
    out = "\n".join(lines) + ("\n" if text.endswith("\n") else "")
    header = "\n".join(lines[:end + (0 if at is not None else 1)])
    got = plan_wave.parse_review(header)
    v = verdicts[-1]
    if got is None or got.verdict != v["verdict"] or got.sha != v["head"] or got.findings != v["findings"]:
        raise RecordError(f"the stamp would not read back through plan_wave.parse_review: {line!r} -> {got}")
    report.write_text(out, encoding="utf-8", newline="")
    return line


def show(path: pathlib.Path, rnd: int | None = None) -> list[str]:
    recs = load(path)
    rnd = rnd or max((r["round"] for r in recs if r["kind"] == "verdict"), default=None)
    return [f"{r['id']}  {r['file']}:{r['line']}  [{r['category']}] {r['summary']}\n    scenario: {r['failure_scenario']}"
            for r in recs if r["kind"] == "finding" and r["round"] == rnd]


def minutes(a: str, b: str) -> float:
    return (dt.datetime.fromisoformat(b) - dt.datetime.fromisoformat(a)).total_seconds() / 60


def summary(paths: list[pathlib.Path]) -> dict:
    """The measurement (kb/Work PB3027 item 5): per branch review file its rounds, findings and reviewer minutes; over
    all of them, findings per branch, the re-review rate (branches reviewed more than once) and the minutes."""
    files = sorted({f for p in paths for f in ([p] if p.is_file() else p.glob("review-w*.jsonl"))})
    per = []
    for f in files:
        recs = load(f)
        rounds = []
        for v in (r for r in recs if r["kind"] == "verdict"):
            s = of_round(recs, v["round"], "start")[0]
            rounds.append({"round": v["round"], "verdict": v["verdict"], "findings": v["findings"],
                           "minutes": round(minutes(s["at"], v["at"]), 1)})
        per.append({"file": str(f), "rounds": rounds, "open": any(r["kind"] == "start" for r in recs)
                    and len(rounds) < max(r["round"] for r in recs if r["kind"] == "start")})
    done = [p for p in per if p["rounds"]]
    n = len(done)
    return {"branches": n,
            "findings_per_branch": round(sum(r["findings"] for p in done for r in p["rounds"]) / n, 2) if n else 0.0,
            "re_review_rate": round(sum(1 for p in done if len(p["rounds"]) > 1) / n, 2) if n else 0.0,
            "clean": sum(1 for p in done if p["rounds"][-1]["verdict"] == "CLEAN"),
            "reviewer_minutes": round(sum(r["minutes"] for p in done for r in p["rounds"]), 1),
            "per_branch": per}


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test() -> int:
    fails: list[str] = []

    def check(what: str, ok: bool) -> None:
        if not ok:
            fails.append(what)

    def refused(fn, *a) -> bool:
        try:
            fn(*a)
        except RecordError:
            return True
        return False

    tmp = pathlib.Path(tempfile.mkdtemp(prefix="review-findings-selftest-"))
    try:
        f = tmp / "review-w9-a.jsonl"
        report = tmp / "w9a-PB1-report.md"
        report.write_text("# PB1 — implementer report\n\n**Status:** DONE · **branch:** `b`\n**diffstat:** x\n\n"
                          "## Reproduced?\n**Review:** below the header binds nothing\n", encoding="utf-8")
        check("arm: a finding before its round started is refused", refused(finding, f, 1, '{"file":"a"}'))
        start(f, 1, "b", "abc1234", "w9a-review-r1")
        check("arm: a second start of one round is refused", refused(start, f, 1, "b", "abc1234", "x"))
        check("arm: a reviewer label with a space is refused", refused(start, f, 2, "b", "abc1234", "two words"))
        check("arm: a finding without its scenario is refused",
              refused(finding, f, 1, json.dumps({"file": "a.py", "line": 3, "category": "correctness", "summary": "s"})))
        check("arm: a finding whose line is not an integer is refused",
              refused(finding, f, 1, json.dumps({"file": "a.py", "line": "3", "category": "c", "summary": "s",
                                                 "failure_scenario": "x"})))
        r = finding(f, 1, json.dumps({"file": "a.py", "line": 3, "category": "correctness", "summary": "off by one",
                                      "failure_scenario": "n=0 -> IndexError", "id": "forged", "round": 7}))
        check("arm: the tool numbers the finding, whatever the record claimed", r["id"] == "R1-1" and r["round"] == 1)
        check("arm: round 2 before round 1's verdict is refused", refused(start, f, 2, "b", "def5678", "x"))
        v = verdict(f, 1)
        check("arm: the verdict is computed from the findings on disk", v["verdict"] == "FINDINGS" and v["findings"] == 1)
        check("arm: a finding after the round's verdict is refused",
              refused(finding, f, 1, json.dumps({"file": "a", "line": 1, "category": "c", "summary": "s",
                                                 "failure_scenario": "x"})))
        line = stamp(report, f)
        rep = plan_wave.parse_report(report)
        check(f"arm: a FINDINGS stamp reads back through plan_wave: {line} -> {rep.review if rep else None}",
              rep is not None and rep.review == plan_wave.Review("FINDINGS", 1, "w9a-review-r1", "abc1234", str(f))
              and rep.status == "DONE")
        check("arm: the stamp goes right under the Status line",
              report.read_text(encoding="utf-8").splitlines()[3].startswith("**Review:** FINDINGS 1 "))
        check("arm: `show` lists the round's findings by id", show(f)[0].startswith("R1-1  a.py:3  [correctness]"))
        start(f, 2, "b2", "def5678", "w9a-review-r2")
        verdict(f, 2)
        line = stamp(report, f)
        text = report.read_text(encoding="utf-8")
        rep = plan_wave.parse_report(report)
        check("arm: a re-review REPLACES the stamp, never adds a second",
              text.count("**Review:** CLEAN") == 1 and "FINDINGS 1" not in text.split("## ")[0])
        check("arm: a CLEAN stamp covers exactly its head",
              rep.review.verdict == "CLEAN" and rep.review.covers("def5678aaaa") and not rep.review.covers("abc1234"))
        check("arm: the line below the header is untouched", "**Review:** below the header binds nothing" in text)
        check("arm: a FINDINGS stamp without its file does not parse",
              plan_wave.parse_review("**Review:** FINDINGS 2 x abc1234") is None)
        check("arm: a placeholder does not parse", plan_wave.parse_review("**Review:** pending") is None)
        bad = tmp / "bad.jsonl"
        bad.write_text('{"kind":"start","round":1}\nnot json\n', encoding="utf-8")
        check("arm: a malformed line is an error, never skipped (a lost finding would read CLEAN)", refused(load, bad))
        bad.unlink()
        s = summary([tmp])
        check(f"arm: the summary measures findings per branch and the re-review rate: {s}",
              s["branches"] == 1 and s["findings_per_branch"] == 1.0 and s["re_review_rate"] == 1.0 and s["clean"] == 1)
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    for f in fails:
        print(f"FAIL  {f}")
    print(f"=== REVIEW_FINDINGS SELF-TEST: {'GREEN' if not fails else f'RED ({len(fails)})'} ===")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    argv = sys.argv[1:] if argv is None else argv
    if argv[:1] == ["--self-test"]:
        return self_test()
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    sub = ap.add_subparsers(dest="cmd", required=True)
    p = sub.add_parser("start"); p.add_argument("file", type=pathlib.Path); p.add_argument("--round", type=int, required=True)
    p.add_argument("--branch", required=True); p.add_argument("--head", required=True); p.add_argument("--reviewer", required=True)
    p = sub.add_parser("finding"); p.add_argument("file", type=pathlib.Path); p.add_argument("--round", type=int, required=True)
    p.add_argument("json")
    p = sub.add_parser("verdict"); p.add_argument("file", type=pathlib.Path); p.add_argument("--round", type=int, required=True)
    p = sub.add_parser("stamp"); p.add_argument("report", type=pathlib.Path); p.add_argument("--findings", type=pathlib.Path, required=True)
    p = sub.add_parser("show"); p.add_argument("file", type=pathlib.Path); p.add_argument("--round", type=int)
    p = sub.add_parser("summary"); p.add_argument("paths", type=pathlib.Path, nargs="+")
    a = ap.parse_args(argv)
    try:
        if a.cmd == "start":
            start(a.file, a.round, a.branch, a.head, a.reviewer)
            print(f"started round {a.round} of {a.branch} at {a.head}")
        elif a.cmd == "finding":
            print(f"recorded {finding(a.file, a.round, a.json)['id']}")
        elif a.cmd == "verdict":
            v = verdict(a.file, a.round)
            print(f"=== BRANCH REVIEW: {v['verdict']}{'' if v['verdict'] == 'CLEAN' else ' ' + str(v['findings'])} "
                  f"(round {v['round']}, {v['branch']} at {v['head']}) ===")
        elif a.cmd == "stamp":
            print(stamp(a.report, a.findings))
        elif a.cmd == "show":
            print("\n".join(show(a.file, a.round)) or "(no findings)")
        else:
            print(json.dumps(summary(a.paths), indent=1))
    except RecordError as e:
        print(f"REFUSED: {e}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main())
