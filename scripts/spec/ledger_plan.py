#!/usr/bin/env python3
"""What remains to v1.0, measured off the register: the plan half of the completion ledger.

⛔ WHY THIS EXISTS. The owner's live status page (`gen_ledger.py`) is a derived view of the register and the inventory.
This module measures the plan's part of it and writes nothing:

  * WHAT REMAINS (kb/Work PB2912, owner 2026-10-10: "update the ledger to contain this info and track it. Remove
    unneeded info from ledger"): the known compiler defects and analyses, the GAP rows and the notes that hold them,
    the rest of v1.0 (its gates), and the work the owner HELD until zero GAP and zero known compiler defects (kb/Work
    PB2911). Each bucket is counted with its harm breakdown, its notes ready / waiting / blocked, and the LANE that
    plans each note: the fix lane, a campaign lane, held, or none. ⛔ A note no lane plans is the defect that kept GAP
    flat for days (PB2911: 53 open notes held every GAP row, and the fix lane plans only `.agent-fleet.json`'s
    population), so `remaining()` names every such note and the page flags it loudly.
  * AN ESTIMATE PER BUCKET from measured inputs only: groups from the fix lane's own clustering (`fix_clusters.py`,
    through `plan_wave.run_fix_clusters`) and the planner's group cap, waves from the planner's groups per wave,
    weekly points from `model_rules.json`'s cost constants (`plan_wave.group_cost` / `lander_cost`, imported) and from
    the newest meter-measured burn, hours from the batched trains' landing rate (`train_measure.landing_rate`). A
    missing input makes its figure UNMEASURED (None), never a guessed number.
  * THE REGISTER SERIES the trend file tracks beside GAP (`register_counts`): open compiler notes, defects, wrong
    answers, crashes and GAP-holder notes, one function for the current tree and for any commit's own tree.
  * The external repository's slices (found by title) and the v1.0 gates (`GATES`).

⭐ EVERY STATUS IS READ, NEVER WRITTEN. Cluster membership is work.py's `cluster_members`, a note's status and flags are
its frontmatter, `open_blockers` decides waiting, the fix lane's population is `.agent-fleet.json`'s `kind` and
`skip_flag` (the file fix_clusters.py and plan_wave.py read), and the lanes the loop runs are the owner's decision
PB2911 (`CAMPAIGNS`, `HELD`). A note filed tomorrow in any of those shapes is counted with no edit here.

⛔ THE HAND-KEPT STRUCTURES ARE SMALL AND SELF-TESTED: the campaign and held clusters, the v1.0 gates (a note id, a
title rule for a gate whose note is not filed yet, or a GAP threshold). `problems()` fails on a structure id the
register lacks, a lane that names no note, a slice number claimed twice, and bucket counts that disagree with
`register_counts`.

    python scripts/spec/ledger_plan.py --self-test   # every arm on planted registers, then the real one
                                                     # (the gate's audits and CI's `audits` job run this)
"""
from __future__ import annotations

import argparse
import collections
import datetime
import json
import math
import pathlib
import re
import sys
import tempfile

REPO = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.insert(0, str(REPO / "scripts" / "orchestrator"))
import work  # noqa: E402  (the register's one reader)

#: The fix lane's population: `.agent-fleet.json`'s `kind` and `skip_flag`, the file fix_clusters.py reads.
FLEET = REPO / ".agent-fleet.json"
#: A skip flag that is a STATE, not a population rule: a blocked defect is still the fix lane's, waiting.
STATE_FLAGS = ("blocked",)

#: The owner's decision of 2026-10-10 (kb/Work PB2911): the loop runs these clusters as campaign lanes beside the fix
#: lane (`orchestrate.ps1 -Cluster PB2911,PB1086`) ...
DECISION = "PB2911"
CAMPAIGNS = (("PB2911", "the GAP campaign"), ("PB1086", "the external repository"))
#: ... and HOLDS these until zero GAP and zero known compiler defects.
HELD = (("PB2119", "the Delete program"), ("PB1754", "the architecture review"))
#: The campaign every open GAP-holding note joins (PB2911 "How it runs": a note that newly claims a GAP row joins it).
GAP_CAMPAIGN = "PB2911"

FIX = "fix"
NO_LANE = "none"

#: An external-repository slice: `PB2099 — external repository slice 3: The host entry …`.
EXTERNAL_SLICE = re.compile(r"\A\S+ — external repository slice (?P<n>\d+)\b")

