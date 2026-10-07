#!/usr/bin/env python3
"""The completion plan, lane by lane, measured off the register — the plan half of the completion ledger.

⛔ WHY THIS EXISTS (kb/Work PB2462). The owner's live status page (`gen_ledger.py`) measured one thing: the
conformance burn-down. The completion plan the owner decided on 2026-10-06 (kb/Work R69 §1-§3) runs six lanes at
once: the legacy retirement, conformance to zero GAP, the external repository, the architecture review's phases
R0-R5, the gates between them, and the owner's pacing rules. None of the others was visible anywhere except by
reading kb/Work by hand. This module measures them; `gen_ledger.py` renders them. It writes nothing.

⭐ EVERY STATUS IS READ, NEVER WRITTEN. Cluster membership is work.py's `cluster_members` (the rule `cluster_order`
uses), a note's status is its frontmatter, a phase note is found by its title (`PB2115 — R0 census …`), an
external-repository slice by its title (`… — external repository slice 3: …`), a wave kind by the title prefix the
filing generator writes (`scripts/arch/file_census_notes.py` PREFIX, imported). A note filed tomorrow in any of
those shapes appears with no edit here.

⛔ THE HAND-KEPT STRUCTURES ARE SMALL AND SELF-TESTED: the six lanes (their names and definitions of done), the six
phases (and the one phase note whose title does not name its phase), and the gates (each a note id, a title rule
for a gate whose note is not filed yet, or a GAP measurement). `problems()` fails on a structure id the register
lacks, a wave kind that cannot be read (or reads `other` for EVERY note, the same failure hidden), a lane that
finds nothing, and a total that is not work.py's own count of the cluster.

    python scripts/spec/ledger_plan.py --self-test   # every arm on planted registers, then the real one
                                                     # (the gate's audits and CI's `audits` job run this)
"""
from __future__ import annotations

import argparse
import collections
import datetime
import pathlib
import re
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import work  # noqa: E402  (the register's one reader)

RETIREMENT = "PB2108"
REVIEW_PROGRAM = "PB2119"
LEGACY_TAG = "legacy-byte-engine-final"

#: The plan's lanes, in the page's order: (key, name, definition of done). The words are R69's and plan §0's.
LANES = (
    ("retirement", "Legacy retirement",
     f"Every note of cluster {RETIREMENT} landed; the engine's last commit is kept by the tag {LEGACY_TAG}."),
    ("conformance", "Conformance to zero GAP",
     "Zero GAP in the traceability inventory: every normative rule closed by a spec-derived witness, which is "
     "v1.0's conformance half."),
    ("external", "The external repository",
     "Every slice of the owner-approved §8.13 external-repository design landed."),
    ("review", "The architecture review R0-R5",
     "R5 closed: the battery, the GnuCOBOL differential and the performance re-measure after the last "
     "restructuring wave, with the architecture drift tests in the suite."),
    ("gates", "The gates between the lanes",
     "Every gate passed, the last being v1.0: P15 Cut 3 with the §4.2.16 conformance documentation."),
    ("pacing", "Pacing",
     "Work runs with the appropriate models up to the owner's weekly cap, then pauses; never past the session "
     "window (R69 §5-§6)."),
)

#: The phases of DESIGN-architecture-review.md §3, in order: (key, name, the phase notes whose titles do not name it).
PHASES = (
    ("R0", "Baseline and oracle", ()),
    ("R1", "Target architecture", ()),
    ("R2", "Review fleet", ()),
    ("R3", "Restructuring waves", (REVIEW_PROGRAM,)),
    ("R4", "Modernization", ()),
    ("R5", "Close", ()),
)
#: A title that names its phase: `<id> — R0 census`, `<id> — R1, the target architecture`. The lookahead keeps
#: `R69` (an owner-decision id) and `R0-0053` (a census finding id) out.
PHASE_IN_TITLE = re.compile(r"\A\S+ — (?P<phase>R\d)(?=[ ,:])")
#: An external-repository slice: `PB2099 — external repository slice 3: The host entry …`.
EXTERNAL_SLICE = re.compile(r"\A\S+ — external repository slice (?P<n>\d+)\b")
#: R1's state as the design states it: the `> **Status: DRAFT 3**` line under DESIGN-architecture-review.md's §8.
R1_DESIGN = REPO / "docs" / "rearchitecture" / "DESIGN-architecture-review.md"
R1_STATUS = re.compile(r"^## 8\. [^\n]*\n+> \*\*Status: (?P<status>[^*]+?)\*\*", re.M)

