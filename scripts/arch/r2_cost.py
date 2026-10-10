#!/usr/bin/env python3
"""r2_cost.py — what an R2 launch plan will spend, from the measured cost of earlier batches, against the quota left;
and the split into launches that each fit (kb/Work PB2707; docs/rearchitecture/DESIGN-architecture-review.md §3 R2,
"Sizing a batch").

WHY. R2 batch 2 (13 shards x 5 dimensions = 65 pairs) was launched at 08:30 on 2026-10-08 with no estimate. By 10:02
the account's session meter read 90 %, the operator stopped the batch, and the finders had finished 52 of 65 pairs
while the skeptics (the only stage that decides anything) had not started: a batch that stops halfway pays its
finders and decides nothing. So every batch is sized when it is written (`r2_inputs.py --batch`) and every launch
when it is made (`r2_collect.py --launch`), and one that cannot finish is refused with its estimate and the split
the operator should launch instead, one call per part.

THE MODEL. One launch plan (`r2_collect.py`'s `plan`) is per pair: the files no finder has read, and per lens the
findings no skeptic has decided. Its cost is measured per unit of that work over the batches recorded in
`model_rules.json` `cost.r2.batches` (each written by `--measure` from the batch directory and its Workflow runs'
agent transcripts, never by hand):
  finders     tokens per shard line read in one dimension (a pair reads its shard once per dimension);
  skeptics    tokens per lens verdict; a line not yet read yields findings at the measured findings-per-line rate,
              and each finding takes one verdict per lens (three);
  null exam   tokens per examined null result; no recorded batch had one, so until one does an examination is
              charged as a finder pass over the shard (it reads the shard's largest files);
  clone pass  tokens of the one whole-codebase duplication pass, finder and skeptics together.
The tokens are the kinds `budget.py` counts (input, output, cache creation; `calibration.counted_token_kinds`), as
the transcripts record them. They become weekly points at the Opus rate (every R2 agent is the Opus role
`cobol-reviewer`) and session points at `calibration.session_pct_per_weekly_pct`.

THE ROOM (`budget.py`'s estimate for the running account): the session's is the soft stop less the session spent
less `--session-reserve` (what other work running beside the batch, such as the orchestrator loop, will spend); the
week's is the headroom of the allowance (`--borrow-days` raises it, never assumed: the owner's 2026-10-10 decision
of the whole weekly quota is `--borrow-days 6`); a fresh session window holds the soft stop less the reserve.

THE SPLIT. The shards, in the order given (the operator orders them, the small ones first), are packed greedily:
the first part into the room left now (or, when not even its first shard fits now, into the next window), each
later part into one fresh window, while the week has room; the parts past the week's room are listed as after the
weekly reset. A shard is never split (its pairs share an input and a duplication lens); a shard larger than one
window is its own part, marked so.

Usage:
    python scripts/arch/r2_cost.py --measure <batch dir> <workflow subagent dir>...   # the batch's record, as JSON
    python scripts/arch/r2_cost.py --self-test
The estimate itself is printed by `r2_inputs.py --batch` and `r2_collect.py --launch` (their `--budget-json`,
`--borrow-days` and `--session-reserve` come from `add_budget_args` here).
"""
from __future__ import annotations

import argparse
import json
import re
import sys
import tempfile
from collections import defaultdict
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
ORCH = REPO / "scripts" / "orchestrator"
LENSES = ("site", "rule", "scenario")
GLOBAL_PAIR = "global--duplication"
REFUSED = 3   # the exit code of a refused batch or launch (argparse owns 2)


# ── the measured rates ───────────────────────────────────────────────────────────────────────────────────────────
def rules() -> dict:
    sys.path.insert(0, str(ORCH))
    import coord  # noqa: E402
    return coord.rules()


def rates(rl: dict) -> dict:
    """Tokens per unit of work over every recorded batch of `rl` (model_rules.json)."""
    bs = rl["cost"]["r2"]["batches"]
    line_dims = sum(b["line_dimensions"] for b in bs)
    verdicts = sum(b["verdicts"] for b in bs)
    nulls = sum(b["nulls"] for b in bs)
    dup = [b["duplication_pass_tokens"] for b in bs if b.get("duplication_pass_tokens")]
    finder = sum(b["finder_tokens"] for b in bs) / line_dims
    return {"finder_per_line": finder,
            "verdict": sum(b["skeptic_tokens"] for b in bs) / verdicts,
            "findings_per_line": sum(b["findings"] for b in bs) / line_dims,
            # unmeasured until a batch examines a null: a finder pass over the shard (the module docstring)
            "null_exam": (sum(b["null_tokens"] for b in bs) / nulls) if nulls else None,
            "duplication_pass": sum(dup) / len(dup) if dup else None,
            "batches": [b["batch"] for b in bs]}


