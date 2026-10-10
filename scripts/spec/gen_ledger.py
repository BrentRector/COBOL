#!/usr/bin/env python3
"""Render the owner's **WiseOwl COBOL Completion Ledger** artifact from the repo — every figure COMPUTED.

⭐ WHAT THE PAGE IS (kb/Work PB2912, owner 2026-10-10: "update the ledger to contain this info and track it. Remove
unneeded info from ledger"). What remains to v1.0, measured: at the top the headline (rules closed with GAP; the known
compiler notes), then WHAT REMAINS, one row per bucket (the known compiler defects and analyses with their harm, the
GAP rows and the notes that hold them, the rest of v1.0's gates, and the work the owner HELD until zero GAP and zero
known compiler defects, kb/Work PB2911), each with its notes ready / waiting / blocked and the lane that plans them,
LOUD flags for a GAP holder outside the GAP campaign, an open compiler note no lane plans and a GAP row nobody claims,
and an estimate per bucket from named, measured inputs (UNMEASURED where one is missing); then the conformance lane
(hero, tiles, the GAP burn-down with the register's defect series beside it), the external repository's slices, the
conformance detail (what is proven green, how the rows stand, burn-down by clause, the §4.2.16 posture), and pacing.
The plan half is MEASURED in `ledger_plan.py` (buckets, lanes, estimate inputs, its own `--self-test`) and RENDERED
here. Finished and held work (the legacy retirement, the architecture review's phases, the adjudication and A.1
documentation veins) is not on the page: the held work is one row of WHAT REMAINS.

⛔ WHY THIS EXISTS. The ledger is the owner's live status page, and it was maintained by HAND: a session read the
inventory, the work register, the A.1 audit and plan §0, then retyped ~60 numbers into a 460-line HTML file. Two
failure modes follow from that shape and both had already happened elsewhere in this repo:

  * **A hand-copied number is a remembered number.** `gen_conformance_notes.py`'s docstring records the same
    lesson from the vault view — §15 claimed 533 GAP where the live number was 481, and the stale view was still
    read as current. A status page nobody can re-derive is worse than none, because it is believed.
  * **A hand-maintained page grows into a work list.** CLAUDE.md rule 8 permits exactly one work register
    (`kb/Work/`). This page is a DERIVED VIEW of that register and of the inventory — never a tracker — and the
    only way to keep it that way is to make it impossible to author anything here that is not read off a
    measured artifact. Everything on the rendered page is computed by this file.

So a refresh is now: run this script, then publish `--out` to the artifact's existing URL. No retyping. The URL is the
running Claude account's (an artifact belongs to the account that published it): `ledger_state.py` keeps one per
account in the coordination directory, and this script prints it after a render (kb/Work PB2481).

    python scripts/spec/gen_ledger.py                       # write the default --out, <coord>/ledger.html
    python scripts/spec/gen_ledger.py --out ledger.html     # write somewhere else
    python scripts/spec/gen_ledger.py --check               # non-zero if --out is stale (mirrors
                                                            # gen_conformance_notes.py --check); this and a
                                                            # write both refuse when ledger_plan.problems()
                                                            # finds the lanes unreadable

⭐ INPUTS, each one an artifact something else already gates:

    tests/version-matrix/traceability-inventory.json   rows, states, verdicts, kinds, clause buckets
    kb/Work/*.md frontmatter                           via work.py's own load()/actionable() — IMPORTED, never
                                                       re-implemented, so the ledger's "actionable" is the same
                                                       predicate `work.py next` and session-probe print
    scripts/spec/audit_annex_a1.py --json              the A.1 register: items, scope, discharged, remaining
    docs/CONFORMANCE.md §2 · §4 · §5                   A.3 dispositions, the documented-non-support facilities,
                                                       the A.4 optional-module posture
    docs/COBOLNET_REARCHITECTURE_PLAN.md §0            the `⛔ BATTERY REFERENCE — CURRENT` bullet: number, date,
                                                       tree, and every leg's measured result
    docs/VERSION_CHANGE_REFERENCE.md                   the anchor dispositions (todo · gated · ref-only · pinned)
    tests/version-matrix/constructs.json               the matrix registry size
    tests/conformance/**                               the golden corpus, by edition
    kb/Work, through ledger_plan.py                    what remains: the buckets, each note's lane (`.agent-fleet.json`'s
                                                       fix-lane population, the campaign and held clusters of the
                                                       owner's decision PB2911), the external repository's slices
                                                       and v1.0's gates
    <coord>/readings.json + model_rules.json          pacing and the estimate: the newest meter reading against the
    + <coord>/train-measurements.jsonl                 owner's caps, the burn since the week's first reading, the
    + fix_clusters.py                                  batched trains' landing rate and the fix lane's own groups;
                                                       "not available" / UNMEASURED where this machine has no
                                                       coordination directory or submodule (so a page rendered in CI
                                                       says so; NOT stamp inputs — a new reading makes --check report
                                                       the page stale, which it is)
    git                                                HEAD sha + date, and how far HEAD has drifted from the
                                                       battery's tree (which is what makes "a battery is owed"
                                                       a measurement rather than an opinion)

⭐ THE TREND IS DATA, NOT MEMORY — `docs/rearchitecture/evidence/ledger-trend.json`. A burn-down chart is the one
thing on the page that cannot be recomputed from the current tree: yesterday's GAP is gone. So the series lives in
a small committed file, one point per battery or measurement-moving landing (`sha`, `date`, `gap`, `closed`, `dns`,
`label`, `battery`, and the register's series `ledger_plan.REGISTER_KEYS`: open compiler notes, defects, wrong answers,
crashes, GAP-holder notes; kb/Work PB2912), and a WRITE run appends the current point when any of them moved. Every
field of a point is measured from that commit's own tree, so a point missed between runs is measured the same way
(`missed_points`), and `--backfill-register` added the register fields to the points recorded before they existed,
each from its own sha's `kb/Work` (a point whose tree has no frontmatter register carries none). `--check` never
writes, and it reads the trend file as it is on disk: a measurement that moved with no point recorded in the FILE is
stale even when the page matches. ⛔ Matching the page alone was self-confirming (kb/Work PB2118 Draft 7, H3): the
render appends the current point in memory, so a page rendered by a write run whose trend-file append was then
reverted (or never committed) matched its own re-render, and the check passed with the point missing from the only
place it survives. The file's `program` list (the re-architecture program's counts, 2026-10-07 to 2026-10-08) is
frozen history: the program is held (PB2911) and the page no longer draws it, so nothing appends to it.

⛔ NOTHING ON THE PAGE IS HAND-WRITTEN. The "In flight right now" narrative it carried until 2026-10-10 went stale in a
day (PB2912), and a live in-flight view is the operator's (`scripts/prune_worktrees.py --brief`, minutes per run).

⚠ WHAT THIS SCRIPT DOES NOT DO: it does not decide anything. Every verdict, disposition and gate result it prints
was decided by the artifact it read. If a figure here looks wrong, the fix is in that artifact, never here.
"""
from __future__ import annotations

import argparse
import collections
import html
import json
import pathlib
import re
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "orchestrator"))
import coord  # noqa: E402  (names the coordination directory)
import ledger_plan  # noqa: E402  (the plan's lanes: measured there, rendered here)

INVENTORY_REL = "tests/version-matrix/traceability-inventory.json"
INVENTORY = REPO / INVENTORY_REL
CONSTRUCTS = REPO / "tests" / "version-matrix" / "constructs.json"
CONFORMANCE = REPO / "docs" / "CONFORMANCE.md"
PLAN = REPO / "docs" / "COBOLNET_REARCHITECTURE_PLAN.md"
VCR = REPO / "docs" / "VERSION_CHANGE_REFERENCE.md"
CORPUS = REPO / "tests" / "conformance"
TREND = REPO / "docs" / "rearchitecture" / "evidence" / "ledger-trend.json"
# The page is the orchestrator's: rendered into the coordination directory beside ledger-published.json, where the
# units render it and the attended session publishes it (design section 14). Named without creating the directory.
COORD_DIR = coord.coord_path()
DEFAULT_OUT = COORD_DIR / "ledger.html"

TITLE = "WiseOwl COBOL Completion Ledger"

#: Verdicts that RESOLVE a row — the row still needs a spec-derived witness to close, but no further
#: adjudication. Mirrors `inventory-schema.json`'s `resolves` flag; kept as a literal here because this script
#: only ever READS, and importing the schema loader would drag the inventory validator into a renderer.
RESOLVING = ("CONFORMS", "DOCUMENTED-NON-SUPPORT")
#: Verdicts that name a DEFECT — the rows `DefectiveRowCoverageDriftTests` requires a live `kb/Work` note to own.
DEFECTIVE = ("PARTIAL", "NOT-IMPLEMENTED", "DIVERGES")

#: Display names for the clause buckets — the top-level clause titles of ISO/IEC 1989:2023 exactly as
#: specs/ISO_COBOL.md prints them (`## N Title`). Presentation only — a clause missing from this map still renders,
#: with no subtitle. It is deliberately NOT a list of clauses to work. (A static map, not read from the spec at run
#: time: the spec is a private submodule that CI does not check out, and `--check` must render identically there.)
CLAUSE_TITLE = {
    "1": "Scope", "2": "Normative references", "3": "Terms and definitions", "4": "Conformance",
    "5": "Description techniques", "6": "Reference format", "7": "Compiler directing facility",
    "8": "Language fundamentals", "9": "I-O, objects, and user-defined functions",
    "10": "Structured compilation group", "11": "Identification Division", "12": "Environment Division",
    "13": "Data Division", "14": "Procedure Division", "15": "Intrinsic functions", "16": "Standard classes",
    "A": "Annex A.1 (impl. docs)",
}


# ─────────────────────────────────────────────────────────────────────────────────────────────────────────────
# measurement
# ─────────────────────────────────────────────────────────────────────────────────────────────────────────────

def git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=REPO, capture_output=True, text=True,
                          encoding="utf-8", errors="replace").stdout.strip()


#: The paths whose CONTENT this page reports. The page is stamped with the last commit that touched one of
#: these — NOT with `git HEAD`.
#:
#: ⛔ THIS IS THE DIFFERENCE BETWEEN A GATE AND A PERMANENTLY-RED GATE, and it is worth the extra call. Stamping
#: HEAD makes every unrelated commit — a DEVLOG paragraph, this generator's own source, the narrative fragment,
#: a session's WIP checkpoint — render a *correct* page as stale, and a check that is red for reasons nobody
#: acted on is one people learn to ignore (feedback_green_gates_arent_evidence, in its mirror image). Stamping
#: the last input-touching commit also gives the reader the sha they actually want: the tree the NUMBERS came
#: from. Deliberately absent from this set: the rendered page and the trend series — including either would make
#: regenerating move the stamp, so `--check` would never converge.
STAMP_PATHS = (
    "tests/version-matrix/traceability-inventory.json",
    "tests/version-matrix/inventory-schema.json",
    "tests/version-matrix/constructs.json",
    "tests/conformance",
    "kb/Work",
    "docs/CONFORMANCE.md",
    "docs/COBOLNET_REARCHITECTURE_PLAN.md",
    "docs/VERSION_CHANGE_REFERENCE.md",
    ".agent-fleet.json",
)