#: The gates between the lanes, in order: (label, how it is measured). ("note", id): the note's status.
#: ("title", regex): the note whose title matches, once one is filed. ("gap", n): passed when GAP <= n; None means
#: the threshold is the owner's judgement (R69 §2 "near zero" names no number), so the page shows GAP and waits.
GATES = (
    ("R1 approved by the owner", ("note", "PB2118")),
    ("The oracle's differences explained", ("note", "PB2152")),
    ("D10's second half", ("note", "PB2151")),
    ("GAP near zero (R2/R3 over binding and code generation start)", ("gap", None)),
    ("Zero GAP", ("gap", 0)),
    ("v1.0: P15 Cut 3, the runtime namespace flip", ("title", r"\A\S+ — P15 Cut 3\b")),
)

#: R0's instruments write their records here, one file per measured commit, named by that commit's sha:
#: (label, folder, glob, a suffix the glob also matches that is NOT a record).
ARCH_EVIDENCE = REPO / "docs" / "rearchitecture" / "evidence"
R0_EVIDENCE = (("census", "arch-census", "*.json", ".findings.json"),
               ("oracle", "arch-oracle", "*.manifest.json", None),
               ("perf baseline", "perf-baseline", "*.md", None))

#: The status columns the wave table always shows; any other status appears when a note carries it.
BASE_STATUSES = ("open", "blocked", "landed")
OTHER_WAVE = "other"


def _git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=REPO, capture_output=True, text=True,
                          encoding="utf-8", errors="replace").stdout.strip()


def wave_prefixes() -> dict[str, str]:
    """The R3 wave kinds, as the filing generator spells them at the head of a note's title (`Delete:`, `Unify:` …).

    ⛔ IMPORTED, NEVER COPIED: `scripts/arch/file_census_notes.py` PREFIX is the contract the notes are written to,
    so a wave kind it gains (Extract, Data-ize) is counted here the day it files its first note."""
    sys.path.insert(0, str(REPO / "scripts" / "arch"))
    import file_census_notes  # noqa: PLC0415
    return dict(file_census_notes.PREFIX)


def wave_kind(item: dict, prefixes: dict[str, str]) -> str | None:
    """The note's wave kind (its prefix without the colon), `other` when the title carries none, or None when the
    title is not `<id> — …` at all, so no kind can be read from it."""
    head = f"{item.get('id')} — "
    title = str(item.get("title", ""))
    if not title.startswith(head):
        return None
    rest = title[len(head):]
    return next((p.rstrip(":") for p in prefixes.values() if rest.startswith(p)), OTHER_WAVE)


def newest_record(folder: str, pattern: str, exclude: str | None) -> dict | None:
    """The newest record in an R0 evidence folder, by the date of the commit that added it (a checkout's mtimes are
    the checkout's, not the record's)."""
    files = [p for p in (ARCH_EVIDENCE / folder).glob(pattern) if not (exclude and p.name.endswith(exclude))]
    if not files:
        return None
    dated = [(_git("log", "-1", "--format=%cI", "--", p.relative_to(REPO).as_posix()), p.name, p) for p in files]
    when, _, p = max(dated)
    return {"sha": p.name.split(".")[0][:12], "date": when[:10]}


def r1_status() -> str | None:
    """`DRAFT 3` while the design's §8 says so; None when the design carries no §8 status line yet."""
    m = R1_STATUS.search(R1_DESIGN.read_text(encoding="utf-8")) if R1_DESIGN.exists() else None
    return m["status"].strip() if m else None


def finished(status: str) -> bool:
    return status in work.TERMINAL_STATUSES