def plan_work(plan: dict) -> dict:
    """The work of a launch plan, per shard (the global clone pass under the key `global`)."""
    shards = plan["shards"]
    work = defaultdict(lambda: {"lines": 0, "verdicts": 0, "nulls": 0, "null_lines": 0, "duplication_pass": 0, "pairs": 0})
    for e in plan["pairs"]:
        w = work[e["shard"] or "global"]
        w["pairs"] += 1
        w["verdicts"] += sum(len(e["undecided"][lens]) for lens in LENSES)
        if e["slug"] == GLOBAL_PAIR:
            w["duplication_pass"] += 0 if e["done"] else 1
            continue
        w["lines"] += sum(n for _, n in e["unread"])
        if not e["findings"] and not e["unread"] and not e["null_checked"]:
            w["nulls"] += 1
            w["null_lines"] += shards[e["shard"]]["lines"]
    return dict(work)


def work_tokens(w: dict, rt: dict) -> float:
    """Tokens of one shard's (or the clone pass's) work: the finders, the verdicts of the findings already on disk,
    the verdicts of the findings the unread lines are expected to yield, the null exams and the clone pass."""
    t = w["lines"] * rt["finder_per_line"]
    t += (w["verdicts"] + 3 * w["lines"] * rt["findings_per_line"]) * rt["verdict"]
    if w["nulls"]:
        t += w["nulls"] * rt["null_exam"] if rt["null_exam"] else w["null_lines"] * rt["finder_per_line"]
    if w["duplication_pass"]:
        if rt["duplication_pass"] is None:
            raise SystemExit("no recorded batch measured the clone pass: record one with --measure before sizing it")
        t += w["duplication_pass"] * rt["duplication_pass"]
    return t


def points(tokens: float, rl: dict) -> tuple[float, float]:
    """-> (weekly points, session points) of Opus tokens."""
    cal = rl["calibration"]
    week = tokens / cal["tokens_per_point"]["opus"]
    return week, week * cal["session_pct_per_weekly_pct"]


# ── the room left ────────────────────────────────────────────────────────────────────────────────────────────────
def add_budget_args(ap: argparse.ArgumentParser) -> None:
    ap.add_argument("--budget-json", type=Path, help="a budget.py --json output to size against (a test seam); "
                                                     "default: budget.py's estimate for the running account now")
    ap.add_argument("--borrow-days", type=int, default=0,
                    help="days of later weekly allowance the owner allowed (budget.py's; never assumed)")
    ap.add_argument("--session-reserve", type=float, default=0.0,
                    help="session points kept for other work running beside the batch (the orchestrator loop)")


def budget_from(a) -> dict:
    if a.budget_json:
        return json.loads(a.budget_json.read_text(encoding="utf-8"))
    sys.path.insert(0, str(ORCH))
    import budget  # noqa: E402
    return budget.current(borrow_days=a.borrow_days)


def room(b: dict, reserve: float) -> dict:
    window = float(b["session_soft_stop_pct"]) - reserve
    return {"session": window - float(b["session_est_pct"]), "window": window, "week": float(b["headroom_pct"]),
            "session_spent": float(b["session_est_pct"]),
            "session_reset": b.get("resume_at") or (b.get("anchor") or {}).get("session_reset")}


# ── the estimate and the split ───────────────────────────────────────────────────────────────────────────────────
def size(plan: dict, rl: dict, b: dict, reserve: float) -> dict:
    """The estimate of `plan` against the budget `b`; `fits` when it can finish in the session's room AND the week's,
    else `parts`: the launches, in the plan's shard order, that each fit."""
    rt = rates(rl)
    work = plan_work(plan)
    order = [s for s in plan["shard_order"] if s in work] + (["global"] if "global" in work else [])
    units = [(s, work_tokens(work[s], rt), work[s]["pairs"]) for s in order]
    tokens = sum(t for _, t, _ in units)
    week, session = points(tokens, rl)
    r = room(b, reserve)
    est = {"tokens": round(tokens), "weekly_points": round(week, 2), "session_points": round(session, 2),
           "pairs": sum(n for _, _, n in units), "room": {k: (round(v, 2) if isinstance(v, float) else v) for k, v in r.items()},
           "rates": {k: (round(v, 6) if isinstance(v, float) else v) for k, v in rt.items()}, "reserve": reserve}
    est["fits"] = session <= r["session"] and week <= r["week"]
    est["parts"] = [] if est["fits"] else split(units, rl, r)
    return est