#: v1.0's gates, in order: (label, how it is measured). ("note", id): the note's status. ("title", regex): the note
#: whose title matches, once one is filed. ("gap", n): passed when GAP <= n. v1.0 is zero GAP (D13) plus P15 Cut 3 and
#: the §4.2.16 user documentation (PB2911, "Why a campaign"); the review's gates (R1, the oracle, D10) all landed and
#: the review itself is held, so they are no longer the page's.
GATES = (
    ("Zero GAP", ("gap", 0)),
    ("P15 Cut 3, the runtime namespace flip", ("title", r"\A\S+ — P15 Cut 3\b")),
    ("The §4.2.16 user documentation", ("note", "PB1610")),
)

#: Verdicts whose GAP rows wait only on a witness (gen_ledger.RESOLVING's twin is the schema's `resolves` flag).
RESOLVING = ("CONFORMS", "DOCUMENTED-NON-SUPPORT")


def finished(status: str) -> bool:
    return status in work.TERMINAL_STATUSES


def live(items: list[dict]) -> list[dict]:
    return [i for i in items if not finished(str(i.get("status")))]


def fleet_population(path: pathlib.Path = FLEET) -> tuple[str, tuple[str, ...]]:
    """(kind, population skip flags) of the fix lane, read from `.agent-fleet.json`."""
    cfg = json.loads(path.read_text(encoding="utf-8"))
    return str(cfg.get("kind", "")), tuple(f for f in cfg.get("skip_flag", []) if f not in STATE_FLAGS)


def note_state(it: dict, by_id: dict[str, dict]) -> str:
    """`waiting` on an open blocker note, `blocked` on something that is not a note (status owner or blocked, or the
    `blocked` flag with no open blocker), `ready` otherwise: work.py's cluster_order reading, for any note."""
    if work.open_blockers(it, by_id):
        return "waiting"
    if it.get("status") in ("owner", "blocked") or it.get("blocked"):
        return "blocked"
    return "ready"


def lanes_of(it: dict, fleet: tuple[str, tuple[str, ...]]) -> list[str]:
    """Every lane that plans the note: FIX, a campaign lead, `held:<lead>`; [] when none does."""
    kind, skip = fleet
    out = [FIX] if it.get("kind") == kind and not any(it.get(f) for f in skip) else []
    clusters = it.get("cluster") if isinstance(it.get("cluster"), list) else []
    out += [lead for lead, _ in CAMPAIGNS if lead in clusters]
    out += [f"held:{lead}" for lead, _ in HELD if lead in clusters]
    return out


def planner(lanes: list[str]) -> str:
    """The lane a note's estimate is charged to: the fix lane, else its first campaign, else held, else none."""
    active = [x for x in lanes if not x.startswith("held:")]
    return active[0] if active else (lanes[0] if lanes else NO_LANE)


def register_counts(items: list[dict], gap_rows: set[str]) -> dict | None:
    """The register's series for the trend file, from `items` (any tree's notes) and that tree's GAP row ids: open
    compiler notes (not `process_only`), of them `kind: defect`, wrong answers and crashes, and the open notes that hold
    a GAP row (None when no note of that tree carries `inventory_rows` yet). None when the tree has no register."""
    if not items:
        return None
    comp = [i for i in live(items) if not i.get("process_only")]
    holders = (sum(1 for i in live(items) if set(i.get("inventory_rows") or []) & gap_rows)
               if any("inventory_rows" in i for i in items) else None)
    return {"compiler_open": len(comp), "defects": sum(1 for i in comp if i.get("kind") == "defect"),
            "wrong_answer": sum(1 for i in comp if i.get("wrong_answer")),
            "crashes": sum(1 for i in comp if i.get("crashes")), "gap_holders": holders}


REGISTER_KEYS = ("compiler_open", "defects", "wrong_answer", "crashes", "gap_holders")