def measure(items: list[dict], prefixes: dict[str, str], gap: int, evidence: bool = True) -> dict:
    """Every lane but pacing, read off `items` (the register) and the inventory's `gap`."""
    by_id = {i.get("id"): i for i in items}

    def status(nid: str) -> str:
        return str(by_id.get(nid, {}).get("status", "missing"))

    def title(nid: str) -> str:
        t = str(by_id.get(nid, {}).get("title", ""))
        return t.split(" — ", 1)[1] if " — " in t else t

    def cluster(lead: str) -> dict:
        members = sorted(work.cluster_members(items, lead), key=lambda i: work.id_order(str(i.get("id"))))
        live = [i for i in members if not finished(str(i.get("status")))]
        return {"lead": lead, "members": members,
                "notes": [(str(i.get("id")), str(i.get("status"))) for i in members],
                "total": len(members), "unfinished": len(live),
                "waiting": sum(1 for i in live if work.open_blockers(i, by_id))}

    phases = []
    for key, name, listed in PHASES:
        titled = {str(i.get("id")) for i in items
                  if (m := PHASE_IN_TITLE.match(str(i.get("title", "")))) and m["phase"] == key}
        ids = sorted(titled | set(listed), key=work.id_order)
        phases.append({"key": key, "name": name, "notes": [(nid, status(nid)) for nid in ids]})

    retire, review = cluster(RETIREMENT), cluster(REVIEW_PROGRAM)
    grid: dict[str, collections.Counter] = {k: collections.Counter()
                                            for k in [*(p.rstrip(":") for p in prefixes.values()), OTHER_WAVE]}
    unreadable, gates_waited = [], collections.Counter()
    for it in review["members"]:
        k = wave_kind(it, prefixes)
        if k is None:
            unreadable.append(str(it.get("id")))
        else:
            grid[k][str(it.get("status"))] += 1
        if not finished(str(it.get("status"))):
            gates_waited.update(work.open_blockers(it, by_id))
    present = {s for c in grid.values() for s in c}
    statuses = [*BASE_STATUSES, *sorted(present - set(BASE_STATUSES), key=lambda s: (finished(s), s))]
    review.update(grid=grid, statuses=statuses, unreadable=unreadable,
                  table_total=sum(sum(c.values()) for c in grid.values()),
                  blockers=[(b, title(b), c) for b, c in sorted(gates_waited.items(), key=lambda kv: work.id_order(kv[0]))])

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
                          "status": ("landed" if arg is not None and gap <= arg else "open")})

    program = {nid for p in phases for nid, _ in p["notes"]} | {nid for nid, _ in retire["notes"] + review["notes"]}
    landed = sum(1 for nid in program if finished(status(nid)))
    waiting = sum(1 for nid in program if not finished(status(nid)) and work.open_blockers(by_id.get(nid, {}), by_id))
    tag = _git("rev-parse", "--verify", "--quiet", "--short=9", f"{LEGACY_TAG}^{{commit}}") if evidence else ""
    return {"phases": phases, "retire": retire, "review": review, "slices": slices, "gates": gates,
            "r1_status": r1_status() if evidence else None, "legacy_tag": tag or None,
            "evidence": ([(label, newest_record(folder, pat, exc)) for label, folder, pat, exc in R0_EVIDENCE]
                         if evidence else []),
            "counts": {"total": len(program), "landed": landed, "open": len(program) - landed, "waiting": waiting,
                       "retire_landed": retire["total"] - retire["unfinished"], "retire_total": retire["total"],
                       "external_landed": sum(1 for s in slices if finished(s[2])), "external_total": len(slices)}}


def measure_pacing() -> dict | None:
    """The RUNNING account's newest meter reading against its caps, or None when this machine has no coordination
    directory (CI, a fresh clone) or that account has no reading. The directory, the readings file and the account are
    the orchestrator's (`coord.py`, `budget.py`, `account.py`): two Claude accounts record into the one file, and a
    reading of the other account is not this one's meter (kb/Work PB2478)."""
    sys.path.insert(0, str(REPO / "scripts" / "orchestrator"))
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


def problems(items: list[dict], prefixes: dict[str, str], plan: dict) -> list[str]:
    """Why the plan's lanes cannot be trusted, or [] — the drift test CLAUDE.md rule 5 asks of these structures."""
    bad = []
    if len(LANES) != len({k for k, _, _ in LANES}):
        bad.append("lanes: LANES keys must be distinct")
    if [k for k, _, _ in PHASES] != [f"R{i}" for i in range(6)]:
        bad.append(f"phases: PHASES must be R0-R5 in order, got {[k for k, _, _ in PHASES]}")
    ids = {i.get("id") for i in items}
    for key, _, listed in PHASES:
        bad += [f"phase-note-missing: {key} names {nid}, which is not a kb/Work note" for nid in listed if nid not in ids]
    bad += [f"gate-note-missing: '{label}' names {arg}, which is not a kb/Work note"
            for label, (how, arg) in GATES if how == "note" and arg not in ids]
    bad += [f"status-vocabulary: {s!r} is not a work.py status" for s in BASE_STATUSES if s not in work.STATUSES]
    if not prefixes or any(not p.endswith(":") for p in prefixes.values()):
        bad.append(f"wave-prefixes: file_census_notes.PREFIX must be non-empty `Kind:` spellings, got {prefixes}")
    rv = plan["review"]
    bad += [f"wave-kind-unreadable: {nid}'s title is not '<id> — …', so no wave kind can be read"
            for nid in rv["unreadable"]]
    readable = rv["table_total"]
    if readable and sum(rv["grid"][OTHER_WAVE].values()) == readable:
        bad.append(f"wave-kind-all-other: all {readable} notes of cluster {REVIEW_PROGRAM} read as '{OTHER_WAVE}' — "
                   f"their titles no longer carry file_census_notes.PREFIX")
    for c in (plan["retire"], rv):
        order = work.cluster_order(items, c["lead"])
        if c["total"] != order["named"] or c["unfinished"] != len(order["notes"]):
            bad.append(f"cluster-total: {c['lead']} shows {c['total']} notes, {c['unfinished']} unfinished; work.py "
                       f"counts {order['named']}, {len(order['notes'])} unfinished")
    if readable + len(rv["unreadable"]) != rv["total"]:
        bad.append(f"cluster-total: the wave table holds {readable} of the {rv['total']} notes of {REVIEW_PROGRAM}")
    if not plan["slices"]:
        bad.append("external-slices-none: no note's title reads '<id> — external repository slice N', so the lane "
                   "would render empty")
    numbers = [s[0] for s in plan["slices"]]
    if len(numbers) != len(set(numbers)):
        bad.append(f"external-slices-duplicate: two notes claim one slice number: {sorted(numbers)}")
    return bad


