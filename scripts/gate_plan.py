#!/usr/bin/env python3
"""gate_plan.py — the ORDER PLAN of an implementer gate (kb/Work PB1717; DESIGN-test-build-ci.md §3.14.2).

The gate runs the WHOLE discovered population of every assembly in two LEGS: the likely-red cases first, everything
else second. Ordering changes WHEN a case runs, never WHETHER. This script decides, per assembly, which cases are in
leg 1, which in leg 2, and in what order, and writes that as `plan.json`, the one file the test hosts' leg filter
reads (M12) and the gate driver hands them (M13).

Inputs, each optional except the listings:
  * the DISCOVERED population, per assembly: the raw output of a `--list-tests` run (`--list Unit=<file>`);
  * the change: the diff against `--base` (the worktree's, or `--head`'s for a replay), analysed by
    `scripts/spec/impacted_tests.py` against the impact map for the base (any age; `--map`/`--store`);
  * TIMINGS: the per-case durations in the trx files of the worktree's previous gate (`--previous-run <dir>`),
    else the map's recorded durations; and that gate's REDS (trx outcome `Failed`). The gate driver times its plan
    from the SHARED TIMINGS STORE instead (`publish_timings`/`read_shared_timings`, kb/Work PB2527): every gate
    publishes there, so a fresh worktree's plan is timed too.

THE NAME KEY. Every lookup keys on `name_key(display name)`: the display name with the partition suffix `_P<k>`
removed from its CLASS segment. A partitioned family (`TestPartitioning.Slice`) puts row i in class
`_P{i % Partitions}`, so appending one golden to a manifest renames every later row of the family; keyed without the
suffix, the append leaves ONE unknown case (evidence `impact-map-pb1708/b7`).

THE TIERS (per case; a key shared by several cases takes the lowest):
  0a  a case of a test method the diff ADDS · a corpus row whose manifest entry or golden files the diff changes ·
      red at the previous gate — always leg 1 (an EDITED test or harness is tier 1 through the map: a whole changed
      test file would put every row of its theories here — 5,774 cases for 68b X, evidence b4);
  0u  known to no timing source and to no map (genuinely new) — charged the assembly's median time and admitted to
      leg 1, in declaration order, within LEG_ONE_UNKNOWN_BUDGET; the overflow LEADS leg 2;
  1–3 impacted_tests.py's tiers (§3.13); with no map, every case is tier 1.
LEG 1 = tier 0a + the admitted 0u + the cheapest tier-1 cases while their recorded time stays within LEG_ONE_BUDGET
and each case's collection's budgeted leg-1 time within LEG_ONE_COLLECTION_CAP (tier 0 is exempt from the cap).
LEG 2 = everything else, collections LONGEST FIRST. Degenerate inputs are decided PER ASSEMBLY: no timings → leg 1
is tier 0a alone; an assembly whose whole recorded time fits the collection cap runs entirely in leg 1; an empty
leg 1, or one holding the whole population, makes the assembly ONE leg.

THE PLAN FILE (`--out`), canonical JSON (UTF-8, sorted keys, no insignificant whitespace):
  {"schema": 1, "base", "map", "map_state", "every_tier1": [...], "constants": {...},
   "assemblies": {"<assembly>": {"legs": [1, 2] | [1] | [2], "leg1": [key, ...], "leg2": [key, ...],
                                  "stats": {...}}},
   "sha256": "<SHA-256 of the canonical JSON of every other field>"}
`leg1` and `leg2` are in RANK order: a key's rank is its position, leg 1's list first. THE LEG FUNCTION (`leg_of`,
mirrored by the test hosts): 1 for a leg-1 key, 2 for a leg-2 key, and 1 for a key in neither — total over every
name, so a case the plan never heard of runs in leg 1, never nowhere. The last stdout line is the SHA-256 of the
file's BYTES: the digest the gate driver hands the test hosts (COBOLNET_GATE_PLAN_SHA256).

Usage:
    python scripts/gate_plan.py --out plan.json --list Conformance=conf.txt --list Unit=unit.txt \\
        --list Characterization=char.txt [--base <sha> [--head <sha>]] [--map <file> | --store <dir>] \\
        [--previous-run <dir>]
    python scripts/gate_plan.py --self-test
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import statistics
import sys
import tempfile
import xml.etree.ElementTree as ET
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent / "spec"))
sys.path.insert(0, str(Path(__file__).resolve().parent))
import impacted_tests  # noqa: E402
from test_population import LISTED_MARK, PopulationError, parse_listing  # noqa: E402

PLAN_SCHEMA = 1
LEG_ONE_BUDGET = 0.02            # of the assembly's recorded test-seconds, for tier 1
LEG_ONE_UNKNOWN_BUDGET = 0.02    # of the assembly's recorded test-seconds, for tier 0u (charged the median)
LEG_ONE_COLLECTION_CAP = 15.0    # seconds of one collection's BUDGETED leg-1 time (xunit runs a collection serially)
TIER_ORDER = ("0a", "0u", "1", "2", "3")
TRX_NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
PARTITION_CLASS_RE = re.compile(r"(?P<family>.+)_P\d+")


def name_key(display: str) -> str:
    """The display name with the partition suffix `_P<k>` removed from its CLASS segment (the one before the method;
    a theory's arguments are never touched)."""
    head, paren, args = display.partition("(")
    parts = head.split(".")
    if len(parts) >= 3:
        m = PARTITION_CLASS_RE.fullmatch(parts[-2])
        if m:
            parts[-2] = m.group("family")
    return ".".join(parts) + paren + args


def method_of(display: str) -> str:
    """The method segment of a display name, without generic arguments."""
    return display.partition("(")[0].split(".")[-1].split("<")[0]


def class_of(display: str) -> str:
    """The RAW class segment of a display name (a partition keeps its suffix: it is its own collection)."""
    parts = display.partition("(")[0].split(".")
    return parts[-2] if len(parts) >= 2 else ""


def leg_of(plan: dict, assembly: str, display: str) -> int:
    """THE LEG FUNCTION: total over every name, deterministic, and disjoint by construction."""
    a = plan["assemblies"].get(assembly)
    return 2 if a is not None and name_key(display) in set(a["leg2"]) else 1


def read_listing(path: Path) -> list[str]:
    """The discovered display names, in discovery order, from a `--list-tests` run's output — read by THE one listing
    parser, `test_population.parse_listing` (DESIGN §3.14.4)."""
    try:
        return list(parse_listing(path.read_text(encoding="utf-8-sig", errors="replace"), str(path)).elements())
    except PopulationError as e:
        raise SystemExit(str(e)) from None


class Timings:
    """Per-case recorded seconds by name key. A key with several results (xunit truncates long theory arguments
    with `···`, so rows share a name) is charged their mean; a theory whose data xunit cannot serialize is LISTED as
    one case (`Class.Method`) but RUN as rows (`Class.Method(item: …)`), so that listed key is charged its rows' sum."""

    def __init__(self, source: str):
        self.source = source
        self._rows: dict[str, list[float]] = defaultdict(list)
        self._theory_sum: dict[str, float] = defaultdict(float)

    def add(self, display: str, seconds: float) -> None:
        key = name_key(display)
        self._rows[key].append(seconds)
        if "(" in key:
            self._theory_sum[key.partition("(")[0]] += seconds

    def seconds(self, key: str) -> float | None:
        if key in self._rows:
            return statistics.fmean(self._rows[key])
        return self._theory_sum.get(key) if "(" not in key else None

    def __bool__(self) -> bool:
        return bool(self._rows)


def trx_secs(duration: str) -> float:
    h, m, s = duration.split(":")
    return int(h) * 3600 + int(m) * 60 + float(s)


def trx_results(run: Path):
    """(display name, seconds or None, outcome) of every result in every trx file of a gate run directory."""
    files = sorted(run.glob("*.trx"))
    if not files:
        raise SystemExit(f"{run}: no trx file — not a gate run directory")
    for trx in files:
        for r in ET.parse(trx).getroot().iter(TRX_NS + "UnitTestResult"):
            d = r.get("duration")
            yield r.get("testName") or "", (trx_secs(d) if d else None), r.get("outcome")


def read_previous_run(run: Path) -> tuple[Timings, set[str]]:
    """The previous gate's timings and REDS (by name key), from every trx file in its run directory."""
    timings, reds = Timings(f"previous run {run}"), set()
    for name, seconds, outcome in trx_results(run):
        if seconds is not None:
            timings.add(name, seconds)
        if outcome == "Failed":
            reds.add(name_key(name))
            reds.add(name_key(name).partition("(")[0])  # the listed key of an unserializable theory
    return timings, reds


# ── THE SHARED TIMINGS STORE (kb/Work PB2527) ──────────────────────────────────────────────────────────────────────
#: ⛔ WHY A STORE SHARED BY EVERY WORKTREE. Leg 2 runs its collections LONGEST FIRST, and a plan with no timings runs
#: them in declaration order — and every implementer gate runs in a FRESH worktree with no previous run, so every one
#: of them reported `timings none` (w1033 gate diagnosis: a long serial collection that starts late sets the leg's
#: wall; longest first modeled the Conformance leg 212 s -> 162 s at 24 corpus partitions). So every gate, lander
#: included, PUBLISHES its measured durations here after its legs, and every implementer plan reads them: a fresh
#: worktree plans from the newest durations any gate on this machine measured. Its own REDS still come only from its
#: own previous run. Timings are hints: they change the ORDER and the leg-1 share, never WHETHER a case runs.
#: It lives in `<git common dir>/cobol-gate-timings/` beside the gate slots; a write replaces the file atomically, and
#: two gates publishing at once lose at most one gate's merge (the next gate's publish carries the newest durations).
TIMINGS_STORE_SCHEMA = 1
TIMINGS_STORE_DIR = "cobol-gate-timings"
TIMINGS_STORE_FILE = "timings.json"


def timings_store(common_dir: Path) -> Path:
    """The shared store's file under a repository's git common dir."""
    return common_dir / TIMINGS_STORE_DIR / TIMINGS_STORE_FILE


def _read_store(store: Path) -> dict:
    """The store's content (an empty store when the file does not exist); a malformed file raises ValueError."""
    if not store.exists():
        return {"schema": TIMINGS_STORE_SCHEMA, "updated": None, "by": None, "durations": {}}
    try:
        data = json.loads(store.read_text(encoding="utf-8"))
    except (OSError, ValueError) as e:
        raise ValueError(f"{store}: unreadable shared timings store ({type(e).__name__}: {e})") from None
    if (not isinstance(data, dict) or data.get("schema") != TIMINGS_STORE_SCHEMA
            or not isinstance(data.get("durations"), dict)):
        raise ValueError(f"{store}: not a schema-{TIMINGS_STORE_SCHEMA} shared timings store")
    return data


def read_shared_timings(store: Path) -> Timings:
    """The shared store as Timings (empty — falsy — when no gate has published yet)."""
    data = _read_store(store)
    t = Timings(f"shared store, {len(data['durations'])} cases, updated {data['updated']} by {data['by']}"
                if data["durations"] else "none")
    for display, seconds in data["durations"].items():
        for s in seconds:
            t.add(display, float(s))
    return t


def publish_timings(store: Path, run: Path, discovered: set[str], by: str, now: str) -> tuple[int, str]:
    """Merge one gate run's measured durations into the shared store; return (cases published, a note).

    A case this run measured replaces its entry; a case it did not run (a stopped or leg-1-only gate) keeps the
    duration an earlier gate measured; a case no longer DISCOVERED (by name key, so a partition renumbering or an
    unserializable theory's rows survive) is dropped, which bounds the store to the live population. A malformed
    store is replaced rather than merged, and the note says so."""
    keys = {name_key(d) for d in discovered}

    def live(display: str) -> bool:
        k = name_key(display)
        return k in keys or k.partition("(")[0] in keys

    note = ""
    try:
        old = _read_store(store)["durations"]
    except ValueError as e:
        old, note = {}, f"replaced a malformed store: {e}"
    measured: dict[str, list[float]] = defaultdict(list)
    for display, seconds, _outcome in trx_results(run):
        if seconds is not None and live(display):
            measured[display].append(round(seconds, 4))
    merged = {d: s for d, s in old.items() if live(d)}
    merged.update(measured)
    store.parent.mkdir(parents=True, exist_ok=True)
    tmp = store.with_name(f"{store.name}.{os.getpid()}.tmp")
    tmp.write_text(json.dumps({"schema": TIMINGS_STORE_SCHEMA, "updated": now, "by": by, "durations": merged},
                              ensure_ascii=False, sort_keys=True, separators=(",", ":")), encoding="utf-8")
    os.replace(tmp, store)
    return len(measured), note


def map_timings(m: dict | None) -> Timings:
    t = Timings(f"map {m['commit'][:12]}" if m else "none")
    for row in (m or {}).get("tests", []):
        t.add(row[2], float(row[5]))
    return t


@dataclass
class Context:
    """Everything one assembly's plan needs beyond its listing."""
    tiers: dict[str, int]                   # name key -> map tier (1..3), for this assembly
    map_known: set[str]                     # every name key the map recorded, in any assembly
    every_tier1: bool                       # the change cannot be attributed (no map, stale map, ...)
    added_tests: set[str]                   # test methods the change adds
    goldens: set[str]                       # corpus goldens the change touches
    reds: set[str]
    previous: Timings
    recorded: Timings                       # the map's durations
    collection_of_class: dict[str, str] = field(default_factory=dict)


@dataclass
class KeyGroup:
    """The unit of assignment: every discovered case sharing one name key (the leg function sees only the key)."""
    key: str
    index: int                              # first declaration position
    cases: list[tuple[str, str]]            # (raw display, collection) per case
    tier: str = "1"
    per_case: float | None = None           # recorded seconds of one case; None = no timing source knows it

    def cost(self, median: float) -> float:
        return (self.per_case if self.per_case is not None else median) * len(self.cases)


def tier_of(display: str, key: str, ctx: Context, timed: bool, has_timings: bool) -> str:
    if (method_of(display) in ctx.added_tests or key in ctx.reds
            or any(f'name: "{g}"' in display for g in ctx.goldens)):
        return "0a"
    if has_timings and not timed and key not in ctx.map_known:
        return "0u"
    if ctx.every_tier1 or key not in ctx.tiers:
        return "1"
    return str(ctx.tiers[key])


def plan_assembly(displays: list[str], ctx: Context) -> dict:
    units: dict[str, KeyGroup] = {}
    for i, d in enumerate(displays):
        k = name_key(d)
        u = units.setdefault(k, KeyGroup(k, i, []))
        u.cases.append((d, ctx.collection_of_class.get(class_of(d), class_of(d))))
    has_timings = any((ctx.previous.seconds(k) if ctx.previous else None) is not None
                      or ctx.recorded.seconds(k) is not None for k in units)
    for u in units.values():
        prev = ctx.previous.seconds(u.key) if ctx.previous else None
        u.per_case = prev if prev is not None else ctx.recorded.seconds(u.key)
        u.tier = min((tier_of(d, u.key, ctx, u.per_case is not None, has_timings) for d, _ in u.cases),
                     key=TIER_ORDER.index)
    known = [u.per_case for u in units.values() for _ in u.cases if u.per_case is not None]
    median = statistics.median(known) if known else 0.0
    total = sum(known)
    whole = sum(u.cost(median) for u in units.values())
    order = sorted(units.values(), key=lambda u: u.index)

    leg1: list[KeyGroup] = []
    overflow: list[KeyGroup] = []
    budget_per: dict[str, float] = defaultdict(float)
    reason = ""
    if has_timings and whole <= LEG_ONE_COLLECTION_CAP:
        leg1 = list(order)
        reason = f"its whole recorded time {whole:.1f} s fits the collection cap"
    else:
        leg1 = [u for u in order if u.tier == "0a"]
        spent = 0.0
        for u in (u for u in order if u.tier == "0u"):
            if spent + u.cost(median) <= LEG_ONE_UNKNOWN_BUDGET * total:
                leg1.append(u)
                spent += u.cost(median)
            else:
                overflow.append(u)
        if has_timings:
            acc = 0.0
            for u in sorted((u for u in order if u.tier == "1"), key=lambda u: (u.cost(median), u.index)):
                if acc + u.cost(median) > LEG_ONE_BUDGET * total:
                    break
                part: dict[str, float] = defaultdict(float)
                for _, c in u.cases:
                    part[c] += u.per_case or 0.0
                if any(budget_per[c] + s > LEG_ONE_COLLECTION_CAP for c, s in part.items()):
                    continue
                leg1.append(u)
                acc += u.cost(median)
                for c, s in part.items():
                    budget_per[c] += s
        else:
            reason = "no timings: leg 1 is tier 0a alone"

    in_leg1 = {u.key for u in leg1}
    rest = [u for u in order if u.key not in in_leg1]
    if not leg1:
        legs, reason = [2], "leg 1 would be empty" + (f" ({reason})" if reason else "")
    elif not rest:
        legs, reason = [1], reason or "leg 1 would hold the whole population"
    else:
        legs = [1, 2]

    def within(u: KeyGroup) -> tuple:
        return (TIER_ORDER.index(u.tier), u.cost(median) if has_timings else 0.0, u.index)

    leg1_ranked = sorted(leg1, key=within)
    overflow_keys = {u.key for u in overflow}
    by_collection: dict[str, list[KeyGroup]] = defaultdict(list)
    for u in rest:
        if u.key not in overflow_keys:
            by_collection[u.cases[0][1]].append(u)
    collections = sorted(by_collection.values(),
                         key=lambda us: (-sum(u.cost(median) for u in us) if has_timings else 0.0,
                                         min(u.index for u in us)))
    leg2_ranked = overflow + [u for us in collections for u in sorted(us, key=within)]

    floor: dict[str, float] = defaultdict(float)
    label: dict[str, str] = {}              # a collection id (the map's is a GUID) -> a class it runs, for people
    for u in leg1:
        for d, c in u.cases:
            floor[c] += u.per_case if u.per_case is not None else median
            label.setdefault(c, class_of(d))
    big = max(floor.items(), key=lambda kv: kv[1]) if floor else ("", 0.0)
    big = (label.get(big[0], big[0]), big[1])
    tiers_count = {t: sum(len(u.cases) for u in units.values() if u.tier == t) for t in TIER_ORDER}
    stats = {"cases": len(displays), "keys": len(units), "tiers": tiers_count, "timed": has_timings,
             "recorded_seconds": round(total, 3), "median_seconds": round(median, 4),
             "leg1_cases": sum(len(u.cases) for u in leg1),
             "leg1_seconds": round(sum(u.cost(median) for u in leg1), 3),
             "leg1_floor_seconds": round(big[1], 3), "leg1_floor_collection": big[0],
             "unknown_admitted": sum(1 for u in leg1 if u.tier == "0u"), "unknown_overflow": len(overflow),
             "one_leg_reason": reason if len(legs) == 1 else ""}
    return {"legs": legs, "leg1": [u.key for u in leg1_ranked], "leg2": [u.key for u in leg2_ranked],
            "stats": stats}


def canonical(obj: dict) -> bytes:
    return json.dumps(obj, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")


def build_plan(listings: dict[str, list[str]], analysis: impacted_tests.Analysis, previous: Timings,
               reds: set[str], base: str | None) -> dict:
    m = analysis.map
    recorded = map_timings(m)
    map_known = {name_key(row[2]) for row in (m or {}).get("tests", [])}
    collection_of_class: dict[str, dict[str, str]] = defaultdict(dict)
    for row in (m or {}).get("tests", []):
        collection_of_class[row[0]][row[3].rsplit(".", 1)[-1]] = row[4]
    plan: dict = {"schema": PLAN_SCHEMA, "base": base, "map": m["commit"] if m else None,
                  "map_state": analysis.map_state, "every_tier1": analysis.reach.every_tier1,
                  "constants": {"LEG_ONE_BUDGET": LEG_ONE_BUDGET, "LEG_ONE_UNKNOWN_BUDGET": LEG_ONE_UNKNOWN_BUDGET,
                                "LEG_ONE_COLLECTION_CAP": LEG_ONE_COLLECTION_CAP},
                  "timings": previous.source if previous else recorded.source, "assemblies": {}}
    for asm, displays in listings.items():
        tiers: dict[str, int] = {}
        for display, t in analysis.tiers.get(asm, {}).items():
            k = name_key(display)
            tiers[k] = min(tiers.get(k, 3), t)
        ctx = Context(tiers=tiers, map_known=map_known, every_tier1=bool(analysis.reach.every_tier1),
                      added_tests=analysis.reach.added_tests.get(asm, set()), goldens=analysis.reach.goldens,
                      reds=reds, previous=previous, recorded=recorded,
                      collection_of_class=collection_of_class.get(asm, {}))
        plan["assemblies"][asm] = plan_assembly(displays, ctx)
    plan["sha256"] = hashlib.sha256(canonical(plan)).hexdigest()
    return plan


def summary_lines(plan: dict, out: Path) -> list[str]:
    """What a plan says, for people: the every-tier-1 reasons, each assembly's tiers, leg 1 and legs, and the plan's
    identity. The CLI and the gate driver (scripts/run_gate_legs.py) both print it."""
    lines = [f"  every case tier 1: {why}" for why in plan["every_tier1"]]
    for asm, a in plan["assemblies"].items():
        s = a["stats"]
        lines.append(f"gate_plan: {asm}: {s['cases']} cases, tiers {s['tiers']}; leg 1 {s['leg1_cases']} cases "
                     f"({s['leg1_seconds']} of {s['recorded_seconds']} recorded s; floor {s['leg1_floor_seconds']} s "
                     f"in {s['leg1_floor_collection'] or '-'}); legs {a['legs']}"
                     + (f" — {s['one_leg_reason']}" if s["one_leg_reason"] else ""))
    lines.append(f"gate_plan: map {plan['map_state']}, timings {plan['timings']}; plan {plan['sha256'][:12]} → {out}")
    return lines


def write_plan(plan: dict, out: Path) -> str:
    """Write the plan canonically; return the SHA-256 of the file's bytes (the handshake digest)."""
    data = canonical(plan)
    out.write_bytes(data)
    return hashlib.sha256(data).hexdigest()


# ── self-test ─────────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    """Drive every arm on planted inputs (feedback_prove_the_watchdog_fails)."""
    failures = 0

    def check(label: str, ok: bool, detail: object = "") -> None:
        nonlocal failures
        failures += 0 if ok else 1
        print(f"  {'PASS' if ok else '⛔ FAIL'}  {label}{': ' + str(detail) if detail != '' else ''}")

    ns = "CobolNet.Tests.Conformance."
    check("NameKey strips the partition suffix from the class segment",
          name_key(ns + 'CorpusRunnerTests_P2.Run(edition: "85", name: "x")')
          == ns + 'CorpusRunnerTests.Run(edition: "85", name: "x")')
    check("NameKey never touches a theory's arguments",
          name_key(ns + 'T.M(path: "a.B_P1.c")') == ns + 'T.M(path: "a.B_P1.c")')
    check("NameKey leaves a class without a numeric partition suffix alone",
          name_key(ns + "Foo_Pbar.M") == ns + "Foo_Pbar.M" and name_key(ns + "Foo.M") == ns + "Foo.M")

    def ctx(**kw) -> Context:
        base = dict(tiers={}, map_known=set(), every_tier1=False, added_tests=set(), goldens=set(), reds=set(),
                    previous=Timings("none"), recorded=Timings("none"))
        base.update(kw)
        return Context(**base)

    def timed(pairs: list[tuple[str, float]], source: str = "planted") -> Timings:
        t = Timings(source)
        for d, s in pairs:
            t.add(d, s)
        return t

    # 100 cheap tier-1 cases in 10 classes (1 s each) + one 1,000 s long pole: budget 2 % of 1,100 s = 22 s.
    cheap = [f"{ns}C{i % 10}.M{i}" for i in range(100)]
    pole = f"{ns}Pole.Run"
    t = timed([(d, 1.0) for d in cheap] + [(pole, 1000.0)])
    known = {name_key(d) for d in cheap + [pole]}
    p = plan_assembly(cheap + [pole], ctx(previous=t, map_known=known, tiers={k: 1 for k in known}))
    check("tier 1: the cheapest cases fill leg 1 within LEG_ONE_BUDGET, the rest go to leg 2",
          p["stats"]["leg1_cases"] == 22 and p["legs"] == [1, 2] and name_key(pole) in p["leg2"], p["stats"])
    # the collection cap: one class of 40 × 1 s cases — only 15 of them fit its cap.
    one = [f"{ns}Big.M{i}" for i in range(40)]
    t2 = timed([(d, 1.0) for d in one] + [(pole, 1000.0)])
    k2 = {name_key(d) for d in one + [pole]}
    p = plan_assembly(one + [pole], ctx(previous=t2, map_known=k2, tiers={k: 1 for k in k2}))
    check("the collection cap holds one collection's budgeted leg-1 time to LEG_ONE_COLLECTION_CAP",
          p["stats"]["leg1_cases"] == 15 and p["stats"]["leg1_floor_seconds"] == 15.0, p["stats"])
    # tier 0a: a changed test file's class, a touched golden, a previous red — in leg 1 whatever their cost.
    row = f'{ns}CorpusRunnerTests_P1.EnabledProgram(edition: "85", name: "g1")'
    red = f"{ns}C3.M3"
    t3 = timed([(d, 1.0) for d in cheap] + [(pole, 1000.0), (row, 500.0), (f"{ns}Changed.M", 400.0)])
    all3 = cheap + [pole, row, f"{ns}Changed.M"]
    k3 = {name_key(d) for d in all3}
    p = plan_assembly(all3, ctx(previous=t3, map_known=k3, tiers={k: 3 for k in k3}, added_tests={"M"},
                                goldens={"g1"}, reds={name_key(red)}))
    check("tier 0a: an added test method, a touched golden and a previous red lead leg 1, exempt from the budget",
          p["leg1"] == [name_key(red), f"{ns}Changed.M", name_key(row)]   # inside tier 0a, cheapest first
          and p["stats"]["tiers"]["0a"] == 3, p["leg1"])
    check("tiers 2 and 3 never enter leg 1 on the budget", p["stats"]["leg1_cases"] == 3, p["stats"])
    # tier 0u: unknown cases charged the median; the budget admits some, the overflow LEADS leg 2.
    new = [f"{ns}New.M{i}" for i in range(30)]
    p = plan_assembly(cheap + [pole] + new, ctx(previous=t, map_known=known, tiers={k: 1 for k in known}))
    check("tier 0u: unknown cases are charged the median within LEG_ONE_UNKNOWN_BUDGET; the overflow leads leg 2",
          p["stats"]["unknown_admitted"] == 22 and p["stats"]["unknown_overflow"] == 8
          and p["leg2"][:8] == [name_key(d) for d in new[22:]], p["stats"])
    # a partition rename after a manifest append: keyed by NameKey the renamed row is KNOWN, not 0u.
    renamed = f'{ns}CorpusRunnerTests_P2.EnabledProgram(edition: "85", name: "g1")'
    p = plan_assembly(cheap + [pole, renamed], ctx(previous=t3, map_known=k3))
    check("a partition-renamed row keeps its timing through NameKey (not tier 0u)",
          p["stats"]["tiers"]["0u"] == 0, p["stats"]["tiers"])
    # no map: every case tier 1 (the timings still order it).
    p = plan_assembly(cheap + [pole], ctx(previous=t, every_tier1=True))
    check("no map: every case is tier 1", p["stats"]["tiers"]["1"] == 101 and p["stats"]["leg1_cases"] == 22,
          p["stats"]["tiers"])
    # stale map: every case tier 1, its recorded durations still time the plan.
    p = plan_assembly(cheap + [pole], ctx(recorded=t, map_known=known, tiers={k: 3 for k in known},
                                          every_tier1=True))
    check("a stale map: every case tier 1, its durations still order leg 1",
          p["stats"]["tiers"]["1"] == 101 and p["stats"]["leg1_cases"] == 22, p["stats"])
    # no timings: leg 1 is tier 0a alone, ordered by tier.
    p = plan_assembly(cheap + [f"{ns}Changed.M"], ctx(added_tests={"M"}))
    check("no timings: leg 1 is tier 0a alone and nothing is tier 0u",
          p["leg1"] == [f"{ns}Changed.M"] and p["stats"]["tiers"]["0u"] == 0 and p["legs"] == [1, 2], p["stats"])
    # empty leg 1: one leg, leg 2 only (the driver does not invoke the assembly for leg 1).
    p = plan_assembly(cheap, ctx())
    check("an empty leg 1 makes the assembly one leg (leg 2)", p["legs"] == [2] and not p["leg1"]
          and len(p["leg2"]) == 100, p["stats"]["one_leg_reason"])
    # whole population in leg 1: an assembly whose whole recorded time fits the cap.
    small = [f"{ns}S.M{i}" for i in range(33)]
    p = plan_assembly(small, ctx(previous=timed([(d, 0.05) for d in small])))
    check("an assembly whose whole recorded time fits the collection cap runs entirely in leg 1",
          p["legs"] == [1] and len(p["leg1"]) == 33 and not p["leg2"], p["stats"]["one_leg_reason"])
    # leg 2: collections longest first.
    lz = [f"{ns}A.M1", f"{ns}B.M1", f"{ns}B.M2"]
    p = plan_assembly(cheap + [pole] + lz, ctx(previous=timed([(d, 1.0) for d in cheap] + [(pole, 1000.0)]
                                                               + [(d, 50.0) for d in lz]),
                                               map_known=known | set(lz), tiers={k: 3 for k in known | set(lz)}))
    check("leg 2 runs collections longest first", p["leg2"][0] == name_key(pole) and p["leg2"][1] == f"{ns}B.M1",
          p["leg2"][:3])
    # truncated names: several cases share one key — one leg, counted per case.
    trunc = [f"{ns}T.M(x: \"aaa\"···)"] * 3
    p = plan_assembly(cheap + trunc, ctx(previous=timed([(d, 1.0) for d in cheap + trunc])))
    check("cases sharing a truncated name share one key and one leg, counted per case",
          p["stats"]["cases"] == 103 and p["stats"]["keys"] == 101
          and (name_key(trunc[0]) in p["leg1"]) != (name_key(trunc[0]) in p["leg2"]), p["stats"]["keys"])
    # an unserializable theory: listed once, timed by its rows' sum.
    t4 = timed([(f"{ns}U.M(item: 1)", 2.0), (f"{ns}U.M(item: 2)", 3.0)])
    check("a theory listed as one case is charged the sum of its rows", t4.seconds(f"{ns}U.M") == 5.0,
          t4.seconds(f"{ns}U.M"))
    # the leg function is total and disjoint.
    plan = {"assemblies": {"Conformance": {"legs": [1, 2], "leg1": [f"{ns}A.M"], "leg2": [f"{ns}B.M"]}}}
    check("leg_of is total: leg 1, leg 2, and leg 1 for a key the plan never heard of",
          (leg_of(plan, "Conformance", f"{ns}A.M"), leg_of(plan, "Conformance", f"{ns}B.M"),
           leg_of(plan, "Conformance", f"{ns}Z.M"), leg_of(plan, "Unit", f"{ns}B.M")) == (1, 2, 1, 1))
    # the shared timings store (kb/Work PB2527): every gate publishes, a fresh worktree's plan reads it.
    def trx_with(run_dir: Path, rows: list[tuple[str, str]]) -> None:
        run_dir.mkdir(parents=True, exist_ok=True)
        body = "".join(f'<UnitTestResult testName="{n}" duration="{d}" outcome="Passed" />' for n, d in rows)
        (run_dir / "leg-1-Conformance.trx").write_text(
            '<?xml version="1.0" encoding="utf-8"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/'
            f'TeamTest/2010"><Results>{body}</Results></TestRun>', encoding="utf-8")

    with tempfile.TemporaryDirectory() as tmp:
        store = timings_store(Path(tmp) / "common")
        empty = read_shared_timings(store)
        long_c, short_c, gone = f"{ns}Long_P3.M", f"{ns}Short.M", f"{ns}Gone.M"
        trx_with(Path(tmp) / "run1", [(long_c, "00:01:40.0"), (short_c, "00:00:01.0"), (gone, "00:00:02.0")])
        n1, _ = publish_timings(store, Path(tmp) / "run1", {long_c, short_c, gone}, "gate one", "t1")
        # a later, partial gate (leg 1 only): it measured only Short, and Gone is no longer discovered;
        # the long partition was renumbered (_P3 -> _P17) by a partition-count change.
        trx_with(Path(tmp) / "run2", [(short_c, "00:00:03.0")])
        renumbered = f"{ns}Long_P17.M"
        n2, _ = publish_timings(store, Path(tmp) / "run2", {renumbered, short_c}, "gate two", "t2")
        shared = read_shared_timings(store)
        check("the shared timings store: empty before any gate publishes (a plan then has no timings)",
              not empty and empty.source == "none", empty.source)
        check("a published gate's durations time a fresh worktree's plan; a partial gate replaces only what it "
              "measured, keeps the rest by name key across a partition renumbering, and drops what is no longer "
              "discovered",
              n1 == 3 and n2 == 1 and shared.seconds(name_key(renumbered)) == 100.0
              and shared.seconds(short_c) == 3.0 and shared.seconds(gone) is None
              and "gate two" in shared.source, (n1, n2, shared.source))
        p = plan_assembly([short_c, renumbered], ctx(previous=shared, map_known=set(), every_tier1=True))
        check("timed from the store, leg 2 starts with the longest collection",
              p["stats"]["timed"] and (p["leg2"] or p["leg1"])[0] == name_key(renumbered), p)
        store.write_text("{ not json", encoding="utf-8")
        try:
            read_shared_timings(store)
            malformed_read_refused = False
        except ValueError:
            malformed_read_refused = True
        _, note = publish_timings(store, Path(tmp) / "run2", {renumbered, short_c}, "gate three", "t3")
        check("a malformed store is refused on read and REPLACED (named) on publish",
              malformed_read_refused and "replaced a malformed store" in note
              and read_shared_timings(store).seconds(short_c) == 3.0, note)

    # the file: canonical, digested, readable back.
    listing = f"Test run for x.dll\n{LISTED_MARK}\n" + "\n".join("    " + d for d in cheap[:3]) + "\n"
    with tempfile.TemporaryDirectory() as tmp:
        lf = Path(tmp) / "list.txt"
        lf.write_text(listing, encoding="utf-8")
        check("a --list-tests output reads back as its display names", read_listing(lf) == cheap[:3])
        a = impacted_tests.Analysis(None, "none", impacted_tests.Reach(every_tier1=["no map"]), {})
        full = build_plan({"Conformance": read_listing(lf)}, a, Timings("none"), set(), None)
        out = Path(tmp) / "plan.json"
        digest = write_plan(full, out)
        body = json.loads(out.read_text(encoding="utf-8"))
        own = body.pop("sha256")
        check("the plan carries the SHA-256 of its content and the file digest is of its bytes",
              own == hashlib.sha256(canonical(body)).hexdigest()
              and digest == hashlib.sha256(out.read_bytes()).hexdigest())
    print("ALL GREEN" if not failures else f"⛔ {failures} SELF-TEST FAILURE(S)")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--out", type=Path, help="the plan file to write")
    ap.add_argument("--list", action="append", default=[], metavar="ASSEMBLY=FILE",
                    help="an assembly's --list-tests output (repeat per assembly)")
    ap.add_argument("--base", help="the change's base commit (the worktree's cut point)")
    ap.add_argument("--head", help="plan for --base..--head instead of the worktree (a replay)")
    ap.add_argument("--map", type=Path, help="an explicit impact map (otherwise the store is searched)")
    ap.add_argument("--store", type=Path, help="the map store (default <git common dir>/cobol-impact)")
    ap.add_argument("--previous-run", type=Path, help="the previous gate's run directory (its trx files)")
    ap.add_argument("--self-test", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if args.self_test:
        return self_test()
    if not args.out or not args.list:
        ap.error("--out and at least one --list are required")
    listings = {}
    for spec in args.list:
        asm, sep, path = spec.partition("=")
        if not sep:
            ap.error(f"--list {spec!r}: expected ASSEMBLY=FILE")
        listings[asm] = read_listing(Path(path))
    impacted_tests.HEAD = impacted_tests.git("rev-parse", args.head) if args.head else None
    analysis = impacted_tests.analyse(args.base, None, args.map, args.store)
    previous, reds = read_previous_run(args.previous_run) if args.previous_run else (Timings("none"), set())
    plan = build_plan(listings, analysis, previous, reds, args.base)
    digest = write_plan(plan, args.out)
    for line in summary_lines(plan, args.out):
        print(line, file=sys.stderr)
    print(digest)
    return 0


if __name__ == "__main__":
    sys.exit(main())