def measure_head() -> dict:
    """The tree this page describes: the last commit that touched a measured input (see :data:`STAMP_PATHS`)."""
    line = git("log", "-1", "--format=%h|%cI", "--abbrev=8", "--", *STAMP_PATHS)
    if not line:
        raise SystemExit("⛔ no commit touches any measured input — is this a git checkout of the repo?")
    sha, _, iso = line.partition("|")
    return {"sha": sha, "date": iso[:10]}


def measure_inventory() -> dict:
    rows = json.loads(INVENTORY.read_text(encoding="utf-8"))
    verdict = collections.Counter(r["verdict"] for r in rows)
    kind = collections.Counter(r["kind"] for r in rows)

    def cell(v: str, state: str) -> int:
        return sum(1 for r in rows if r["verdict"] == v and r["state"] == state)

    closed = sum(1 for r in rows if r["state"] == "OK")
    resolved_owed = sum(cell(v, "GAP") for v in RESOLVING)
    defective = sum(verdict[v] for v in DEFECTIVE)
    unadjudicated = verdict[""]

    # ⛔ The four hero bands must PARTITION the inventory. A composition that silently drops a verdict class
    # would under-report the work and the page would still look tidy, which is the failure this whole file
    # exists to prevent — so it is asserted rather than trusted.
    if closed + resolved_owed + defective + unadjudicated != len(rows):
        raise SystemExit(
            f"⛔ the hero bands do not partition the inventory ({closed}+{resolved_owed}+{defective}+"
            f"{unadjudicated} != {len(rows)}) — a verdict exists that this renderer does not classify: "
            f"{sorted(set(verdict) - set(RESOLVING) - set(DEFECTIVE) - {''})}")

    buckets: dict[str, collections.Counter] = collections.defaultdict(collections.Counter)
    for r in rows:
        key = "A" if r["kind"] == "DOC" else (re.match(r"^([A-Za-z]?\d+)", r["section"] or "?") or [None, "?"])[1]
        b = buckets[key]
        b["n"] += 1
        b["adjudicated"] += 1 if r["verdict"] else 0
        b["gap"] += 1 if r["state"] == "GAP" else 0
        if r["state"] == "OK":
            b["closed"] += 1
        elif r["verdict"]:
            b["adjopen"] += 1
        else:
            b["unadj"] += 1

    return {
        "rows": len(rows), "closed": closed, "gap": sum(1 for r in rows if r["state"] == "GAP"),
        "adjudicated": sum(1 for r in rows if r["verdict"]),
        "kind": kind, "verdict": verdict,
        "conforms_closed": cell("CONFORMS", "OK"), "conforms_owed": cell("CONFORMS", "GAP"),
        "dns_closed": cell("DOCUMENTED-NON-SUPPORT", "OK"), "dns_owed": cell("DOCUMENTED-NON-SUPPORT", "GAP"),
        "resolved_owed": resolved_owed, "defective": defective, "unadjudicated": unadjudicated,
        "doc_rows": kind["DOC"],
        "doc_verdicted": sum(1 for r in rows if r["kind"] == "DOC" and r["verdict"]),
        "doc_closed": sum(1 for r in rows if r["kind"] == "DOC" and r["state"] == "OK"),
        "buckets": buckets,
    }


def measure_work() -> dict:
    """The register's own numbers, through the register's own predicate.

    ⛔ `actionable()` IS IMPORTED, NOT REIMPLEMENTED. `work.py`'s docstring records what a second copy of that
    predicate costs: the `Fix next` Bases view and `work.py` both read `wrong_answer or crashes` and both were
    wrong the same way, hiding nine open items for months. A ledger that computed its own "actionable" would be
    a third copy — and the one people quote."""
    import work  # noqa: PLC0415  (deliberately late: this module is a renderer, work.py owns the register)
    items = work.load()
    return {
        "items": len(items),
        "kind": collections.Counter(i.get("kind") for i in items),
        "status": collections.Counter(i.get("status") for i in items),
        "actionable": work.actionable(items),
        "owner_parked": [i for i in items if i.get("status") == "owner"],
    }


def measure_annex_a1() -> dict:
    """A.1 coverage, from `audit_annex_a1.py --json`'s one machine-readable line.

    Run as a SUBPROCESS on purpose: that `JSON {...}` line is the contract the audit publishes for exactly this
    (its own comment names the C# gate that reads it), and importing the module would couple this renderer to
    the audit's internals and to its stdout."""
    out = subprocess.run([sys.executable, str(REPO / "scripts" / "spec" / "audit_annex_a1.py"), "--json"],
                         cwd=REPO, capture_output=True, text=True, encoding="utf-8", errors="replace").stdout
    line = next((l for l in out.splitlines() if l.startswith("JSON ")), None)
    if line is None:
        raise SystemExit("⛔ audit_annex_a1.py --json printed no JSON line — the ledger cannot invent A.1 coverage")
    d = json.loads(line[5:])
    return {"items": d["items"], "scope": d["documented_required"], "discharged": d["discharged"],
            "remaining": d["remaining"], "findings": d["findings"], "unreachable": d["unreachable"]}


def _tables(body: str) -> list[list[str]]:
    rows = [l.strip() for l in body.splitlines() if l.strip().startswith("|")]
    rows = [l for l in rows if not re.match(r"^\|[\s:|-]+\|$", l)]
    return [[c.strip() for c in l.strip("|").split("|")] for l in rows]


def _section(text: str, num: str) -> str:
    m = re.search(rf"^## {num}\.[^\n]*\n(.*?)(?=^## |\Z)", text, re.M | re.S)
    if m is None:
        raise SystemExit(f"⛔ docs/CONFORMANCE.md has no §{num} — the ledger reads its posture from there")
    return m.group(1)


def _disposition(cell: str) -> str:
    c = cell.replace("*", "").strip().lower()
    for k in ("claimed", "partial", "not claimed", "n/a"):
        if c.startswith(k):
            return k
    return "other"


def measure_conformance() -> dict:
    """A.3 / A.4 posture and the documented-non-support facilities, straight off `docs/CONFORMANCE.md`.

    ⚠ `_disposition` anchors on the START of the whole cell, not on a substring search. A `"claimed" in cell`
    test would score every *Not claimed* row as claimed — the single most flattering way this page could be
    wrong, and the reason the classifier is one function with one rule rather than a chain of `in` tests."""
    text = CONFORMANCE.read_text(encoding="utf-8")
    a3 = _tables(_section(text, "2"))
    a4 = _tables(_section(text, "5"))
    facilities = re.findall(r"^\d+\.\s+\*\*(.+?)\*\*", _section(text, "4"), re.M)
    return {
        "a3_rows": len(a3) - 1,
        "a3": collections.Counter(_disposition(r[3]) for r in a3[1:]),
        "a4_rows": len(a4) - 1,
        "a4": collections.Counter(_disposition(r[2]) for r in a4[1:]),
        "a4_by": {d: [r[0] for r in a4[1:] if _disposition(r[2]) == d]
                  for d in ("claimed", "partial", "not claimed")},
        "facilities": facilities,
    }


#: The legs of the comprehensive battery, as (label, regex over the §0 bullet, formatter).
BATTERY_LEGS = (
    ("Conformance (greenfield, full)", r"Conformance \*\*(\d[\d,]*) / (\d[\d,]*)\*\*", "{0} / {1}"),
    ("Unit", r"Unit \*\*(\d[\d,]*) / (\d[\d,]*)\*\*", "{0} / {1}"),
    ("Characterization", r"characterization \*\*(\d[\d,]*) / (\d[\d,]*)\*\*", "{0} / {1}"),
    ("NIST CCVS", r"NIST \*\*(\d[\d,]*) MATCH / (\d[\d,]*) REGRESSION\*\*", "{0} MATCH / {1} regr."),
    ("GnuCOBOL differential", r"differential \*\*(\d[\d,]*) cases\*\*.{0,120}?(\d+) PER-CASE FLIP",
     "{0} cases · {1} flips"),
)


def measure_battery(stamp_sha: str) -> dict:
    """Parse plan §0's `⛔ BATTERY REFERENCE — CURRENT` bullet — the single-write record of the last
    comprehensive run — and measure how far HEAD has drifted from the tree it was run on.

    ⛔ THE BULLET IS THE RECORD AND THIS IS ONLY A READER. If a leg's number cannot be found the row says so
    rather than being dropped: a leg that silently vanishes from a gate table reads as a gate that was not owed
    (feedback_verdict_evidence_invariant), and that is the one lie a status page must not tell."""
    text = PLAN.read_text(encoding="utf-8")
    m = re.search(r"^- \*\*⛔ BATTERY REFERENCE — CURRENT.*?(?=^- \*\*)", text, re.M | re.S)
    if m is None:
        raise SystemExit("⛔ plan §0 has no '⛔ BATTERY REFERENCE — CURRENT' bullet — the ledger reads the gate "
                         "standing from there and will not guess it")
    blk = m.group(0)
    n = re.search(r"battery #(\d+)", blk)
    date = re.search(r"(\d{4}-\d{2}-\d{2})", blk)
    sha = re.search(r"`([0-9a-f]{7,40})`", blk)
    if not (n and date and sha):
        raise SystemExit("⛔ the CURRENT battery bullet does not carry a #number, a date and a `sha` — "
                         "fix the bullet (it is the single-write record) or this reader")
    legs = []
    for label, rx, fmt in BATTERY_LEGS:
        mm = re.search(rx, blk, re.S)
        # Re-format the captured digits rather than echoing them: plan §0 writes `5241`, this page writes
        # `5,241` everywhere else, and a table that groups its thousands in some rows and not others reads as
        # two tables.
        legs.append((label, fmt.format(*(f"{int(g.replace(',', '')):,}" for g in mm.groups()))
                     if mm else "not parsed from plan §0", bool(mm)))
    unread = [label for label, _, ok in legs if not ok]
    if unread:
        # ⛔ A leg the reader cannot find shows as UNREAD on the owner's page, and the page cannot say WHY. The bullet is
        # hand-written, so say it here, where whoever regenerates the page is looking (2026-10-05: the differential row
        # read UNREAD for two batteries because the bullet said `GnuCOBOL differential: 1323 cases, 43 per-case flips`).
        shapes = "; ".join(f"{label}: /{rx}/" for label, rx, _ in BATTERY_LEGS if label in unread)
        print(f"⛔ battery #{n.group(1)}: the CURRENT bullet in plan §0 does not carry {', '.join(unread)} in the shape "
              f"this reader parses. Reword the bullet to match: {shapes}", file=sys.stderr)
    totals = re.search(r"totals unchanged at\s*\n?\s*([\d/]+)", blk)
    # Measured to the STAMP commit, not to HEAD: "is a battery owed" must not answer differently because a
    # session happened to have an unrelated WIP commit on top.
    since = git("rev-list", "--count", f"{sha.group(1)}..{stamp_sha}")
    since_code = git("rev-list", "--count", f"{sha.group(1)}..{stamp_sha}", "--", "src", "tests")
    return {"n": n.group(1), "date": date.group(1), "sha": sha.group(1), "legs": legs,
            "totals": totals.group(1) if totals else "", "since": int(since or 0),
            "since_code": int(since_code or 0), "owed": int(since_code or 0) > 0}