def live_problems(gap: int = 1) -> list[str]:
    items, prefixes = work.load(), wave_prefixes()
    return problems(items, prefixes, measure(items, prefixes, gap, evidence=False))


def self_test() -> int:
    """Fire every arm of `problems` on planted registers, then run it on the real one."""

    def note(nid, title, cluster=(), status="open", blocked_by=()):
        return {"id": nid, "title": f"{nid} — {title}", "cluster": list(cluster), "status": status,
                "blocked_by": list(blocked_by), "_file": f"{nid}.md"}

    prefixes = {"delete": "Delete:", "unify": "Unify:"}
    gate_notes = [note(arg, "a gate") for _, (how, arg) in GATES if how == "note"]
    good = [*gate_notes, note(RETIREMENT, "lead", [RETIREMENT], "landed"), note(REVIEW_PROGRAM, "the Delete program"),
            note("PB9001", "Delete: x", [REVIEW_PROGRAM]), note("PB9002", "Unify: y", [REVIEW_PROGRAM], "landed"),
            note("PB9003", "R0 census"), note("PB9004", "something", [REVIEW_PROGRAM], blocked_by=["PB9001"]),
            note("PB9006", "external repository slice 1: z", status="landed"),
            note("PB9007", "external repository slice 2: w"), note("PB9008", "P15 Cut 3 itself")]
    results = []

    def case(name, items, expect, mutate=None):
        plan = measure(items, prefixes, 5, evidence=False)
        if mutate:
            mutate(plan)
        got = problems(items, prefixes, plan)
        ok = (not got) if expect is None else any(g.startswith(expect) for g in got)
        results.append(ok)
        print(f"{'✓' if ok else '✗'} {name}: {got or 'clean'}")

    case("clean planted register", good, None)
    p = measure(good, prefixes, 5, evidence=False)
    g = p["review"]["grid"]
    shape = (g["Delete"]["open"], g["Unify"]["landed"], g[OTHER_WAVE]["open"], p["review"]["blockers"],
             p["phases"][0]["notes"], p["phases"][3]["notes"], [s[:3] for s in p["slices"]],
             [x["status"] for x in p["gates"]], p["counts"]["external_landed"], p["counts"]["waiting"])
    want = (1, 1, 1, [("PB9001", "Delete: x", 1)], [("PB9003", "open")], [(REVIEW_PROGRAM, "open")],
            [(1, "PB9006", "landed"), (2, "PB9007", "open")], ["open", "open", "open", "open", "open", "open"], 1, 1)
    results.append(shape == want)
    print(f"{'✓' if shape == want else '✗'} measured: wave grid, blockers, phases by title and by structure, slices, "
          f"gates, counts: {shape}")
    zero = measure(good, prefixes, 0, evidence=False)["gates"]
    ok = [x["status"] for x in zero][3:5] == ["open", "landed"]
    results.append(ok)
    print(f"{'✓' if ok else '✗'} zero GAP passes the zero-GAP gate and leaves 'near zero' to the owner")
    case("phase-note-missing", [i for i in good if i["id"] != REVIEW_PROGRAM], "phase-note-missing")
    case("gate-note-missing", [i for i in good if i["id"] != "PB2152"], "gate-note-missing")
    case("wave-kind-unreadable", [*good, {**note("PB9005", "x", [REVIEW_PROGRAM]), "title": "no dash"}],
         "wave-kind-unreadable")
    case("wave-kind-all-other", [i for i in good if i["id"] not in ("PB9001", "PB9002")], "wave-kind-all-other")
    case("cluster-total", good, "cluster-total", lambda q: q["review"].update(total=q["review"]["total"] + 1))
    case("external-slices-none", [i for i in good if i["id"] not in ("PB9006", "PB9007")], "external-slices-none")
    case("external-slices-duplicate", [*good, note("PB9009", "external repository slice 2: again")],
         "external-slices-duplicate")

    live = live_problems()
    results.append(not live)
    print(f"{'✓' if not live else '✗'} the real register: {live or 'clean'}")
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
    print("✓ the plan's lanes read cleanly off the register" if not bad else f"⛔ {len(bad)} problem(s)")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