NOW, NEXT_WINDOW, AFTER_SESSION, AFTER_WEEK = "now", "next session window", "after the session reset", "after the weekly reset"


def split(units, rl: dict, r: dict) -> list[dict]:
    """Pack the (shard, tokens, pairs) units, in order, into launches: the first into the room left now (or the next
    window when not even its first shard fits now), each later one into a fresh window, while the week has room; the
    rest after the weekly reset."""
    parts, week_left = [], r["week"]
    for s, t, n in units:
        wk, ss = points(t, rl)
        cur = parts[-1] if parts else None
        over_week = (cur is not None and cur["when"] == AFTER_WEEK) or wk > week_left
        if cur is None or cur["session_points"] + ss > cur["cap"] or (over_week and cur["when"] != AFTER_WEEK):
            if over_week:
                when = AFTER_WEEK
            elif cur is None:
                when = NOW if ss <= r["session"] else AFTER_SESSION
            else:
                when = NEXT_WINDOW
            cur = {"shards": [], "tokens": 0, "weekly_points": 0.0, "session_points": 0.0, "pairs": 0, "when": when,
                   "cap": r["session"] if when == NOW else r["window"], "over_one_window": False}
            parts.append(cur)
        cur["shards"].append(s)
        cur["tokens"] += round(t)
        cur["weekly_points"] += wk
        cur["session_points"] += ss
        cur["pairs"] += n
        cur["over_one_window"] = cur["session_points"] > r["window"]
        if cur["when"] != AFTER_WEEK:
            week_left -= wk
    for p in parts:
        p["weekly_points"], p["session_points"] = round(p["weekly_points"], 2), round(p["session_points"], 2)
        del p["cap"]
    return parts


def report(est: dict, what: str, command) -> str:
    """The estimate as printed; `command(part, i)` renders the one command that writes or launches part i."""
    r = est["room"]
    head = ("%s: %d pair(s), ≈ %.1f M tokens = %.2f weekly points = %.1f session points; room: session %.1f (the "
            "soft stop less %.1f spent%s), week %.1f%s" % (
                what, est["pairs"], est["tokens"] / 1e6, est["weekly_points"], est["session_points"], r["session"],
                r["session_spent"], ", less a reserve of %.1f" % est["reserve"] if est["reserve"] else "", r["week"],
                " — FITS" if est["fits"] else " — DOES NOT FIT: the parts below each fit, one command each"))
    lines = [head, "  rates (batches %s): %.1f tokens per shard line per dimension; %.0f per lens verdict; %.5f "
             "findings per line%s" % (",".join(est["rates"]["batches"]), est["rates"]["finder_per_line"],
                                      est["rates"]["verdict"], est["rates"]["findings_per_line"],
                                      "" if est["rates"]["null_exam"] else "; a null exam unmeasured: a finder pass")]
    for i, p in enumerate(est["parts"]):
        lines.append("  part %d (%s%s): %d pair(s) of %d shard(s), ≈ %.1f M tokens = %.2f weekly = %.1f session points"
                     % (i + 1, p["when"] + (" at " + r["session_reset"] if p["when"] == AFTER_SESSION and r["session_reset"]
                                            else ""), ", OVER ONE WINDOW alone" if p["over_one_window"] else "", p["pairs"],
                        len(p["shards"]), p["tokens"] / 1e6, p["weekly_points"], p["session_points"]))
        lines.append("    " + command(p, i))
    return "\n".join(lines)


# ── measuring a finished batch ───────────────────────────────────────────────────────────────────────────────────
def transcript_tokens(path: Path, kinds=("input_tokens", "output_tokens", "cache_creation_input_tokens")) -> int:
    """The counted tokens of one agent transcript: per API message its final usage (a streamed message is logged
    more than once with a growing usage, so the largest one per message id)."""
    per = {}
    for i, line in enumerate(path.read_text(encoding="utf-8").splitlines()):
        try:
            d = json.loads(line)
        except json.JSONDecodeError:
            continue
        m = d.get("message")
        u = m.get("usage") if isinstance(m, dict) else None
        if not u:
            continue
        n = sum(int(u.get(k) or 0) for k in kinds)
        key = m.get("id") or i   # a message with no id is its own message, never merged with another
        per[key] = max(per.get(key, 0), n)
    return sum(per.values())