def edition_order(name: str) -> int:
    """`85` is COBOL-1985 and sorts FIRST — a plain string sort puts it after 2023, and a plain int sort puts it
    in 85 AD. The corpus directories are named the way `--std` spells the editions, so the mapping lives here."""
    v = int(name)
    return v if v > 1900 else 1900 + v


def measure_corpus() -> dict:
    editions = {d.name: len(list(d.glob("*.out")))
                for d in sorted(CORPUS.iterdir(), key=lambda p: edition_order(p.name) if p.name.isdigit() else 0)
                if d.is_dir() and d.name != "negative"}
    vcr = VCR.read_text(encoding="utf-8")
    gate = [g for g in re.findall(r"<!--\s*gate:([A-Za-z0-9_\-]+)\s*-->", vcr)
            if g not in ("CONSTRUCT-ID", "id")]  # the two are the doc's own worked example, not anchors
    todo, ref, pin = vcr.count("<!-- todo -->"), vcr.count("<!-- ref-only -->"), vcr.count("<!-- pin-to-spec -->")
    return {
        "positive": sum(editions.values()), "negative": len(list((CORPUS / "negative").glob("*.err"))),
        "editions": editions,
        "constructs": len(json.loads(CONSTRUCTS.read_text(encoding="utf-8"))["constructs"]),
        "vcr_todo": todo, "vcr_gated": len(gate), "vcr_ref": ref, "vcr_pin": pin,
        "vcr_done": len(gate) + ref + pin, "vcr_total": len(gate) + ref + pin + todo,
    }


# ─────────────────────────────────────────────────────────────────────────────────────────────────────────────
# the trend series — the ONLY thing on this page the current tree cannot recompute
# ─────────────────────────────────────────────────────────────────────────────────────────────────────────────