def remaining(items: list[dict], rows: list[dict], gap: int, fleet: tuple[str, tuple[str, ...]]) -> dict:
    """The buckets of what remains to v1.0, each with its notes, harm, states and lanes."""
    by_id = {i.get("id"): i for i in items}
    open_items = live(items)
    gap_rows = {r["rule-id"]: r for r in rows if r["state"] == "GAP"}
    lanes = {str(i.get("id")): lanes_of(i, fleet) for i in open_items}

    def bucket(notes: list[dict]) -> dict:
        ids = [str(i.get("id")) for i in notes]
        lane_n = collections.Counter(x for nid in ids for x in (lanes[nid] or [NO_LANE]))
        return {"ids": ids, "n": len(ids), "kind": collections.Counter(str(i.get("kind")) for i in notes),
                "states": collections.Counter(note_state(i, by_id) for i in notes), "lanes": lane_n,
                "no_lane": [nid for nid in ids if not lanes[nid]],
                "no_lane_kind": collections.Counter(str(i.get("kind")) for i in notes if not lanes[str(i.get("id"))]),
                "process_only": sum(1 for i in notes if i.get("process_only"))}

    comp = [i for i in open_items if not i.get("process_only")]
    compiler = bucket(comp)
    compiler["harm"] = collections.Counter(f for i in comp for f in (*work.HARM_FLAGS, "silent") if i.get(f))
    compiler["defect_harm"] = collections.Counter(f for i in comp if i.get("kind") == "defect"
                                                  for f in (*work.HARM_FLAGS, "silent") if i.get(f))

    holders = [i for i in open_items if set(i.get("inventory_rows") or []) & set(gap_rows)]
    gapb = bucket(holders)
    claimed = {r for i in holders for r in (i.get("inventory_rows") or [])}
    gapb.update(rows=len(gap_rows), verdicts=collections.Counter(r["verdict"] or "unadjudicated" for r in gap_rows.values()),
                witness_owed=sum(1 for r in gap_rows.values() if r["verdict"] in RESOLVING),
                unclaimed=sorted(set(gap_rows) - claimed),
                outside_campaign=[str(i.get("id")) for i in holders
                                  if GAP_CAMPAIGN not in (i.get("cluster") if isinstance(i.get("cluster"), list) else [])],
                biggest=sorted(((len(set(i.get("inventory_rows") or []) & set(gap_rows)), str(i.get("id")))
                                for i in holders), reverse=True)[:1])

    held_notes: dict[str, dict] = {}
    held = []
    for lead, name in HELD:
        members = work.cluster_members(items, lead)
        held_notes.update({str(i.get("id")): i for i in live(members)})
        held.append({"lead": lead, "name": name, "total": len(members), "open": len(live(members))})
    heldb = bucket(list(held_notes.values()))
    heldb["clusters"] = held

    # v1.0's gates, each with the lanes that plan its note: a gate note that only a HELD cluster or no lane plans
    # waits for nobody's wave, which is the same defect as a GAP holder no lane plans (P15 Cut 3, PB2349, sits in
    # the held Delete program's cluster).
    gates = []
    for g in measure(items, gap)["gates"]:
        gl = lanes.get(g["id"], []) if g.get("id") else []
        gates.append({**g, "lanes": gl, "unplanned": bool(g.get("id")) and not finished(g["status"])
                      and not any(not x.startswith("held:") for x in gl)})

    return {"compiler": compiler, "gap": gapb, "held": heldb, "lanes": lanes, "gates": gates,
            "counts": register_counts(items, set(gap_rows)), "gap_now": gap}


# ── the estimate ────────────────────────────────────────────────────────────────────────────────────────────────