def agent_kind(label: str) -> str:
    if GLOBAL_PAIR in label:
        return "duplication"
    return {"find": "finder", "finish": "finder", "null": "null", "skeptic": "skeptic"}.get(label.split(":")[0], "other")


def measure(out: Path, wf_dirs: list[Path]) -> dict:
    """The batch's record for model_rules.json `cost.r2.batches`: its work from the batch directory, its tokens by
    agent kind from the agent transcripts of every Workflow run that worked on it."""
    sys.path.insert(0, str(HERE))
    import r2_collect  # noqa: E402
    args = json.loads((out / "batch-args.json").read_text(encoding="utf-8"))
    pairs, votes = r2_collect.scan(out)
    shard_pairs = {n: p for n, p in pairs.items() if n != GLOBAL_PAIR}
    ids = {fid for p in shard_pairs.values() for fid in p["findings"]}
    tok = defaultdict(int)
    agents = 0
    for d in wf_dirs:
        for meta in sorted(d.glob("agent-*.meta.json")):
            t = meta.with_name(meta.name.replace(".meta.json", ".jsonl"))
            if not t.exists():
                continue
            tok[agent_kind(json.loads(meta.read_text(encoding="utf-8")).get("description", ""))] += transcript_tokens(t)
            agents += 1
    if tok.get("other"):
        raise SystemExit("agents of no R2 kind in %s: not an R2 batch's runs" % ", ".join(map(str, wf_dirs)))
    return {"batch": str(args["batch"]), "pin": args["pinCommit"][:12],
            "line_dimensions": sum(s["lines"] for s in args["shards"]) * len(args["dimensions"]),
            "findings": len(ids), "verdicts": sum(len(v) for f, v in votes.items() if f in ids),
            "nulls": sum(1 for p in shard_pairs.values() if p["null_checked"]),
            "finder_tokens": tok["finder"], "skeptic_tokens": tok["skeptic"], "null_tokens": tok["null"],
            "duplication_pass_tokens": tok["duplication"] or None, "workflow_runs": len(wf_dirs), "agents": agents}


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def fixture_rules() -> dict:
    """Round-number rules for the self-tests (here and in r2_collect.py): 100 tokens per line, 10,000 per verdict,
    0.01 findings per line, a 50,000-token clone pass, 1 M tokens per weekly point, 4 session points per weekly one."""
    rec = {"line_dimensions": 1000, "findings": 10, "verdicts": 30, "nulls": 0, "finder_tokens": 100_000,
           "skeptic_tokens": 300_000, "null_tokens": 0}
    return {"calibration": {"tokens_per_point": {"opus": 1_000_000}, "session_pct_per_weekly_pct": 4.0},
            "cost": {"r2": {"batches": [dict(rec, batch="1", duplication_pass_tokens=50_000),
                                        dict(rec, batch="2", duplication_pass_tokens=None)]}}}