def inventory_at(sha: str) -> list[dict] | None:
    """The inventory's rows AS COMMITTED at `sha`, or None when that tree has none."""
    r = subprocess.run(["git", "show", f"{sha}:{INVENTORY_REL}"], cwd=REPO,
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        return None
    try:
        return json.loads(r.stdout)
    except ValueError:
        return None


def inventory_counts_at(sha: str) -> dict | None:
    """GAP, closed and documented-non-support counts of the inventory AS COMMITTED at `sha`."""
    rows = inventory_at(sha)
    if rows is None:
        return None
    return {"gap": sum(1 for x in rows if x["state"] == "GAP"), "closed": sum(1 for x in rows if x["state"] == "OK"),
            "dns": sum(1 for x in rows if x["verdict"] == "DOCUMENTED-NON-SUPPORT")}


def notes_at(sha: str) -> list[dict]:
    """The register AS COMMITTED at `sha`: every `kb/Work/*.md` blob read in ONE `git cat-file --batch`, parsed by
    work.py's own frontmatter reader (a file without frontmatter is not a note, exactly as `work.load` skips it)."""
    import work  # noqa: PLC0415
    names = [n for n in git("ls-tree", "--name-only", f"{sha}:kb/Work").splitlines() if n.endswith(".md")]
    if not names:
        return []
    out = subprocess.run(["git", "cat-file", "--batch"], cwd=REPO, capture_output=True,
                         input="".join(f"{sha}:kb/Work/{n}\n" for n in names).encode("utf-8")).stdout
    items, at = [], 0
    while at < len(out):
        eol = out.index(b"\n", at)
        header = out[at:eol].split()
        if len(header) < 3 or header[1] != b"blob":
            at = eol + 1
            continue
        size = int(header[2])
        d = work.parse_frontmatter(out[eol + 1:eol + 1 + size].decode("utf-8", errors="replace"))
        if d is not None:
            items.append(d)
        at = eol + 1 + size + 1
    return items


def register_counts_at(sha: str) -> dict | None:
    """`ledger_plan.register_counts` of the register and inventory AS COMMITTED at `sha` (one rule for every tree)."""
    rows = inventory_at(sha) or []
    return ledger_plan.register_counts(notes_at(sha), {r["rule-id"] for r in rows if r.get("state") == "GAP"})


def missed_points(pts: list[dict]) -> list[dict]:
    """The inventory-moving commits since the series' last point that no generator run recorded.

    ⛔ A generator run measures ONE tree, so a session that landed three trains between runs recorded one point
    for three landings (2026-10-04: trains 1011 and 1012 were missing until the owner noticed the page stale).
    Every commit that touched the inventory file after the last recorded sha is measured from its own tree here,
    so the series is one point per GAP-moving landing no matter how rarely the generator runs. Each such point
    carries the register's series too, measured from the same tree (kb/Work PB2912)."""
    last = pts[-1] if pts else None
    if last is None or not git("rev-parse", "--verify", "--quiet", last["sha"]):
        return []
    log = git("log", "--reverse", "--format=%h|%cI", "--abbrev=8", f"{last['sha']}..HEAD", "--",
              INVENTORY_REL)
    out: list[dict] = []
    for line in filter(None, log.splitlines()):
        sha, _, iso = line.partition("|")
        counts = inventory_counts_at(sha)
        prev = out[-1] if out else last
        if counts is None or all(counts[k] == prev.get(k) for k in ("gap", "closed", "dns")):
            continue
        out.append({"sha": sha, "date": iso[:10], **counts,
                    "label": git("log", "-1", "--format=%s", sha)[:28].rstrip(), "battery": None,
                    **(register_counts_at(sha) or {})})
    return out


#: The keys whose movement records a point: the inventory's and the register's (kb/Work PB2912).
MOVING_KEYS = ("gap", "closed", "dns", *ledger_plan.REGISTER_KEYS)


def trend_series(inv: dict, head: dict, battery: dict, register: dict | None) -> tuple[list[dict], bool]:
    """Return (series to render, whether the current point is new).

    A point is appended only when the MEASUREMENT moved (the inventory's counts or the register's series) — a commit
    that changes neither adds no point, or the series would become a commit log with a y-axis. `--check` calls this
    too and simply does not write, so a moved-but-unrecorded measurement renders differently from the file on disk
    and reports as stale."""
    doc = json.loads(TREND.read_text(encoding="utf-8")) if TREND.exists() else {"points": []}
    pts = doc.get("points", [])
    missed = missed_points(pts)
    pts = [*pts, *missed]
    cur = {"sha": head["sha"], "date": head["date"], "gap": inv["gap"], "closed": inv["closed"],
           "dns": inv["verdict"]["DOCUMENTED-NON-SUPPORT"],
           "label": git("log", "-1", "--format=%s", head["sha"])[:28].rstrip(),
           "battery": int(battery["n"]) if head["sha"].startswith(battery["sha"][:8]) else None,
           **(register or {})}
    last = pts[-1] if pts else None
    moved = last is None or any(cur.get(k) != last.get(k) for k in MOVING_KEYS)
    # ⛔ A BATTERY ALWAYS GETS A POINT, even one that moved nothing. The chart's second job is saying which
    # point the last comprehensive gate was run on, and a battery that lands on an unchanged inventory (which
    # is the normal case — a test run closes no rows) would otherwise never appear, leaving the mark on some
    # earlier commit. That is precisely the "ungated number read as gated" failure the mark exists to prevent.
    is_battery = cur["battery"] is not None and not any(p.get("battery") == cur["battery"] for p in pts)
    if not (moved or is_battery) or (last is not None and last.get("sha") == cur["sha"]):
        return pts, bool(missed)
    return [*pts, cur], True


def backfill_register(pts: list[dict]) -> int:
    """Add the register's series to every point recorded before it existed, each measured from its own sha's tree
    (`register_counts_at`); a point whose sha is not in this clone, or whose tree has no frontmatter register, is left
    as it is. Returns how many points gained the fields."""
    filled = 0
    for p in pts:
        if "compiler_open" in p or not git("rev-parse", "--verify", "--quiet", f"{p['sha']}^{{commit}}"):
            continue
        counts = register_counts_at(p["sha"])
        if counts:
            p.update(counts)
            filled += 1
    return filled


def write_trend(series: list[dict]) -> None:
    doc = json.loads(TREND.read_text(encoding="utf-8")) if TREND.exists() else {}
    doc["points"] = series
    TREND.parent.mkdir(parents=True, exist_ok=True)
    TREND.write_text(json.dumps(doc, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


# ─────────────────────────────────────────────────────────────────────────────────────────────────────────────
# rendering
# ─────────────────────────────────────────────────────────────────────────────────────────────────────────────

def n(x) -> str:
    return f"{x:,}"


def e(s) -> str:
    return html.escape(str(s), quote=True)


def battery_point(pts: list[dict], bat: dict) -> int | None:
    """Index of the trend point the CURRENT battery ran on, or None when the series never recorded it.

    ⛔ Not "the first point carrying any battery flag": that read the OLDEST flagged point and labelled it with the
    CURRENT battery's number, so the page printed "-1506 since battery #85" (the delta from an August battery) and
    put battery #85's mark on an August point."""
    try:
        n_ = int(bat["n"])
    except (TypeError, ValueError):
        n_ = None
    sha = str(bat.get("sha", ""))[:8]
    for i in range(len(pts) - 1, -1, -1):
        p = pts[i]
        if (n_ is not None and p.get("battery") == n_) or (sha and str(p.get("sha", "")).startswith(sha)):
            return i
    return None


def gap_at(sha: str) -> int | None:
    """GAP of the inventory AS COMMITTED at `sha` — measured from that tree, never from the trend series, which
    records a point only when the generator happened to run and so can miss whole trains."""
    counts = inventory_counts_at(sha) if sha else None
    return counts["gap"] if counts else None


def trend_svg(pts: list[dict], bat: dict) -> str:
    battery_n = bat["n"]
    """A line of GAP-over-landings, drawn to ONE scale with every label naming a value the chart reaches.

    The y labels are the series' own min and max (not round numbers above and below them), so no tick can point
    at a value the data never took. Room is left in the viewBox for the outermost labels: the left gutter holds
    the value labels and the bottom band holds the dates."""
    if len(pts) < 2:
        return '<p class="note">Not enough recorded points to draw a trend yet.</p>'
    x0, x1, ytop, ybot, base = 46, 884, 26.0, 78.0, 88
    lo, hi = min(p["gap"] for p in pts), max(p["gap"] for p in pts)
    step = (x1 - x0) / (len(pts) - 1)
    xs = [x0 + step * i for i in range(len(pts))]
    ys = [(ytop + ybot) / 2 if hi == lo else ytop + (hi - p["gap"]) / (hi - lo) * (ybot - ytop) for p in pts]

    bi = battery_point(pts, bat)
    parts = [f'<line class="tl-grid" x1="{x0}" y1="{base}" x2="{x1}" y2="{base}"></line>']
    if bi is not None:
        parts.append(f'<line class="tl-mark" x1="{xs[bi]:.0f}" y1="18" x2="{xs[bi]:.0f}" y2="{base}"></line>')
    pl = " ".join(f"{x:.0f},{y:.0f}" for x, y in zip(xs, ys))
    parts.append(f'<polyline class="tl-line" points="{pl}"></polyline>')
    for i, (x, y) in enumerate(zip(xs, ys)):
        cls, r = ("tl-dot-b", 4.5) if i == bi else ("tl-dot", 3.5)
        parts.append(f'<circle class="{cls}" cx="{x:.0f}" cy="{y:.0f}" r="{r}"></circle>')
    yhi = ytop if hi != lo else (ytop + ybot) / 2
    parts.append(f'<text class="tl-lab-e" x="40" y="{yhi + 4:.0f}" text-anchor="end">{n(hi)}</text>')
    if hi != lo:
        parts.append(f'<text class="tl-lab-e" x="40" y="{ybot + 4:.0f}" text-anchor="end">{n(lo)}</text>')

    # ⛔ EXACTLY THREE x LABELS, AND NEVER TWO THAT CAN COLLIDE. The battery point gets its own centred label
    # only when it is at least two positions from either end; nearer than that its date band would overlap an
    # end label, so the battery is named INSIDE that end label instead. Labels that overlap are not a cosmetic
    # problem here — the chart's whole job is saying which point the last gate was run on.
    stamp = [p["date"][5:] for p in pts]
    at_start = bi is not None and bi <= 1
    at_end = bi is not None and bi >= len(pts) - 2
    lab_first = f'{stamp[0]} {pts[0].get("label", "")}'.strip()
    lab_last = f"{stamp[-1]} now"
    if at_start:
        lab_first = f"{stamp[0]} battery #{battery_n}" if bi == 0 else f"{lab_first} · battery #{battery_n}"
    elif at_end:
        lab_last = f"{lab_last} · battery #{battery_n}"
    parts.append(f'<text class="tl-lab" x="{x0}" y="104" text-anchor="start">{e(lab_first)}</text>')
    if bi is not None and not at_start and not at_end:
        parts.append(f'<text class="tl-lab" x="{xs[bi]:.0f}" y="104" text-anchor="middle">'
                     f'{e(stamp[bi])} battery #{e(battery_n)}</text>')
    parts.append(f'<text class="tl-lab" x="{x1}" y="104" text-anchor="end">{e(lab_last)}</text>')

    desc = " · ".join(f"{p['date']} {n(p['gap'])}" for p in pts)
    return (f'<svg viewBox="0 0 900 112" role="img" aria-label="GAP rows remaining per landing: {e(desc)}">'
            + "".join(parts) + "</svg>")


def clause_rows(buckets: dict) -> str:
    biggest = max(b["n"] for b in buckets.values())
    out = []
    for key, b in sorted(buckets.items(), key=lambda kv: (-kv[1]["n"], kv[0])):
        title = CLAUSE_TITLE.get(key)
        sub = f' <span class="dim">{e(title)}</span>' if title else ""
        segs = "".join(f'<span class="{c}" style="flex:{b[k]}"></span>'
                       for c, k in (("c1", "closed"), ("c2", "adjopen"), ("c3", "unadj")) if b[k])
        width = 100.0 * b["n"] / biggest
        out.append(
            f'<tr><td class="mono">§{e(key)}{sub}</td>'
            f'<td class="r num">{n(b["n"])}</td>'
            f'<td class="r num">{n(b["adjudicated"])}</td>'
            f'<td class="r num">{n(b["gap"])}</td>'
            f'<td class="cbar-track"><div class="cbar" style="width:{width:.1f}%">{segs}</div></td></tr>')
    return "\n        ".join(out)


#: A status's pill: finished is good, held on something is critical, everything else is still owed.
PILL = {"landed": "good", "retired": "good", "blocked": "crit", "owner": "crit", "missing": "crit"}


def status_pill(s: str) -> str:
    return f'<span class="pill {PILL.get(s, "warn")}">{e(s)}</span>'


#: The register series the trend file carries (ledger_plan.REGISTER_KEYS), as (key, legend label, line class).
REGISTER_LINES = (("compiler_open", "open compiler notes", "rl-0"), ("defects", "of them defects", "rl-1"),
                  ("gap_holders", "GAP-holding notes", "rl-4"), ("wrong_answer", "wrong answers", "rl-2"),
                  ("crashes", "crashes", "rl-3"))


def register_svg(pts: list[dict]) -> str:
    """The register's series over the trend points that carry it, on one scale from zero, beside the GAP line."""
    rp = [p for p in pts if "compiler_open" in p]
    if len(rp) < 2:
        only = rp[0] if rp else None
        return ('<p class="note">The register series starts at '
                + (f'{e(only["date"])} ({" · ".join(f"{n(only[k])} {lab}" for k, lab, _ in REGISTER_LINES if only.get(k) is not None)})'
                   if only else "the next recorded point") + '.</p>')
    x0, x1, ytop, ybot = 46, 884, 14.0, 88.0
    hi = max(p[k] for p in rp for k, _, _ in REGISTER_LINES if p.get(k) is not None) or 1
    step = (x1 - x0) / (len(rp) - 1)
    parts = [f'<line class="tl-grid" x1="{x0}" y1="{ybot:.0f}" x2="{x1}" y2="{ybot:.0f}"></line>']
    for key, _, cls in REGISTER_LINES:
        xy = [(x0 + step * i, ybot - p[key] / hi * (ybot - ytop)) for i, p in enumerate(rp) if p.get(key) is not None]
        if xy:
            parts.append(f'<polyline class="{cls}" points="{" ".join(f"{x:.0f},{y:.0f}" for x, y in xy)}"></polyline>')
    parts.append(f'<text class="tl-lab-e" x="40" y="{ytop + 4:.0f}" text-anchor="end">{n(hi)}</text>')
    parts.append(f'<text class="tl-lab-e" x="40" y="{ybot + 4:.0f}" text-anchor="end">0</text>')
    parts.append(f'<text class="tl-lab" x="{x0}" y="104" text-anchor="start">{e(rp[0]["date"][5:])}</text>')
    parts.append(f'<text class="tl-lab" x="{x1}" y="104" text-anchor="end">{e(rp[-1]["date"][5:])} now</text>')
    desc = " · ".join(f"{p['date']} {p['compiler_open']} open, {p.get('defects')} defects" for p in rp)
    return (f'<svg viewBox="0 0 900 112" role="img" aria-label="Open compiler notes per recorded point: {e(desc)}">'
            + "".join(parts) + "</svg>")


def register_legend(pts: list[dict]) -> str:
    last = next((p for p in reversed(pts) if "compiler_open" in p), {})
    return "".join(f'<span class="key"><span class="sw {cls}"></span>{e(label)} '
                   f'<span class="num">{n(last[k]) if last.get(k) is not None else "—"}</span></span>'
                   for k, label, cls in REGISTER_LINES)


# ── what remains ────────────────────────────────────────────────────────────────────────────────────────────────

UNMEASURED = '<span class="pill warn">UNMEASURED</span>'
HARM_WORDS = (("wrong_answer", "wrong answers"), ("crashes", "crashes"), ("rejects_legal_source", "reject legal source"),
              ("under_rejects", "under-reject"), ("silent", "silent"))


def lane_label(lane: str) -> str:
    names = dict((*ledger_plan.CAMPAIGNS, *ledger_plan.HELD))
    if lane == ledger_plan.FIX:
        return "fix lane"
    if lane == ledger_plan.NO_LANE:
        return "NO LANE"
    lead = lane.split(":", 1)[-1]
    return f'{"held" if lane.startswith("held:") else "campaign"} {lead} ({names.get(lead, "?")})'


def lane_text(lanes: collections.Counter) -> str:
    order = [ledger_plan.FIX, *(lead for lead, _ in ledger_plan.CAMPAIGNS),
             *(f"held:{lead}" for lead, _ in ledger_plan.HELD), ledger_plan.NO_LANE]
    out = []
    for lane in order:
        if lanes.get(lane):
            word = f'{e(lane_label(lane))} <span class="num">{n(lanes[lane])}</span>'
            out.append(f'<span class="pill crit">{word}</span>' if lane == ledger_plan.NO_LANE else word)
    return " · ".join(out) or '<span class="dim">no open note</span>'


def states_text(s: collections.Counter) -> str:
    return " · ".join(f'<span class="num">{n(s.get(k, 0))}</span> {k}' for k in ("ready", "waiting", "blocked"))


def harm_text(h: collections.Counter) -> str:
    return " · ".join(f'<span class="num">{n(h.get(k, 0))}</span> {w}' for k, w in HARM_WORDS)


def kinds_text(k: collections.Counter) -> str:
    return " · ".join(f'<span class="num">{n(v)}</span> {e(kind)}' for kind, v in k.most_common())


def alerts(rem: dict) -> str:
    """The LOUD flags: work no lane plans is the defect that kept GAP flat (kb/Work PB2911)."""
    c, g = rem["compiler"], rem["gap"]
    out = []
    if g["outside_campaign"]:
        out.append(f'⛔ <strong>{n(len(g["outside_campaign"]))} open note(s) hold GAP rows outside the GAP campaign</strong> '
                   f'(cluster <span class="mono">{e(ledger_plan.GAP_CAMPAIGN)}</span>), so no campaign wave plans them: '
                   f'<span class="mono">{e(" ".join(g["outside_campaign"]))}</span>')
    if c["no_lane"]:
        out.append(f'⛔ <strong>{n(len(c["no_lane"]))} open compiler note(s) no lane plans</strong> (the fix lane plans '
                   f'<span class="mono">.agent-fleet.json</span>\'s population; none of these is in a campaign or held '
                   f'cluster; {kinds_text(c["no_lane_kind"])}): '
                   f'<span class="mono">{e(" ".join(c["no_lane"]))}</span>')
    if g["unclaimed"]:
        out.append(f'⛔ <strong>{n(len(g["unclaimed"]))} GAP row(s) no open note claims</strong>: '
                   f'<span class="mono">{e(" ".join(g["unclaimed"]))}</span>')
    idle = [x for x in rem["gates"] if x["unplanned"]]
    if idle:
        out.append(f'⛔ <strong>{n(len(idle))} v1.0 gate note(s) only a held cluster or no lane plans</strong>: '
                   + " · ".join(f'{e(x["label"])} <span class="mono">{e(x["id"])}</span> '
                                f'({e(", ".join(lane_label(y) for y in x["lanes"]) or "no lane")})' for x in idle))
    if not out:
        return ('<p class="alert good">Every open compiler note, every GAP-holding note and every v1.0 gate note is '
                'planned by a lane, and every GAP row is claimed.</p>')
    return "\n  ".join(f'<p class="alert crit">{a}</p>' for a in out)


def fig(x, fmt: str) -> str:
    return UNMEASURED if x is None else f'<span class="num">{fmt.format(x)}</span>'


def estimate_row(label: str, est: dict | None, why: str = "") -> str:
    if est is None:
        return (f'<tr><td>{label}</td>' + f'<td class="r">{UNMEASURED}</td>' * 5 + f'<td class="dim">{why}</td></tr>')
    left = []
    if est["unplanned"]:
        left.append(f'{n(est["unplanned"])} note(s) no lane plans')
    if est["held_out"]:
        left.append(f'{n(est["held_out"])} held note(s)')
    return (f'<tr><td>{label}</td><td class="r">{fig(est["groups"], "{:,}")}</td><td class="r">{fig(est["waves"], "{:,}")}</td>'
            f'<td class="r">{fig(est["points"], "{:.1f}")}</td><td class="r">{fig(est["hours"], "{:.1f} h")}</td>'
            f'<td class="r">{fig(est["burn_points"], "{:.1f}")}</td>'
            f'<td class="dim">{e(", ".join(left) + (". " if left else "") + why)}</td></tr>')


def inputs_text(inputs: dict) -> str:
    rules, fg, rate, burn = inputs.get("rules"), inputs.get("fix_groups"), inputs.get("rate"), inputs.get("burn")
    items = []
    if fg is not None:
        items.append(f'the fix lane\'s groups: <span class="mono">fix_clusters.py</span> forms '
                     f'<span class="num">{n(len(fg.get("clusters", [])))}</span> groups and '
                     f'<span class="num">{n(len(fg.get("unsited", [])))}</span> unsited notes over '
                     f'<span class="mono">.agent-fleet.json</span>\'s population (a blocked defect it skips counts as a group of its own)')
    else:
        items.append(f'the fix lane\'s groups: {UNMEASURED} (the <span class="mono">tools/claude-skills</span> submodule '
                     f'is not checked out here)')
    if rules:
        w, c, cal = rules["wave"], rules["cost"], rules["calibration"]["tokens_per_point"]
        m = w["implementer_model"]
        items.append(f'the planner (<span class="mono">model_rules.json</span> <span class="mono">wave</span>): '
                     f'{n(w["max_notes_per_group"])} notes per group (a campaign\'s and a held cluster\'s groups are counted '
                     f'at this cap, a lower bound: a campaign clusters by the sites its notes name), {n(w["max_groups"])} groups '
                     f'per wave, {n(w["train_size"])} groups per train, implementers on {e(m)}')
        items.append(f'the cost constants (<span class="mono">cost</span>, <span class="mono">calibration</span>): a group '
                     f'{n(c["per_group_tokens"][m])} + {n(c["per_note_tokens"][m])} tokens per note at {n(cal[m])} {e(m)} '
                     f'tokens per weekly point; a lander {n(c["lander_tokens"])} at {n(cal[c["lander_model"]])} '
                     f'{e(c["lander_model"])} tokens per point, one per train')
    if rate:
        items.append(f'the landing rate (<span class="mono">train_measure.py</span>, {e(rate["gating"])} gating): '
                     f'<span class="num">{rate["per_h"]:.2f}</span> changes per hour, {n(rate["landed"])} changes in '
                     f'{rate["window_h"]:.1f} h over {n(rate["trains"])} trains')
    else:
        items.append(f'the landing rate: {UNMEASURED} (no <span class="mono">train-measurements.jsonl</span> on this machine)')
    if burn:
        items.append(f'the measured burn (<span class="mono">readings.json</span>, {e(burn["account"])}): '
                     f'<span class="num">{burn["per_h"]:.1f}</span> weekly points per hour, {burn["from_pct"]:g} % → '
                     f'{burn["to_pct"]:g} % in {burn["hours"]:.1f} h since the week\'s first reading')
    else:
        items.append(f'the measured burn: {UNMEASURED} (fewer than two moving meter readings this week on this machine)')
    items.append(f'new defects found per wave (the inflow): {UNMEASURED}')
    return "<br>".join(items)


def render_remaining(rem: dict, est: dict, cp: dict, inputs: dict) -> str:
    c, g, h = rem["compiler"], rem["gap"], rem["held"]
    verdicts = " · ".join(f'<span class="num">{n(v)}</span> {e(k)}' for k, v in g["verdicts"].most_common())
    biggest = (f'; the largest holder is <span class="mono">{e(g["biggest"][0][1])}</span> '
               f'({n(g["biggest"][0][0])} rows)' if g["biggest"] else "")
    gates = rem["gates"]
    gates_open = [x for x in gates if not ledger_plan.finished(x["status"])]
    gate_text = " · ".join(
        f'{e(x["label"])} '
        + (f'(GAP <span class="num">{n(x["gap"])}</span>) ' if "gap" in x else
           (f'<span class="mono">{e(x["id"])}</span> ' if x["id"] else ""))
        + status_pill(x["status"])
        + (f' <span class="pill crit">{e(", ".join(lane_label(y) for y in x["lanes"]) or "NO LANE")}</span>'
           if x["unplanned"] else "") for x in gates)
    held_text = " · ".join(f'<span class="mono">{e(x["lead"])}</span> {e(x["name"])}: <span class="num">{n(x["open"])}</span> '
                           f'open of {n(x["total"])}' for x in h["clusters"])
    rows = [
        (f'<strong>Known compiler defects and analyses</strong><br><span class="dim">open notes that are not '
         f'<span class="mono">process_only</span>: {kinds_text(c["kind"])}; harm {harm_text(c["harm"])}; of the '
         f'defects alone {harm_text(c["defect_harm"])}</span>', c),
        (f'<strong>The GAP rows</strong> (v1.0 is zero GAP, D13)<br><span class="dim"><span class="num">{n(g["rows"])}</span> '
         f'rows: {verdicts}; {n(g["witness_owed"])} wait only on a witness; held by the open notes counted here '
         f'({kinds_text(g["kind"])}; {n(g["process_only"])} process-only){biggest}</span>', g),
    ]
    trs = [f'<tr><td>{label}</td><td class="r num">{n(b["n"])}</td><td>{states_text(b["states"])}</td>'
           f'<td>{lane_text(b["lanes"])}</td></tr>' for label, b in rows]
    trs.append(f'<tr><td><strong>The rest of v1.0</strong><br><span class="dim">its gates: {gate_text}; version-change '
               f'anchors still <span class="mono">todo</span>: <span class="num">{n(cp["vcr_todo"])}</span> of '
               f'{n(cp["vcr_total"])}</span></td><td class="r num">{n(len(gates_open))}</td>'
               f'<td class="dim">gates still open</td>'
               f'<td>{lane_text(collections.Counter(y for x in gates_open if x["id"] for y in (x["lanes"] or [ledger_plan.NO_LANE])))}</td></tr>')
    trs.append(f'<tr><td><strong>Held until zero GAP and zero known defects</strong> '
               f'(owner 2026-10-10, <span class="mono">{e(ledger_plan.DECISION)}</span>)<br><span class="dim">{held_text}'
               f'</span></td><td class="r num">{n(h["n"])}</td><td>{states_text(h["states"])}</td>'
               f'<td>{lane_text(h["lanes"])}</td></tr>')
    tr_html = "\n        ".join(trs)
    est_rows = "\n        ".join([
        estimate_row("Known compiler defects and analyses", est["compiler"]),
        estimate_row("The GAP rows (their holders)", est["gap"]),
        estimate_row("The rest of v1.0", None, "no planner input sizes these gates until their notes are filed"),
        estimate_row("Held work, once released", est["held"]),
    ])
    return f"""<p>v1.0 is zero GAP (D13), P15 Cut 3 and the §4.2.16 user documentation; the owner put the compiler and zero GAP first (2026-10-10, <span class="mono">{e(ledger_plan.DECISION)}</span>). One row per bucket, counted from the register and the inventory; a note can count in more than one bucket (a defect that holds a GAP row). <em>Planned by</em> is the lane that plans each note: the fix lane (<span class="mono">.agent-fleet.json</span>'s population), a campaign lane, held, or none.</p>
  {alerts(rem)}
  <div class="tablecard">
    <table>
      <thead><tr><th>Bucket</th><th class="r">Open</th><th>Ready · waiting · blocked</th><th>Planned by</th></tr></thead>
      <tbody>
        {tr_html}
      </tbody>
    </table>
  </div>
  <h3>Estimate, from measured inputs</h3>
  <div class="tablecard">
    <table>
      <thead><tr><th>Bucket</th><th class="r">Groups</th><th class="r">Waves</th><th class="r">Weekly points (cost constants)</th><th class="r">Loop hours (landing rate)</th><th class="r">Weekly points at the measured burn</th><th>Not estimated</th></tr></thead>
      <tbody>
        {est_rows}
      </tbody>
    </table>
  </div>
  <p class="note">An ESTIMATE, not a measurement of the future: each figure is computed from the inputs below and is UNMEASURED when one of them is missing. Groups are what the planners would form now; waves = groups ÷ groups per wave; weekly points = each group's cost plus a lander per train; loop hours = groups ÷ the landing rate; points at the burn = those hours × the measured burn. The buckets overlap, so their rows do not add. Inputs: {inputs_text(inputs)}.</p>"""


def render_external(plan: dict) -> str:
    rows = "\n        ".join(
        f'<tr><td class="r num">{num}</td><td class="mono">{e(nid)}</td><td>{e(title.split(": ", 1)[-1])}</td>'
        f'<td>{status_pill(s)}</td></tr>' for num, nid, s, title in plan["slices"])
    c = plan["counts"]
    return (f'<p>The owner-approved §8.13 external-repository design, landed slice by slice (campaign '
            f'<span class="mono">PB1086</span>). <strong class="num">{n(c["external_landed"])}</strong> of '
            f'{n(c["external_total"])} slices landed.</p>\n'
            f'  <div class="tablecard"><table>\n'
            f'      <thead><tr><th class="r">Slice</th><th>Note</th><th>What it lands</th><th>Status</th></tr></thead>\n'
            f'      <tbody>\n        {rows}\n      </tbody>\n    </table></div>')


def render_pacing(pacing: dict | None, burn: dict | None) -> str:
    if pacing is None:
        return ('<p>Not available: the machine that rendered this page has no coordination directory with a meter '
                'reading of the account that rendered it (<span class="mono">readings.json</span>). The owner\'s caps '
                'are in <span class="mono">scripts/orchestrator/model_rules.json</span>.</p>')
    r = pacing["reading"]
    left = float(pacing["weekly_cap"]) - float(r["weekly_pct"])
    funds = (f' At the measured burn ({burn["per_h"]:.1f} weekly points per hour) the {left:g} points left under the cap '
             f'fund about <strong class="num">{left / burn["per_h"]:.0f} h</strong> of loop time.' if burn and left > 0 else "")
    return f"""<p>The newest meter reading of <span class="mono">{e(pacing["account"])}</span>, the Claude account that rendered this page, noted <span class="mono">{e(pacing["noted_utc"])} UTC</span>, against the caps in <span class="mono">model_rules.json</span>.{funds}</p>
  <div class="meter-row">
    <div class="meter-card">
      <div class="meter-head"><span class="t">Weekly quota used</span><span class="v num">{r["weekly_pct"]:g} % · cap {pacing["weekly_cap"]:g} %</span></div>
      <div class="meter"><div class="fill" style="width:{min(100.0, float(r["weekly_pct"])):.1f}%"></div></div>
    </div>
    <div class="meter-card">
      <div class="meter-head"><span class="t">Session window used</span><span class="v num">{r["session_pct"]:g} % · soft stop {pacing["soft"]:g} % · hard stop {pacing["hard"]:g} %</span></div>
      <div class="meter"><div class="fill" style="width:{min(100.0, float(r["session_pct"])):.1f}%"></div></div>
    </div>
  </div>"""


def render(ctx: dict) -> str:
    inv, wk, a1, cf, bat, cp = (ctx["inv"], ctx["work"], ctx["a1"], ctx["conf"], ctx["battery"], ctx["corpus"])
    head, pts, plan, pacing, rem = ctx["head"], ctx["trend"], ctx["plan"], ctx["pacing"], ctx["remaining"]
    pct = 100.0 * inv["closed"] / inv["rows"]
    kinds = " · ".join(f'{n(v)} {k}' for k, v in inv["kind"].most_common())
    top = " · ".join(i["id"] for i in wk["actionable"][:3])
    parked = ", ".join(i["id"] for i in wk["owner_parked"]) or "none"
    a4 = cf["a4"]
    a3 = cf["a3"]
    bat_gap = gap_at(str(bat.get("sha", "")))
    gap_delta = None if bat_gap is None else inv["gap"] - bat_gap
    comp = rem["compiler"]

    return TEMPLATE.format(
        title=TITLE,
        css=CSS_PATH.read_text(encoding="utf-8"),
        head_sha=e(head["sha"]), head_date=e(head["date"]),
        bat_n=e(bat["n"]), bat_date=e(bat["date"]), bat_sha=e(bat["sha"]),
        bat_since=n(bat["since"]), bat_since_code=n(bat["since_code"]),
        bat_state_class="warn" if bat["owed"] else "good",
        bat_state_value=(f'#{int(bat["n"]) + 1} owed' if bat["owed"] else f'#{bat["n"]} green'),
        bat_state_note=(f'#{bat["n"]} ALL GREEN on <span class="mono">{e(bat["sha"])}</span>; '
                        f'{n(bat["since_code"])} <span class="mono">src</span>/<span class="mono">tests</span> '
                        f'landings since, wave-local-gated only'
                        if bat["owed"] else
                        f'#{bat["n"]} ALL GREEN on <span class="mono">{e(bat["sha"])}</span>; HEAD carries no '
                        f'<span class="mono">src</span>/<span class="mono">tests</span> change since'),
        bat_legs="\n        ".join(
            f'<tr><td>{e(label)}</td><td class="r num">{e(value)}</td>'
            f'<td><span class="pill {"good" if ok else "warn"}">{"GREEN" if ok else "UNREAD"}</span></td></tr>'
            for label, value, ok in bat["legs"]),
        bat_totals=(f' Totals {e(bat["totals"])} (WE_REJECT_THEY_ACCEPT / AGREE_ACCEPT / AGREE_REJECT / '
                    f'WE_ACCEPT_THEY_REJECT).' if bat["totals"] else ""),
        rows=n(inv["rows"]), closed=n(inv["closed"]), pct=f"{pct:.1f}",
        gap=n(inv["gap"]), adjudicated=n(inv["adjudicated"]), kinds=kinds,
        f_closed=inv["closed"], f_resolved=inv["resolved_owed"], f_defective=inv["defective"],
        f_unadj=inv["unadjudicated"],
        resolved_owed=n(inv["resolved_owed"]), defective=n(inv["defective"]),
        unadjudicated=n(inv["unadjudicated"]),
        pc_resolved=f'{100.0 * inv["resolved_owed"] / inv["rows"]:.1f}',
        pc_defective=f'{100.0 * inv["defective"] / inv["rows"]:.1f}',
        pc_unadj=f'{100.0 * inv["unadjudicated"] / inv["rows"]:.1f}',
        conforms_closed=n(inv["conforms_closed"]), conforms_owed=n(inv["conforms_owed"]),
        dns_closed=n(inv["dns_closed"]), dns_owed=n(inv["dns_owed"]),
        v_partial=n(inv["verdict"]["PARTIAL"]), v_notimpl=n(inv["verdict"]["NOT-IMPLEMENTED"]),
        v_diverges=n(inv["verdict"]["DIVERGES"]),
        doc_rows=n(inv["doc_rows"]), doc_verdicted=n(inv["doc_verdicted"]), doc_closed=n(inv["doc_closed"]),
        clause_rows=clause_rows(inv["buckets"]),
        work_items=n(wk["items"]), work_open=n(wk["status"]["open"]),
        work_actionable=n(len(wk["actionable"])), work_top=e(top), work_parked=e(parked),
        comp_n=n(comp["n"]), comp_defects=n(comp["kind"]["defect"]), comp_wrong=n(comp["harm"]["wrong_answer"]),
        comp_crash=n(comp["harm"]["crashes"]), comp_no_lane=n(len(comp["no_lane"])),
        a1_discharged=n(a1["discharged"]), a1_scope=n(a1["scope"]), a1_remaining=n(a1["remaining"]),
        a3_rows=n(cf["a3_rows"]), a3_claimed=n(a3["claimed"]), a3_partial=n(a3["partial"]),
        a3_not=n(a3["not claimed"]), a3_na=n(a3["n/a"]),
        a4_rows=n(cf["a4_rows"]), a4_claimed=n(a4["claimed"]), a4_partial=n(a4["partial"]),
        a4_not=n(a4["not claimed"]),
        a4_claimed_list=e(" · ".join(cf["a4_by"]["claimed"])),
        a4_partial_list=e(" · ".join(cf["a4_by"]["partial"])),
        a4_not_list=e(" · ".join(cf["a4_by"]["not claimed"])),
        a1_state=((f'<span class="pill good">DISCHARGED</span>&ensp;<span class="dim">{n(a1["discharged"])} of '
                   f'{n(a1["scope"])} obligations in scope' if not a1["remaining"] else
                   f'<span class="pill warn">{n(a1["remaining"])} REMAIN</span>&ensp;<span class="dim">of '
                   f'{n(a1["scope"])} in scope')
                  + (f' ({n(len(a1["unreachable"]))} withdrawn with their declined module); '
                     f'<span class="mono">audit_annex_a1.py</span> reports '
                     + ("no findings" if not a1["findings"] else
                        f'</span><span class="pill crit">{n(len(a1["findings"]))} FINDING(S)</span><span class="dim">')
                     + '</span>')),
        facilities=e(" · ".join(cf["facilities"])), facilities_n=n(len(cf["facilities"])),
        corpus_pos=n(cp["positive"]), corpus_neg=n(cp["negative"]),
        corpus_editions=e(" · ".join(f"{n(v)} ({k})" for k, v in cp["editions"].items())),
        constructs=n(cp["constructs"]), matrix_cells=n(cp["constructs"] * 4),
        vcr_todo=n(cp["vcr_todo"]), vcr_done=n(cp["vcr_done"]), vcr_total=n(cp["vcr_total"]),
        vcr_pct=f'{100.0 * cp["vcr_done"] / cp["vcr_total"]:.1f}',
        vcr_gated=n(cp["vcr_gated"]), vcr_ref=n(cp["vcr_ref"]), vcr_pin=n(cp["vcr_pin"]),
        trend_svg=trend_svg(pts, bat),
        trend_from=n(pts[0]["gap"]) if pts else "—", trend_to=n(pts[-1]["gap"]) if pts else "—",
        trend_points=n(len(pts)),
        register_svg=register_svg(pts), register_legend=register_legend(pts),
        register_points=n(sum(1 for p in pts if "compiler_open" in p)),
        gap_delta_word=("not recorded" if gap_delta is None else "unchanged" if gap_delta == 0 else f"{gap_delta:+d}"),
        remaining=render_remaining(rem, ctx["estimate"], cp, ctx["inputs"]),
        lane_external=render_external(plan),
        pacing=render_pacing(pacing, ctx["inputs"].get("burn")),
    )


# ─────────────────────────────────────────────────────────────────────────────────────────────────────────────
# the page — structure and palette are the published artifact's; only the slots are generated
# ─────────────────────────────────────────────────────────────────────────────────────────────────────────────

#: The stylesheet lives beside the script as DATA rather than inside this file: it is 200 lines of tokens and
#: component rules that no Python ever reads, and burying it in a string literal is how a template turns into a
#: file nobody edits. Both themes are defined there — light on bare `:root`, dark under the guarded
#: `prefers-color-scheme` block and again under `:root[data-theme="dark"]` — per the artifact conventions.
CSS_PATH = REPO / "scripts" / "spec" / "data" / "ledger.css"


TEMPLATE = """<title>{title}</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=IBM+Plex+Serif:wght@600&family=IBM+Plex+Sans:wght@400;500;600&family=IBM+Plex+Mono:wght@400;500&display=swap">
<style>
{css}</style>

<div class="wrap">

<header>
  <p class="eyebrow">What remains to v1.0 · kb/Work PB2911 · ISO/IEC 1989:2023 · Four editions</p>
  <h1>{title}</h1>
  <p class="asof">As of <b>{head_date}</b> · measured tree <b class="mono">{head_sha}</b> · last comprehensive battery <b>#{bat_n}</b> on <b class="mono">{bat_sha}</b> — <b>{bat_since} commits back</b>, {bat_since_code} of them touching <span class="mono">src/</span> or <span class="mono">tests/</span></p>
  <p class="mission">v1.0 is <strong>a conforming implementation of the whole of ISO/IEC 1989 in each of its four editions</strong> (85 / 2002 / 2014 / 2023), as §4.2.1 defines conformance, measured by one instrument: every normative rule as a row of the traceability inventory, at <strong>zero GAP</strong>, with P15 Cut 3 and the §4.2.16 user documentation. The owner put the compiler and zero GAP first (2026-10-10, kb/Work PB2911): the known compiler defects and the notes that hold GAP rows run in the loop; the Delete program and the architecture review wait until both reach zero. This page measures what remains from the register, the inventory and the loop's own measurements.</p>
</header>

<section aria-label="Headline">
  <div class="meter-row">
    <div class="meter-card">
      <div class="meter-head"><span class="t">Conformance: rules closed</span><span class="v num">{closed} of {rows} · GAP {gap}</span></div>
      <div class="meter"><div class="fill" style="width:{pct}%"></div></div>
      <p class="note">v1.0's conformance half is GAP at zero.</p>
    </div>
    <div class="meter-card">
      <div class="meter-head"><span class="t">Known compiler defects and analyses</span><span class="v num">{comp_n} open · {comp_defects} defects</span></div>
      <p class="note">{comp_wrong} wrong answers · {comp_crash} crashes among them; {comp_no_lane} planned by no lane. Zero, with zero GAP, releases the held work (PB2911).</p>
    </div>
  </div>
</section>

<section aria-label="What remains">
  <h2>What remains to v1.0</h2>
  {remaining}
</section>

<section aria-label="Conformance">
  <h2>Conformance to zero GAP</h2>
  <div class="hero">
    <div class="hero-figure">
      <span class="big num">{closed}</span>
      <span class="of">of {rows} rules closed</span>
      <span class="pct">{pct}%</span>
    </div>
    <p class="hero-sub">One row per normative rule of the standard ({kinds}). A row closes only when its verdict resolves — CONFORMS, or a documented-non-support determination — <em>and</em> a spec-derived witness covers it. Adjudicating a rule <strong>opens</strong> work; only a witness closes it.</p>
    <div class="stackbar" role="img" aria-label="Inventory composition: {closed} closed, {resolved_owed} resolved awaiting a witness, {defective} known-defective, {unadjudicated} not yet adjudicated">
      <div class="seg closed"   style="flex:{f_closed}"  data-tip="Closed — {closed} rules ({pct}%)"></div>
      <div class="seg testowed" style="flex:{f_resolved}"  data-tip="Resolved, witness owed — {resolved_owed} ({pc_resolved}%)"></div>
      <div class="seg open"     style="flex:{f_defective}"  data-tip="Known-defective — {defective} ({pc_defective}%)"></div>
      <div class="seg track"    style="flex:{f_unadj}" data-tip="Not yet adjudicated — {unadjudicated} ({pc_unadj}%)"></div>
    </div>
    <div class="legend">
      <span class="key"><span class="sw" style="background:var(--s-closed)"></span>Closed <span class="num">{closed}</span></span>
      <span class="key"><span class="sw" style="background:var(--s-testowed)"></span>Resolved, witness owed <span class="num">{resolved_owed}</span></span>
      <span class="key"><span class="sw" style="background:var(--s-open)"></span>Known-defective <span class="num">{defective}</span></span>
      <span class="key"><span class="sw" style="background:var(--s-track)"></span>Not yet adjudicated <span class="num">{unadjudicated}</span></span>
    </div>
  </div>

  <div class="tiles">
    <div class="tile">
      <div class="label">Battery status</div>
      <div class="value {bat_state_class}">{bat_state_value}</div>
      <div class="note">{bat_state_note}</div>
    </div>
    <div class="tile">
      <div class="label">Known-defective rules</div>
      <div class="value num">{defective}</div>
      <div class="note">every row claimed by a live work note, held there by <span class="mono">DefectiveRowCoverageDriftTests</span></div>
    </div>
    <div class="tile">
      <div class="label">Open register items</div>
      <div class="value num">{work_open}</div>
      <div class="note">of {work_items} notes; {work_actionable} actionable, led by {work_top}; owner-parked: {work_parked}</div>
    </div>
    <div class="tile">
      <div class="label">GAP</div>
      <div class="value num">{gap}</div>
      <div class="note">{gap_delta_word} since battery #{bat_n}; v1.0 is this number at zero</div>
    </div>
  </div>

  <div class="trend">
    <div class="trend-head">
      <span class="t">GAP burn-down, per recorded point</span>
      <span class="v num">{trend_from} → {trend_to} · {trend_points} points</span>
    </div>
    {trend_svg}
    <p class="note">Each point is a landing that moved the inventory or the register's series, measured from that commit's own tree. A flat GAP step is real and worth reading: correcting an already-CONFORMS row moves nothing, and a register-only move adds a point with GAP unchanged. Series: <span class="mono">docs/rearchitecture/evidence/ledger-trend.json</span>, appended by <span class="mono">gen_ledger.py</span>.</p>
  </div>
  <div class="trend">
    <div class="trend-head">
      <span class="t">Known compiler notes, beside the GAP line</span>
      <span class="v num">{register_points} points</span>
    </div>
    {register_svg}
    <div class="legend">{register_legend}</div>
    <p class="note">The register's series on the same points (kb/Work PB2912): open notes that are not <span class="mono">process_only</span>, of them <span class="mono">kind: defect</span>, the wrong answers and crashes among them, and the open notes holding a GAP row. Each point was measured from its own commit's <span class="mono">kb/Work</span>; a point whose tree had no frontmatter register carries none.</p>
  </div>
</section>

<section aria-label="External repository">
  <h2>The external repository</h2>
  {lane_external}
</section>

<section aria-label="Gates">
  <h2>Conformance detail · what is proven green</h2>
  <p><strong>Battery #{bat_n} ({bat_date}, tree <span class="mono">{bat_sha}</span>) is the last comprehensive run.</strong> The tree above is {bat_since} commits past it, and <strong>{bat_since_code}</strong> of those touch <span class="mono">src/</span> or <span class="mono">tests/</span> — which is what decides whether another battery is owed, rather than a judgement about how big the changes felt. Everything since was gated <em>wave-locally</em> only.{bat_totals}</p>
  <div class="tablecard">
    <table>
      <thead><tr><th>Leg</th><th class="r">Result at #{bat_n}</th><th>Status</th></tr></thead>
      <tbody>
        {bat_legs}
        <tr><td>Version matrix</td><td class="r num">{constructs} × 4 = {matrix_cells} cells</td><td><span class="pill good">GREEN</span></td></tr>
      </tbody>
      <tfoot><tr><td>Corpus, measured tree</td><td class="r num">{corpus_pos} + {corpus_neg}</td><td>positive goldens · negative fixtures — {corpus_editions}</td></tr></tfoot>
    </table>
  </div>
</section>

<section aria-label="Verdicts">
  <h2>Conformance detail · how the {rows} rows stand</h2>
  <div class="tablecard">
    <table>
      <thead><tr><th>Verdict</th><th class="r">Rows</th><th>Can it close a row?</th><th>Standing</th></tr></thead>
      <tbody>
        <tr><td>CONFORMS — closed</td><td class="r num">{conforms_closed}</td><td>Closed</td><td class="dim">spec-derived witness in place</td></tr>
        <tr><td>DOCUMENTED-NON-SUPPORT — closed</td><td class="r num">{dns_closed}</td><td>Closed</td><td class="dim">D13's licence: a recorded determination with its witness</td></tr>
        <tr><td>CONFORMS — witness owed</td><td class="r num">{conforms_owed}</td><td>Yes, once a golden lands</td><td class="dim">no compiler change stands between these and closed</td></tr>
        <tr><td>DOCUMENTED-NON-SUPPORT — witness owed</td><td class="r num">{dns_owed}</td><td>Yes, once witnessed</td><td class="dim">stamped by the A.4 derived-verdict selectors, one per <em>Not claimed</em> module</td></tr>
        <tr><td>PARTIAL</td><td class="r num">{v_partial}</td><td>No — needs a fix</td><td><span class="pill warn">OPEN</span></td></tr>
        <tr><td>NOT-IMPLEMENTED</td><td class="r num">{v_notimpl}</td><td>No — needs a fix</td><td><span class="pill warn">OPEN</span></td></tr>
        <tr><td>DIVERGES</td><td class="r num">{v_diverges}</td><td>No — needs a fix</td><td><span class="pill crit">OPEN</span></td></tr>
        <tr><td>Not yet adjudicated</td><td class="r num">{unadjudicated}</td><td>—</td><td class="dim">unaudited territory</td></tr>
      </tbody>
      <tfoot><tr><td>Total</td><td class="r num">{rows}</td><td colspan="2">{closed} OK · {gap} GAP · {adjudicated} adjudicated · of {doc_rows} Annex A.1 DOC rows, {doc_verdicted} carry a verdict and {doc_closed} have closed</td></tr></tfoot>
    </table>
  </div>
</section>

<section aria-label="Burn-down by clause">
  <h2>Conformance detail · burn-down by clause</h2>
  <p>Bar length is the clause's share of the largest clause; fill shows its state — closed, adjudicated-but-open, never looked at.</p>
  <div class="tablecard">
    <table>
      <thead><tr><th>Clause</th><th class="r">Rules</th><th class="r">Adjudicated</th><th class="r">GAP</th><th class="cbar-track">Closed · adjudicated-open · unadjudicated</th></tr></thead>
      <tbody>
        {clause_rows}
      </tbody>
      <tfoot><tr><td>Total</td><td class="r num">{rows}</td><td class="r num">{adjudicated}</td><td class="r num">{gap}</td><td></td></tr></tfoot>
    </table>
  </div>
</section>

<section aria-label="Documentation posture">
  <h2>Conformance detail · the §4.2.16 documentation posture</h2>
  <div class="tablecard">
    <table>
      <thead><tr><th>Register</th><th>Standing</th></tr></thead>
      <tbody>
        <tr><td>Annex A.3 — processor-dependent ({a3_rows} rows)</td><td class="dim"><span class="num">{a3_claimed}</span> claimed · <span class="num">{a3_partial}</span> partial · <span class="num">{a3_not}</span> not claimed · <span class="num">{a3_na}</span> n/a</td></tr>
        <tr><td>Annex A.4 — optional modules ({a4_rows})</td><td class="dim"><span class="num">{a4_claimed}</span> claimed — {a4_claimed_list} · <span class="num">{a4_partial}</span> partial — {a4_partial_list} · <span class="num">{a4_not}</span> not claimed — {a4_not_list}</td></tr>
        <tr><td>Documented non-support ({facilities_n} facilities)</td><td class="dim">{facilities} — each with a named compile-time warning, exactly the posture §4.2.6 ¶3 requires and D13 permits</td></tr>
        <tr><td>Annex A.1 — implementor-defined register</td><td>{a1_state}</td></tr>
        <tr><td>Version-change ledger anchors</td><td class="dim"><span class="num">{vcr_done}</span> of {vcr_total} dispositioned ({vcr_pct}%): {vcr_gated} gated · {vcr_ref} ref-only · {vcr_pin} pinned-to-spec · <span class="num">{vcr_todo}</span> todo; the matrix registry stands at {constructs} constructs</td></tr>
      </tbody>
    </table>
  </div>
</section>

<section aria-label="Pacing">
  <h2>Pacing</h2>
  {pacing}
</section>


<footer>
  <p><strong>Every number on this page is computed — none is remembered, none is hand-written.</strong> Inventory from <span class="mono">tests/version-matrix/traceability-inventory.json</span>, work standing from <span class="mono">kb/Work</span> through <span class="mono">work.py</span>'s own predicates, what remains and its lanes through <span class="mono">ledger_plan.py</span> (the fix lane's population from <span class="mono">.agent-fleet.json</span>, the campaign and held clusters from the owner's decision PB2911), the estimate from <span class="mono">fix_clusters.py</span>, <span class="mono">model_rules.json</span>, <span class="mono">train_measure.py</span> and the orchestrator's meter readings, gates from plan §0's battery reference, documentation posture from <span class="mono">docs/CONFORMANCE.md</span> §2/§4/§5 and <span class="mono">audit_annex_a1.py --json</span>, the corpus and anchor counts from the trees themselves, the trend series from <span class="mono">docs/rearchitecture/evidence/ledger-trend.json</span>. Rendered by <span class="mono">scripts/spec/gen_ledger.py</span>. The work register remains <span class="mono">kb/Work/</span> — this ledger is a derived view, never a tracker.</p>
  <p class="mono">Snapshot: {head_date} · measured tree {head_sha} — the last commit that touched an input this page reports, so an unrelated commit cannot make a correct page look stale · {rows} rows · {closed} closed · {gap} GAP · {adjudicated} adjudicated · battery #{bat_n} on {bat_sha} · A.1 {a1_discharged}/{a1_scope} · register {work_items} notes, {work_open} open, {work_actionable} actionable · {comp_n} known compiler notes.</p>
</footer>

</div>
"""


# ─────────────────────────────────────────────────────────────────────────────────────────────────────────────

def build() -> tuple[str, dict]:
    """The page, and the trend file's series with whether it gained its current point."""
    import work  # noqa: PLC0415
    inv = measure_inventory()
    head = measure_head()
    bat = measure_battery(head["sha"])
    items, rows = work.load(), ledger_plan.load_inventory()
    rem = ledger_plan.remaining(items, rows, inv["gap"], ledger_plan.fleet_population())
    pts, is_new = trend_series(inv, head, bat, rem["counts"])
    plan = ledger_plan.measure(items, inv["gap"])
    inputs = ledger_plan.estimate_inputs()
    ctx = {"inv": inv, "work": measure_work(), "a1": measure_annex_a1(), "conf": measure_conformance(),
           "battery": bat, "corpus": measure_corpus(), "head": head, "trend": pts, "plan": plan,
           "remaining": rem, "estimate": ledger_plan.bucket_estimates(rem, inputs), "inputs": inputs,
           "pacing": ledger_plan.measure_pacing()}
    return render(ctx), {"points": pts, "is_new": is_new}


def self_test() -> int:
    """The page's new parts on planted inputs (kb/Work PB2912): the loud flags, UNMEASURED for a missing input, the
    trend append carrying the register's series, and the register read from a commit's own tree."""
    global REPO, TREND  # noqa: PLW0603  (pointed at temporary fixtures, and restored)
    import tempfile  # noqa: PLC0415
    results: list[tuple[str, bool, str]] = []

    def check(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, ok, detail))

    def note(nid, kind="analysis", status="open", cluster=(), rows=(), **flags):
        return {"id": nid, "title": f"{nid} — x", "kind": kind, "status": status, "cluster": list(cluster),
                "blocked_by": [], "inventory_rows": list(rows), **flags}

    leads = [note(lead, "decision", process_only=True) for lead, _ in (*ledger_plan.CAMPAIGNS, *ledger_plan.HELD)]
    items = [*leads, note("PB9001", "defect", cluster=[ledger_plan.GAP_CAMPAIGN], rows=["R-1"], wrong_answer=True),
             note("PB9003", silent=True), note("PB9004", process_only=True, rows=["R-2"])]
    rows = [{"rule-id": "R-1", "state": "GAP", "verdict": "PARTIAL"}, {"rule-id": "R-2", "state": "GAP", "verdict": "PARTIAL"}]
    rem = ledger_plan.remaining(items, rows, 2, ("defect", ("process_only",)))
    cp = {"vcr_todo": 1, "vcr_total": 2}
    none = {"rules": None, "fix_groups": None, "rate": None, "burn": None}
    page = render_remaining(rem, ledger_plan.bucket_estimates(rem, none), cp, none)
    flags = re.findall(r'<p class="alert crit">(.*?)</p>', page, re.S)
    check("a GAP holder outside the GAP campaign is flagged loudly",
          any("outside the GAP campaign" in f and "PB9004" in f for f in flags), str(flags)[:300])
    check("an open compiler note no lane plans is flagged loudly",
          any("no lane plans" in f and "PB9003" in f and "PB9001" not in f for f in flags), str(flags)[:300])
    est = page[page.index("Estimate, from measured inputs"):]
    cells = re.findall(r'<td class="r">(.*?)</td>', est)
    check("a missing input renders UNMEASURED, never a number",
          bool(cells) and all(c == UNMEASURED for c in cells), str(cells)[:300])
    rules = {"wave": {"max_notes_per_group": 5, "max_groups": 8, "train_size": 5, "implementer_model": "sonnet"},
             "cost": {"per_group_tokens": {"sonnet": 400000}, "per_note_tokens": {"sonnet": 240000},
                      "lander_tokens": 480000, "lander_model": "opus"},
             "calibration": {"tokens_per_point": {"sonnet": 4000000, "opus": 1600000}}}
    full = {"rules": rules, "fix_groups": {"clusters": [{"notes": [{"id": "PB9001"}]}], "unsited": []},
            "rate": {"per_h": 2.0, "landed": 4, "window_h": 2.0, "trains": 1, "gating": "batched"},
            "burn": {"per_h": 3.0, "from_pct": 0.0, "to_pct": 3.0, "hours": 1.0, "account": "t"}}
    page = render_remaining(rem, ledger_plan.bucket_estimates(rem, full), cp, full)
    first = re.search(r"Known compiler defects and analyses</td>(.*?)</tr>", page[page.index("Estimate, from"):], re.S)
    check("measured inputs render numbers: 1 group, 1 wave, 0.5 h, 1.5 points at the burn",
          bool(first) and all(f'<span class="num">{v}</span>' in first.group(1) for v in ("1", "0.5 h", "1.5")),
          first.group(1)[:300] if first else "no row")

    saved = (REPO, TREND)
    tmp = pathlib.Path(tempfile.mkdtemp(prefix="gen-ledger-test-"))
    try:
        TREND = tmp / "trend.json"
        TREND.write_text(json.dumps({"points": [{"sha": "0000000a", "date": "2026-10-09", "gap": 2, "closed": 1,
                                                 "dns": 0, "label": "x", "battery": None}]}), encoding="utf-8")
        inv = {"gap": 2, "closed": 1, "verdict": collections.Counter()}
        bat = {"n": "1", "sha": "ffffffff"}
        reg = ledger_plan.register_counts(items, {"R-1", "R-2"})
        pts, new = trend_series(inv, {"sha": "0000000b", "date": "2026-10-10"}, bat, reg)
        if new:
            write_trend(pts)
        on_disk = json.loads(TREND.read_text(encoding="utf-8"))["points"][-1]
        check("the trend append writes the register's series",
              new and all(on_disk.get(k) == reg[k] for k in ledger_plan.REGISTER_KEYS), str(on_disk))
        moved = {**reg, "wrong_answer": reg["wrong_answer"] + 1}
        pts2, new2 = trend_series(inv, {"sha": "0000000c", "date": "2026-10-10"}, bat, moved)
        _, new3 = trend_series(inv, {"sha": "0000000d", "date": "2026-10-10"}, bat, reg)
        check("a register-only move records a point; an unmoved register does not",
              new2 and pts2[-1]["wrong_answer"] == moved["wrong_answer"] and not new3, f"{new2} {new3}")

        REPO = tmp / "repo"
        (REPO / "kb" / "Work").mkdir(parents=True)
        (REPO / INVENTORY_REL).parent.mkdir(parents=True)
        (REPO / INVENTORY_REL).write_text(json.dumps(rows), encoding="utf-8")
        fm = ("---\ntitle: \"{i} — x\"\nid: {i}\nkind: defect\nstatus: {s}\nwrong_answer: true\ncrashes: false\n"
              "process_only: false\ncluster: []\nblocked_by: []\ninventory_rows: [\"R-1\"]\n---\n\nbody\n")
        (REPO / "kb" / "Work" / "PB1.md").write_text(fm.format(i="PB1", s="open"), encoding="utf-8")
        (REPO / "kb" / "Work" / "PB2.md").write_text(fm.format(i="PB2", s="landed"), encoding="utf-8")
        (REPO / "kb" / "Work" / "README.md").write_text("no frontmatter\n", encoding="utf-8")
        for args in (("init", "-q", "-b", "main"), ("add", "."), ("commit", "-q", "-m", "fixture")):
            subprocess.run(["git", "-c", "user.email=t@t", "-c", "user.name=t", *args], cwd=REPO, check=True,
                           capture_output=True)
        got = register_counts_at("HEAD")
        want = {"compiler_open": 1, "defects": 1, "wrong_answer": 1, "crashes": 0, "gap_holders": 1}
        check("the register is read from a commit's own tree (git cat-file --batch)", got == want, f"{got}")
    finally:
        REPO, TREND = saved
        import shutil  # noqa: PLC0415
        shutil.rmtree(tmp, ignore_errors=True)

    for name, ok, detail in results:
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + ("" if ok else f"\n        {detail}"))
    bad = sum(1 for _, ok, _ in results if not ok)
    print(f"=== gen_ledger SELF-TEST: {'PASS' if not bad else 'FAIL'} ({len(results)} checks) ===")
    return 1 if bad else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--out", type=pathlib.Path, default=DEFAULT_OUT,
                    help=f"HTML to write (default {DEFAULT_OUT})")
    ap.add_argument("--check", action="store_true",
                    help="do not write; exit non-zero if --out differs from what the repo would render now, or if "
                         "the plan does not read cleanly off the register (ledger_plan.problems)")
    ap.add_argument("--self-test", action="store_true",
                    help="the loud flags, UNMEASURED, the trend append and the register at a commit, on planted inputs")
    ap.add_argument("--backfill-register", action="store_true",
                    help="add the register's series to the trend points recorded before it existed, each measured "
                         "from its own sha's tree, write the trend file and stop (kb/Work PB2912)")
    a = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    if a.self_test:
        return self_test()
    if a.backfill_register:
        doc = json.loads(TREND.read_text(encoding="utf-8"))
        filled = backfill_register(doc["points"])
        write_trend(doc["points"])
        print(f"trend  : the register's series added to {filled} of {len(doc['points'])} points "
              f"({TREND.relative_to(REPO)})")
        return 0

    bad = ledger_plan.live_problems()
    if bad:
        print("⛔ THE PLAN CANNOT BE TRUSTED (scripts/spec/ledger_plan.py --self-test):")
        for b in bad:
            print(f"   {b}")
        return 1

    text, tr = build()
    pts, is_new = tr["points"], tr["is_new"]

    if a.check:
        if not a.out.exists():
            # ⛔ ABSENT IS NOT STALE — the gen_conformance_notes.py contract, and for the same reason: the
            # rendered page is a gitignored build output, so on a fresh clone and in CI it legitimately does
            # not exist. Reporting that as a failure would be a manufactured red
            # (feedback_verdict_evidence_invariant).
            print(f"· {a.out} does not exist (gitignored build output) — nothing to check.")
            return 0
        page_ok = a.out.read_text(encoding="utf-8") == text
        if page_ok and not is_new:
            print(f"✓ {a.out.name} matches the repo exactly ({len(text):,} bytes), and the trend file holds its points")
            return 0
        print("⛔ THE CONFORMANCE LEDGER IS STALE — " + ("it describes a tree the repo has moved past." if not page_ok
              else f"the page matches, but {TREND.relative_to(REPO)} lacks the point(s) the page draws."))
        if is_new:
            print(f"      the trend series is missing the current point "
                  f"(GAP {pts[-1]['gap']}, {pts[-1].get('compiler_open')} compiler notes at {pts[-1]['sha']})")
        print("   Run: python scripts/spec/gen_ledger.py    then publish --out to the artifact's URL")
        return 1

    if is_new:
        write_trend(pts)
        p = pts[-1]
        print(f"trend  : appended {p['sha']} — GAP {p['gap']} · closed {p['closed']} · DNS {p['dns']} · "
              f"{p.get('compiler_open')} compiler notes ({p.get('defects')} defects, {p.get('wrong_answer')} wrong "
              f"answers, {p.get('crashes')} crashes) · {p.get('gap_holders')} GAP holders   "
              f"({TREND.relative_to(REPO)}, {len(pts)} points)")
    else:
        print(f"trend  : unchanged — {len(pts)} points, last {pts[-1]['sha'] if pts else '—'}")
    a.out.parent.mkdir(parents=True, exist_ok=True)
    a.out.write_text(text, encoding="utf-8")
    print(f"wrote  : {a.out} ({len(text):,} bytes)")
    import account  # noqa: PLC0415
    import ledger_state  # noqa: PLC0415  (imports this module for STAMP_PATHS; loaded only here, after a render)
    print(ledger_state.publish_hint(COORD_DIR, account.current().name))
    return 0


if __name__ == "__main__":
    sys.exit(main())