def chunks(n: int, cap: int) -> list[int]:
    return [cap] * (n // cap) + ([n % cap] if n % cap else [])


def group_sizes(ids: list[str], lanes: dict[str, list[str]], fix_groups: dict | None, cap: int,
                release_held: bool = False) -> tuple[list[int] | None, list[str]]:
    """(the group sizes the planners would form for `ids`, the ids no lane plans). Fix-lane notes take fix_clusters'
    own groups (None when that input is missing); campaign notes, and held notes when `release_held`, take the
    planner's cap (a campaign clusters by sites, so the count is a lower bound)."""
    by_lane: dict[str, list[str]] = collections.defaultdict(list)
    for nid in ids:
        p = planner(lanes.get(nid, []))
        if p.startswith("held:") and not release_held:
            continue
        by_lane[p].append(nid)
    unplanned = by_lane.pop(NO_LANE, [])
    sizes: list[int] = []
    fix = set(by_lane.pop(FIX, []))
    if fix:
        if fix_groups is None:
            return None, unplanned
        covered: set[str] = set()
        for c in fix_groups.get("clusters", []):
            k = {n["id"] for n in c.get("notes", [])} & fix
            if k:
                sizes.append(len(k))
                covered |= k
        unsited = set(fix_groups.get("unsited", [])) & fix
        sizes += [1] * len(unsited)
        sizes += chunks(len(fix - covered - unsited), cap)
    for notes in by_lane.values():
        sizes += chunks(len(notes), cap)
    return sizes, unplanned


def estimate(sizes: list[int] | None, inputs: dict) -> dict:
    """Groups, waves, planned weekly points, loop hours and points at the measured burn; None where an input is
    missing (the page prints UNMEASURED)."""
    rules = inputs.get("rules")
    if sizes is None or rules is None:
        return {"groups": None, "waves": None, "points": None, "hours": None, "burn_points": None}
    import plan_wave  # noqa: PLC0415  (the planner's cost rule, never a copy)
    w = rules["wave"]
    groups = len(sizes)
    points = (sum(plan_wave.group_cost(w["implementer_model"], k, rules) for k in sizes)
              + plan_wave.lander_cost(rules) * math.ceil(groups / w["train_size"]))
    rate, burn = inputs.get("rate"), inputs.get("burn")
    hours = groups / rate["per_h"] if rate and rate["per_h"] > 0 else None
    return {"groups": groups, "waves": math.ceil(groups / w["max_groups"]), "points": points, "hours": hours,
            "burn_points": hours * burn["per_h"] if hours is not None and burn else None}


def bucket_estimates(rem: dict, inputs: dict) -> dict:
    rules = inputs.get("rules")
    cap = rules["wave"]["max_notes_per_group"] if rules else 1
    out = {}
    for key, release in (("compiler", False), ("gap", False), ("held", True)):
        sizes, unplanned = group_sizes(rem[key]["ids"], rem["lanes"], inputs.get("fix_groups"), cap, release)
        out[key] = {**estimate(sizes, inputs), "unplanned": len(unplanned),
                    "held_out": 0 if release else sum(1 for nid in rem[key]["ids"]
                                                     if planner(rem["lanes"][nid]).startswith("held:"))}
    return out


def measure_burn(readings: list[dict], week_start: datetime.datetime) -> dict | None:
    """Weekly points per hour since the week's first meter reading: (newest - first) over the hours between them.
    None with fewer than two readings this week or no movement (a burn of zero is not a measured rate)."""
    rs = sorted((r for r in readings if datetime.datetime.fromisoformat(r["noted_at"]) >= week_start),
                key=lambda r: datetime.datetime.fromisoformat(r["noted_at"]))
    if len(rs) < 2:
        return None
    a, b = rs[0], rs[-1]
    h = (datetime.datetime.fromisoformat(b["noted_at"]) - datetime.datetime.fromisoformat(a["noted_at"])).total_seconds() / 3600
    d = float(b["weekly_pct"]) - float(a["weekly_pct"])
    if h <= 0 or d <= 0:
        return None
    return {"per_h": d / h, "from_pct": float(a["weekly_pct"]), "to_pct": float(b["weekly_pct"]), "hours": h,
            "from": a["noted_at"], "to": b["noted_at"]}


def estimate_inputs() -> dict:
    """The estimate's measured inputs on this machine; each None when it cannot be read here (CI, a fresh clone)."""
    import account  # noqa: PLC0415
    import budget  # noqa: PLC0415
    import coord  # noqa: PLC0415
    import plan_wave  # noqa: PLC0415
    import train_measure  # noqa: PLC0415
    rules = coord.rules()
    fix_groups = None
    if plan_wave.FIX_CLUSTERS.exists():
        with tempfile.TemporaryDirectory(prefix="ledger-fc-") as d:
            fix_groups = plan_wave.run_fix_clusters("open", pathlib.Path(d) / "clusters-open.json", rules)
    d = coord.coord_path()
    rate = None
    recs = [r for r in train_measure.read_all(d / train_measure.FILE) if r.get("gating") == "batched"] if d.is_dir() else []
    if recs:
        landed, window_h, per_h = train_measure.landing_rate(recs)
        rate = {"per_h": per_h, "landed": landed, "window_h": window_h, "trains": len(recs), "gating": "batched"}
    acct = account.current()
    readings = [r for r in (coord.read_json(d / "readings.json", []) if d.is_dir() else []) if r.get("account") == acct.name]
    now = datetime.datetime.now(datetime.timezone.utc)
    burn = measure_burn(readings, budget.week_start(now, acct.weekly_reset))
    if burn:
        burn["account"] = acct.name
    return {"rules": rules, "fix_groups": fix_groups, "rate": rate, "burn": burn}


# ── the slices and the gates ────────────────────────────────────────────────────────────────────────────────────

def measure(items: list[dict], gap: int) -> dict:
    """The external repository's slices and v1.0's gates, read off `items` and the inventory's `gap`."""
    by_id = {i.get("id"): i for i in items}

    def status(nid: str) -> str:
        return str(by_id.get(nid, {}).get("status", "missing"))

    def title(nid: str) -> str:
        t = str(by_id.get(nid, {}).get("title", ""))
        return t.split(" — ", 1)[1] if " — " in t else t

    slices = sorted(((int(m["n"]), str(i.get("id")), str(i.get("status")), title(str(i.get("id"))))
                     for i in items if (m := EXTERNAL_SLICE.match(str(i.get("title", ""))))))
    gates = []
    for label, (how, arg) in GATES:
        if how == "note":
            gates.append({"label": label, "id": arg, "status": status(arg)})
        elif how == "title":
            hit = sorted((str(i.get("id")) for i in items if re.search(arg, str(i.get("title", "")))), key=work.id_order)
            gates.append({"label": label, "id": hit[0] if hit else None,
                          "status": status(hit[0]) if hit else "not filed"})
        else:
            gates.append({"label": label, "id": None, "gap": gap, "threshold": arg,
                          "status": ("landed" if gap <= arg else "open")})
    return {"slices": slices, "gates": gates,
            "counts": {"external_landed": sum(1 for s in slices if finished(s[2])), "external_total": len(slices)}}


def measure_pacing() -> dict | None:
    """The RUNNING account's newest meter reading against its caps, or None when this machine has no coordination
    directory (CI, a fresh clone) or that account has no reading. The directory, the readings file and the account are
    the orchestrator's (`coord.py`, `budget.py`, `account.py`): two Claude accounts record into the one file, and a
    reading of the other account is not this one's meter (kb/Work PB2478)."""
    import account  # noqa: PLC0415
    import coord  # noqa: PLC0415
    d = coord.coord_path()
    acct = account.current()
    readings = [r for r in (coord.read_json(d / "readings.json", []) if d.is_dir() else [])
                if r.get("account") == acct.name]
    if not readings:
        return None
    q = acct.quota
    newest = max(readings, key=lambda r: datetime.datetime.fromisoformat(r["noted_at"]))
    return {"reading": newest, "account": acct.name, "weekly_cap": q["weekly_cap_pct"],
            "soft": q["session_soft_stop_pct"], "hard": q["session_hard_stop_pct"],
            "cap_source": q.get("weekly_cap_source", ""),
            "noted_utc": datetime.datetime.fromisoformat(newest["noted_at"]).astimezone(datetime.timezone.utc)
            .strftime("%Y-%m-%d %H:%M")}


# ── the drift test ──────────────────────────────────────────────────────────────────────────────────────────────

def problems(items: list[dict], plan: dict, rem: dict) -> list[str]:
    """Why the page's plan half cannot be trusted, or [] (CLAUDE.md rule 5's drift test for these structures)."""
    bad = []
    ids = {i.get("id") for i in items}
    bad += [f"lane-note-missing: {what} names {lead}, which is not a kb/Work note"
            for what, group in (("CAMPAIGNS", CAMPAIGNS), ("HELD", HELD)) for lead, _ in group if lead not in ids]
    if DECISION not in ids:
        bad.append(f"lane-note-missing: the owner's decision {DECISION} is not a kb/Work note")
    bad += [f"lane-empty: {lead} ({name}) is named in no note's `cluster` list" for lead, name in (*CAMPAIGNS, *HELD)
            if lead in ids and not work.cluster_members(items, lead)]
    bad += [f"gate-note-missing: '{label}' names {arg}, which is not a kb/Work note"
            for label, (how, arg) in GATES if how == "note" and arg not in ids]
    if not plan["slices"]:
        bad.append("external-slices-none: no note's title reads '<id> — external repository slice N', so the lane "
                   "would render empty")
    numbers = [s[0] for s in plan["slices"]]
    if len(numbers) != len(set(numbers)):
        bad.append(f"external-slices-duplicate: two notes claim one slice number: {sorted(numbers)}")
    c = rem["counts"]
    if c and (rem["compiler"]["n"], rem["compiler"]["kind"]["defect"]) != (c["compiler_open"], c["defects"]):
        bad.append(f"bucket-count: the compiler bucket counts {rem['compiler']['n']} notes, register_counts "
                   f"{c['compiler_open']}")
    if c and c["gap_holders"] is not None and rem["gap"]["n"] != c["gap_holders"]:
        bad.append(f"bucket-count: the GAP bucket counts {rem['gap']['n']} holders, register_counts {c['gap_holders']}")
    return bad


def load_inventory() -> list[dict]:
    return json.loads((REPO / "tests" / "version-matrix" / "traceability-inventory.json").read_text(encoding="utf-8"))


def live_problems() -> list[str]:
    items, rows = work.load(), load_inventory()
    gap = sum(1 for r in rows if r["state"] == "GAP")
    return problems(items, measure(items, gap), remaining(items, rows, gap, fleet_population()))


def self_test() -> int:
    """Fire every arm on planted registers, then run `problems` on the real one."""

    def note(nid, title="x", kind="analysis", status="open", cluster=(), blocked_by=(), rows=(), **flags):
        return {"id": nid, "title": f"{nid} — {title}", "kind": kind, "status": status, "cluster": list(cluster),
                "blocked_by": list(blocked_by), "inventory_rows": list(rows), "_file": f"{nid}.md", **flags}

    fleet = ("defect", ("process_only",))
    leads = [note(lead, "a lead", "decision", process_only=True) for lead, _ in (*CAMPAIGNS, *HELD)]
    gate_notes = [note(arg, "a gate", process_only=True) for _, (how, arg) in GATES if how == "note"]
    good = [*leads, *gate_notes,
            note("PB9001", kind="defect", wrong_answer=True, rows=["R-1"], cluster=[GAP_CAMPAIGN]),
            note("PB9002", kind="defect", crashes=True, blocked_by=["PB9001"]),
            note("PB9003", kind="analysis", silent=True),                        # a compiler analysis: no lane plans it
            note("PB9004", kind="analysis", process_only=True, rows=["R-2"]),    # a GAP holder outside the campaign
            note("PB9005", kind="analysis", process_only=True, cluster=["PB2119"]),
            note("PB9006", "external repository slice 1: z", kind="defect", status="landed", cluster=["PB1086"]),
            note("PB9007", "external repository slice 2: w", kind="defect", cluster=["PB1086"], process_only=True),
            note("PB9008", kind="defect", status="owner", under_rejects=True),
            note("PB9010", kind="analysis", process_only=True, cluster=["PB1754"])]
    rows = [{"rule-id": "R-1", "state": "GAP", "verdict": "PARTIAL"}, {"rule-id": "R-2", "state": "GAP", "verdict": "CONFORMS"},
            {"rule-id": "R-3", "state": "GAP", "verdict": "DIVERGES"}, {"rule-id": "R-4", "state": "OK", "verdict": "CONFORMS"}]
    results = []

    def check(name, got, want):
        ok = got == want
        results.append(ok)
        print(f"{'✓' if ok else '✗'} {name}: {got!r}" + ("" if ok else f" (want {want!r})"))

    rem = remaining(good, rows, 3, fleet)
    c, g = rem["compiler"], rem["gap"]
    check("compiler bucket: notes, kinds, harm, states", (c["n"], c["kind"]["defect"], c["harm"]["wrong_answer"],
          c["harm"]["crashes"], dict(c["states"])), (4, 3, 1, 1, {"ready": 2, "waiting": 1, "blocked": 1}))
    check("a compiler note no lane plans is named", c["no_lane"], ["PB9003"])
    check("a GAP holder outside the GAP campaign is flagged", g["outside_campaign"], ["PB9004"])
    check("an unclaimed GAP row is named; the witness-owed row counted", (g["unclaimed"], g["witness_owed"], g["rows"]),
          (["R-3"], 1, 3))
    check("the held bucket counts its clusters", [(h["lead"], h["open"]) for h in rem["held"]["clusters"]],
          [("PB2119", 1), ("PB1754", 1)])
    check("a v1.0 gate note only a held cluster or no lane plans is named",
          [x["id"] for x in rem["gates"] if x["unplanned"]], ["PB1610"])
    check("register_counts", rem["counts"],
          {"compiler_open": 4, "defects": 3, "wrong_answer": 1, "crashes": 1, "gap_holders": 2})
    check("register_counts of a tree with no register", register_counts([], set()), None)
    check("register_counts before inventory_rows existed",
          register_counts([{"id": "PB1", "status": "open", "kind": "defect"}], {"R-1"})["gap_holders"], None)

    rules = {"wave": {"max_notes_per_group": 5, "max_groups": 8, "train_size": 5, "implementer_model": "sonnet"},
             "cost": {"per_group_tokens": {"sonnet": 400000}, "per_note_tokens": {"sonnet": 240000},
                      "lander_tokens": 480000, "lander_model": "opus"},
             "calibration": {"tokens_per_point": {"sonnet": 4000000, "opus": 1600000}}}
    fg = {"clusters": [{"notes": [{"id": "PB9001"}]}], "unsited": ["PB9008"]}
    full = {"rules": rules, "fix_groups": fg, "rate": {"per_h": 2.0}, "burn": {"per_h": 3.0}}
    est = bucket_estimates(rem, full)["compiler"]
    # fix lane: PB9001 (a cluster of 1), PB9008 (unsited), PB9002 (not clustered: blocked -> a chunk of 1) = 3 groups
    # points: 3 x (400k + 240k) / 4M + one lander 480k / 1.6M = 0.48 + 0.3
    check("the estimate from planted inputs", (est["groups"], est["waves"], round(est["points"], 2), est["hours"],
          est["burn_points"], est["unplanned"]), (3, 1, 0.78, 1.5, 4.5, 1))
    est = bucket_estimates(rem, {**full, "fix_groups": None, "rate": None})["compiler"]
    check("a missing input is UNMEASURED, never a number", (est["groups"], est["points"], est["hours"]), (None, None, None))
    est = bucket_estimates(rem, {**full, "burn": None})["compiler"]
    check("a missing burn leaves the hours and drops only the points at the burn", (est["hours"], est["burn_points"]),
          (1.5, None))
    check("held notes are estimated only when released",
          (bucket_estimates(rem, full)["held"]["groups"], bucket_estimates(rem, full)["gap"]["held_out"]), (2, 0))
    t0 = datetime.datetime(2026, 10, 10, 17, 0, tzinfo=datetime.timezone.utc)
    rd = [{"noted_at": "2026-10-10T16:00:00+00:00", "weekly_pct": 99}, {"noted_at": "2026-10-10T17:30:00+00:00", "weekly_pct": 2},
          {"noted_at": "2026-10-10T19:30:00+00:00", "weekly_pct": 8}]
    check("the burn since the week's first reading", measure_burn(rd, t0)["per_h"], 3.0)
    check("one reading this week is no burn", measure_burn(rd[:2], t0), None)

    plan = measure(good, 3)
    check("clean planted register", problems(good, plan, rem), [])

    def fires(name, items, prefix, mutate=None):
        r = remaining(items, rows, 3, fleet)
        if mutate:
            mutate(r)
        got = problems(items, measure(items, 3), r)
        ok = any(x.startswith(prefix) for x in got)
        results.append(ok)
        print(f"{'✓' if ok else '✗'} {name}: {got or 'clean'}")

    fires("lane-note-missing", [i for i in good if i["id"] != "PB1754"], "lane-note-missing")
    fires("lane-empty", [i for i in good if i["id"] != "PB9010"], "lane-empty")
    fires("gate-note-missing", [i for i in good if i["id"] != "PB1610"], "gate-note-missing")
    fires("external-slices-none", [i for i in good if i["id"] not in ("PB9006", "PB9007")], "external-slices-none")
    fires("external-slices-duplicate", [*good, note("PB9009", "external repository slice 2: again")],
          "external-slices-duplicate")
    fires("bucket-count", good, "bucket-count", lambda r: r["compiler"].update(n=99))

    live_bad = live_problems()
    results.append(not live_bad)
    print(f"{'✓' if not live_bad else '✗'} the real register: {live_bad or 'clean'}")
    if all(results):
        print(f"✓ ledger_plan self-test: {len(results)} cases")
        return 0
    print(f"⛔ ledger_plan self-test: {results.count(False)} of {len(results)} cases failed")
    return 1


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--self-test", action="store_true", help="fire every arm on planted registers, then the real one")
    a = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if a.self_test:
        return self_test()
    bad = live_problems()
    for b in bad:
        print(f"⛔ {b}")
    print("✓ the plan reads cleanly off the register" if not bad else f"⛔ {len(bad)} problem(s)")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