def self_test() -> int:
    ok = True

    def arm(name, cond):
        nonlocal ok
        print(("PASS  " if cond else "FAIL  ") + name)
        ok = ok and bool(cond)

    rl = fixture_rules()
    rt = rates(rl)
    arm("rates pool every recorded batch (tokens per line, per verdict, findings per line, the clone pass)",
        rt["finder_per_line"] == 100 and rt["verdict"] == 10_000 and rt["findings_per_line"] == 0.01
        and rt["duplication_pass"] == 50_000 and rt["null_exam"] is None)
    shard = lambda n: {"name": n, "lines": 1000, "input": "i", "files": 1}  # noqa: E731
    fresh = lambda s: {"slug": s + "--code", "shard": s, "dim": "code", "unread": [["f", 1000]], "finders": 0,  # noqa: E731
                       "findings": 0, "null_checked": False, "undecided": {lens: [] for lens in LENSES}}
    plan = {"shards": {s: shard(s) for s in "abcd"}, "shard_order": list("abcd"), "pairs": [fresh(s) for s in "abcd"]}
    # one pair of 1,000 unread lines: 100,000 finder tokens + 10 expected findings x 3 verdicts x 10,000 = 400,000
    arm("a fresh pair costs its finders and the verdicts of its expected findings",
        work_tokens(plan_work(plan)["a"], rt) == 400_000)
    decided = dict(fresh("a"), unread=[], findings=2, undecided={"site": ["x"], "rule": [], "scenario": []})
    arm("a relaunch costs only what is undecided (one verdict, no finder)",
        work_tokens(plan_work({"shards": plan["shards"], "pairs": [decided]})["a"], rt) == 10_000)
    null = dict(fresh("a"), unread=[], findings=0)
    arm("an unexamined null is charged a finder pass over its shard until one is measured",
        work_tokens(plan_work({"shards": plan["shards"], "pairs": [null]})["a"], rt) == 100_000)
    b = {"session_soft_stop_pct": 97, "session_est_pct": 90.0, "headroom_pct": 50.0, "resume_at": None,
         "anchor": {"session_reset": "2026-10-10T15:10:00-07:00"}}
    # each shard: 0.4 weekly points = 1.6 session points; 4 shards = 6.4 session points against a room of 7
    est = size(plan, rl, b, 0.0)
    arm("a plan inside the session's and the week's room FITS", est["fits"] and not est["parts"])
    est = size(plan, rl, b, 2.0)
    arm("an over-budget plan is refused WITH its estimate and a split: the room now, then whole windows",
        not est["fits"] and est["session_points"] == 6.4 and [p["shards"] for p in est["parts"]] == [list("abc"), ["d"]]
        and [p["when"] for p in est["parts"]] == ["now", "next session window"])
    text = report(est, "batch t", lambda p, i: "launch %s" % ",".join(p["shards"]))
    arm("the refusal prints the estimate, the room and one command per part",
        "DOES NOT FIT" in text and "6.4 session points" in text and "launch a,b,c" in text and "launch d" in text)
    est = size(plan, rl, dict(b, session_est_pct=96.5), 0.0)
    arm("when not even the first shard fits now, the first part waits for the session reset",
        est["parts"][0]["when"] == "after the session reset" and est["parts"][0]["shards"] == list("abcd"))
    est = size(plan, rl, dict(b, headroom_pct=0.9), 0.0)
    arm("the parts past the week's room are listed after the weekly reset",
        [p["when"] for p in est["parts"]] == ["now", "after the weekly reset"] and est["parts"][0]["shards"] == list("ab"))
    big = dict(plan, shards=dict(plan["shards"], a=dict(shard("a"), lines=100_000)), pairs=[dict(fresh("a"), unread=[["f", 100_000]])])
    est = size(big, rl, dict(b, session_est_pct=0.0), 0.0)
    arm("a shard larger than one window is its own part, marked so", est["parts"][0]["over_one_window"])
    with tempfile.TemporaryDirectory() as tmp:
        t = Path(tmp) / "agent-x.jsonl"
        t.write_text("\n".join(json.dumps(x) for x in (
            {"message": {"id": "m1", "usage": {"input_tokens": 1, "output_tokens": 5, "cache_creation_input_tokens": 10,
                                                "cache_read_input_tokens": 999}}},
            {"message": {"id": "m1", "usage": {"input_tokens": 1, "output_tokens": 50, "cache_creation_input_tokens": 10}}},
            {"message": {"id": "m2", "usage": {"input_tokens": 2, "output_tokens": 3, "cache_creation_input_tokens": 0}}},
            {"type": "user"})) + "\n{broken\n", encoding="utf-8")
        arm("a transcript's counted tokens: per message its largest usage, cache reads not counted",
            transcript_tokens(t) == 61 + 5)
    arm("agents are classed by their label", [agent_kind(x) for x in (
        "find:a--code:f1", "finish:a--code:f2", "null:a--code", "skeptic:a--code:c1:site", "find:global--duplication",
        "skeptic:global--duplication:c1:rule")] == ["finder", "finder", "null", "skeptic", "duplication", "duplication"])
    live = rates(rules())
    arm("model_rules.json records at least one measured batch with a clone pass",
        live["batches"] and live["duplication_pass"] and live["finder_per_line"] > 0)
    print("=== R2 COST SELF-TEST: %s ===" % ("PASS" if ok else "FAIL"))
    return 0 if ok else 1


def main() -> int:
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser()
    ap.add_argument("--measure", nargs="+", metavar="DIR", help="<batch dir> <workflow subagent dir>...")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args()
    if a.self_test:
        return self_test()
    if a.measure and len(a.measure) >= 2:
        print(json.dumps(measure(Path(a.measure[0]), [Path(p) for p in a.measure[1:]]), indent=1))
        return 0
    ap.error("--measure <batch dir> <workflow subagent dir>... or --self-test")
    return 2


if __name__ == "__main__":
    sys.exit(main())
