#!/usr/bin/env python3
"""Render the owner's **WiseOwl COBOL Completion Ledger** artifact from the repo — every figure COMPUTED.

⭐ WHAT THE PAGE IS (kb/Work PB2462, owner 2026-10-07: "Rewrite the ledger as necessary to clearly and accurately
track all aspects of the new plan and progress towards completing it"). The completion plan of kb/Work R69, lane by
lane: a headline with the two meters that matter (rules closed with GAP; re-architecture program notes landed), the
six lanes as cards with each one's definition of done and measured progress, then one section per lane — 1 legacy
retirement, 2 conformance to zero GAP (the original conformance ledger's hero, tiles and GAP trend), 3 the external
repository's slices, 4 the architecture review R0-R5 with the review program by wave kind x status and what its
notes wait on, 5 the gates between the lanes — then the conformance detail (gates proven green, verdicts, burn-down
by clause, the three veins, the §4.2.16 posture), the in-flight narrative, and 6 pacing. The lanes are MEASURED in
`ledger_plan.py` (structures, register reads, its own `--self-test`) and RENDERED here.

⛔ WHY THIS EXISTS. The ledger is the owner's live status page, and it was maintained by HAND: a session read the
inventory, the work register, the A.1 audit and plan §0, then retyped ~60 numbers into a 460-line HTML file. Two
failure modes follow from that shape and both had already happened elsewhere in this repo:

  * **A hand-copied number is a remembered number.** `gen_conformance_notes.py`'s docstring records the same
    lesson from the vault view — §15 claimed 533 GAP where the live number was 481, and the stale view was still
    read as current. A status page nobody can re-derive is worse than none, because it is believed.
  * **A hand-maintained page grows into a work list.** CLAUDE.md rule 8 permits exactly one work register
    (`kb/Work/`). This page is a DERIVED VIEW of that register and of the inventory — never a tracker — and the
    only way to keep it that way is to make it impossible to author anything here that is not read off a
    measured artifact. Everything on the rendered page except the in-flight narrative is computed by this file.

So a refresh is now: run this script, then publish `--out` to the artifact's existing URL. No retyping.

    python scripts/spec/gen_ledger.py                       # write the default --out
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
    kb/Work, through ledger_plan.py                    the plan's lanes: clusters PB2108 and PB2119 (work.py's
                                                       `cluster_members`), phase, slice and gate notes found by
                                                       title or by ledger_plan's small self-tested structures, wave
                                                       kinds by file_census_notes.py's title prefixes (imported)
    DESIGN-architecture-review.md §8                   R1's draft status line, when the design carries one
    docs/rearchitecture/evidence/arch-census ·         R0's records: the newest file in each, by commit date
      arch-oracle · perf-baseline
    <coord>/readings.json + model_rules.json          pacing: the newest meter reading against the owner's caps,
                                                       or "not available" where there is no coordination directory
                                                       (so a page rendered in CI says so; NOT a stamp input — a new
                                                       reading makes --check report the page stale, which it is)
    git                                                HEAD sha + date, and how far HEAD has drifted from the
                                                       battery's tree (which is what makes "a battery is owed"
                                                       a measurement rather than an opinion)

⭐ THE TREND IS DATA, NOT MEMORY — `docs/rearchitecture/evidence/ledger-trend.json`. A burn-down chart is the one
thing on the page that cannot be recomputed from the current tree: yesterday's GAP is gone. So the series lives in
a small committed file, one point per battery or GAP-moving landing (`sha`, `date`, `gap`, `closed`, `dns`,
`label`, `battery`), and a WRITE run appends the current point when the measurement has moved. The same file
carries a second series, `program` (`sha`, `date`, `open`, `landed`, `total` program notes), appended when those
counts move: the program is behavior-neutral, so its progress never moves GAP and cannot ride on a GAP point. `--check` never
writes — it renders against the series plus the current point exactly as a write would, so a moved measurement
whose point has not been recorded shows up as staleness, which is the correct verdict.

⭐ THE ONE HAND-WRITTEN PART is `--in-flight` (default
`docs/rearchitecture/evidence/ledger-in-flight.md`), inserted VERBATIM as the body of the "In flight right now"
section. It is narrative — which lanes are running, which landings are queued, which owner questions are open —
and narrative is the only thing on this page a script has no business inventing. It carries no counts that this
script can compute; if a number in it can be measured, it belongs in the generator instead.

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
import ledger_plan  # noqa: E402  (the plan's lanes: measured there, rendered here)

INVENTORY = REPO / "tests" / "version-matrix" / "traceability-inventory.json"
CONSTRUCTS = REPO / "tests" / "version-matrix" / "constructs.json"
CONFORMANCE = REPO / "docs" / "CONFORMANCE.md"
PLAN = REPO / "docs" / "COBOLNET_REARCHITECTURE_PLAN.md"
VCR = REPO / "docs" / "VERSION_CHANGE_REFERENCE.md"
CORPUS = REPO / "tests" / "conformance"
TREND = REPO / "docs" / "rearchitecture" / "evidence" / "ledger-trend.json"
IN_FLIGHT = REPO / "docs" / "rearchitecture" / "evidence" / "ledger-in-flight.md"
DEFAULT_OUT = REPO / "docs" / "rearchitecture" / "evidence" / "conformance-ledger.html"

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
#: from. Deliberately absent from this set: the rendered page, the trend series and the in-flight narrative —
#: including any of them would make regenerating move the stamp, so `--check` would never converge.
STAMP_PATHS = (
    "tests/version-matrix/traceability-inventory.json",
    "tests/version-matrix/inventory-schema.json",
    "tests/version-matrix/constructs.json",
    "tests/conformance",
    "kb/Work",
    "docs/CONFORMANCE.md",
    "docs/COBOLNET_REARCHITECTURE_PLAN.md",
    "docs/VERSION_CHANGE_REFERENCE.md",
    "docs/rearchitecture/evidence/arch-census",
    "docs/rearchitecture/evidence/arch-oracle",
    "docs/rearchitecture/evidence/perf-baseline",
    "docs/rearchitecture/DESIGN-architecture-review.md",
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

def inventory_counts_at(sha: str) -> dict | None:
    """GAP, closed and documented-non-support counts of the inventory AS COMMITTED at `sha`."""
    r = subprocess.run(["git", "show", f"{sha}:{INVENTORY.relative_to(REPO).as_posix()}"], cwd=REPO,
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        return None
    rows = json.loads(r.stdout)
    return {"gap": sum(1 for x in rows if x["state"] == "GAP"), "closed": sum(1 for x in rows if x["state"] == "OK"),
            "dns": sum(1 for x in rows if x["verdict"] == "DOCUMENTED-NON-SUPPORT")}


def missed_points(pts: list[dict]) -> list[dict]:
    """The inventory-moving commits since the series' last point that no generator run recorded.

    ⛔ A generator run measures ONE tree, so a session that landed three trains between runs recorded one point
    for three landings (2026-10-04: trains 1011 and 1012 were missing until the owner noticed the page stale).
    Every commit that touched the inventory file after the last recorded sha is measured from its own tree here,
    so the series is one point per GAP-moving landing no matter how rarely the generator runs."""
    last = pts[-1] if pts else None
    if last is None or not git("rev-parse", "--verify", "--quiet", last["sha"]):
        return []
    log = git("log", "--reverse", "--format=%h|%cI", "--abbrev=8", f"{last['sha']}..HEAD", "--",
              INVENTORY.relative_to(REPO).as_posix())
    out: list[dict] = []
    for line in filter(None, log.splitlines()):
        sha, _, iso = line.partition("|")
        counts = inventory_counts_at(sha)
        prev = out[-1] if out else last
        if counts is None or all(counts[k] == prev.get(k) for k in ("gap", "closed", "dns")):
            continue
        out.append({"sha": sha, "date": iso[:10], **counts,
                    "label": git("log", "-1", "--format=%s", sha)[:28].rstrip(), "battery": None})
    return out


def trend_series(inv: dict, head: dict, battery: dict) -> tuple[list[dict], bool]:
    """Return (series to render, whether the current point is new).

    A point is appended only when the MEASUREMENT moved — a commit that changes no row adds no point, or the
    series would become a commit log with a y-axis. `--check` calls this too and simply does not write, so a
    moved-but-unrecorded measurement renders differently from the file on disk and reports as stale."""
    doc = json.loads(TREND.read_text(encoding="utf-8")) if TREND.exists() else {"points": []}
    pts = doc.get("points", [])
    missed = missed_points(pts)
    pts = [*pts, *missed]
    cur = {"sha": head["sha"], "date": head["date"], "gap": inv["gap"], "closed": inv["closed"],
           "dns": inv["verdict"]["DOCUMENTED-NON-SUPPORT"],
           "label": git("log", "-1", "--format=%s", head["sha"])[:28].rstrip(),
           "battery": int(battery["n"]) if head["sha"].startswith(battery["sha"][:8]) else None}
    last = pts[-1] if pts else None
    moved = last is None or any(cur[k] != last.get(k) for k in ("gap", "closed", "dns"))
    # ⛔ A BATTERY ALWAYS GETS A POINT, even one that moved nothing. The chart's second job is saying which
    # point the last comprehensive gate was run on, and a battery that lands on an unchanged inventory (which
    # is the normal case — a test run closes no rows) would otherwise never appear, leaving the mark on some
    # earlier commit. That is precisely the "ungated number read as gated" failure the mark exists to prevent.
    is_battery = cur["battery"] is not None and not any(p.get("battery") == cur["battery"] for p in pts)
    if not (moved or is_battery) or (last is not None and last.get("sha") == cur["sha"]):
        return pts, bool(missed)
    return [*pts, cur], True


def program_series(prog: dict, head: dict) -> tuple[list[dict], bool]:
    """The program's counts over time — the trend file's `program` series, beside the GAP `points`.

    ⛔ A SEPARATE SERIES, NOT THREE MORE KEYS ON A GAP POINT. A GAP point is appended only when the inventory moves,
    and the program is behavior-neutral by contract (DESIGN-architecture-review.md §4): its waves close no row, so
    its counts riding on GAP points would almost never be recorded, and adding them to the GAP point's trigger would
    fill the burn-down with flat steps the chart's note says are inventory landings. A point is appended when the
    counts move, exactly as `trend_series` does for GAP, and `--check` renders without writing."""
    doc = json.loads(TREND.read_text(encoding="utf-8")) if TREND.exists() else {}
    pts = doc.get("program", [])
    cur = {"sha": head["sha"], "date": head["date"], **prog["counts"]}
    last = pts[-1] if pts else None
    if last is not None and all(cur[k] == last.get(k) for k in prog["counts"]):
        return pts, False
    if last is not None and last.get("sha") == cur["sha"]:
        return [*pts[:-1], cur], True
    return [*pts, cur], True


def write_trend(series: list[dict], program: list[dict]) -> None:
    doc = json.loads(TREND.read_text(encoding="utf-8")) if TREND.exists() else {}
    doc["points"] = series
    doc["program"] = program
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
    if not sha:
        return None
    r = subprocess.run(["git", "show", f"{sha}:{INVENTORY.relative_to(REPO).as_posix()}"], cwd=REPO,
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        return None
    try:
        return sum(1 for row in json.loads(r.stdout) if row.get("state") != "OK")
    except (ValueError, TypeError):
        return None


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


def note_list(notes: list[tuple[str, str]]) -> str:
    """`open PB2151 PB2152 landed PB2108 …`: grouped by status, so a long cluster reads as runs, not a roll call."""
    if not notes:
        return '<span class="dim">no note filed yet</span>'
    by: dict[str, list[str]] = collections.defaultdict(list)
    for nid, s in notes:
        by[s].append(nid)
    return " ".join(f'{status_pill(s)} <span class="mono">{e(" ".join(ids))}</span>'
                    for s, ids in sorted(by.items(), key=lambda kv: (ledger_plan.finished(kv[0]), kv[0])))


def phase_state(p: dict) -> str:
    if not p["notes"]:
        return "not started"
    return "landed" if all(ledger_plan.finished(s) for _, s in p["notes"]) else "open"


def lane_cards(inv: dict, plan: dict, pacing: dict | None) -> str:
    """The six lanes as cards: each one's definition of done and its measured progress."""
    rt, c = plan["retire"], plan["counts"]
    gates_passed = sum(1 for g in plan["gates"] if ledger_plan.finished(g["status"]))
    nxt = next((g["label"] for g in plan["gates"] if not ledger_plan.finished(g["status"])), None)
    review = " · ".join(
        f'{p["key"]} {e(phase_state(p))}'
        + (f' ({e(plan["r1_status"])})' if p["key"] == "R1" and plan["r1_status"] else "")
        for p in plan["phases"])
    if pacing:
        r = pacing["reading"]
        pace = (f'week <span class="num">{r["weekly_pct"]:g} %</span> of the {pacing["weekly_cap"]:g} % cap · session '
                f'<span class="num">{r["session_pct"]:g} %</span> (soft stop {pacing["soft"]:g} %)')
        stop = ("crit", "paused") if r["weekly_pct"] >= pacing["weekly_cap"] else (
            ("warn", "session stop") if r["session_pct"] >= pacing["soft"] else ("good", "within limits"))
    else:
        pace, stop = "no meter reading on the machine that rendered this page", ("warn", "no reading")

    def done(finished: bool) -> tuple[str, str]:
        return ("good", "done") if finished else ("warn", "open")

    progress = {
        "retirement": (f'<span class="num">{n(rt["total"] - rt["unfinished"])}</span> of {n(rt["total"])} notes landed',
                       *done(not rt["unfinished"])),
        "conformance": (f'<span class="num">{n(inv["closed"])}</span> of {n(inv["rows"])} rules closed · GAP '
                        f'<span class="num">{n(inv["gap"])}</span>', *done(not inv["gap"])),
        "external": (f'<span class="num">{n(c["external_landed"])}</span> of {n(c["external_total"])} slices landed',
                     *done(c["external_landed"] == c["external_total"])),
        "review": (review, *done(phase_state(plan["phases"][-1]) == "landed")),
        "gates": (f'<span class="num">{n(gates_passed)}</span> of {n(len(plan["gates"]))} passed'
                  + (f"; next: {e(nxt)}" if nxt else ""), *done(nxt is None)),
        "pacing": (pace, *stop),
    }
    out = []
    for i, (key, name, definition) in enumerate(ledger_plan.LANES, 1):
        text, pill, word = progress[key]
        out.append(f'<div class="card"><h3>{i} · {e(name)} <span class="pill {pill}">{e(word)}</span></h3>'
                   f'<p>{text}</p><p>Done when: {e(definition)}</p></div>')
    return "\n    ".join(out)


def render_retirement(plan: dict) -> str:
    rt = plan["retire"]
    tag = (f'The annotated tag <span class="mono">{e(ledger_plan.LEGACY_TAG)}</span> keeps the engine\'s last commit, '
           f'<span class="mono">{e(plan["legacy_tag"])}</span>.' if plan["legacy_tag"] else
           f'The tag <span class="mono">{e(ledger_plan.LEGACY_TAG)}</span> is not in this clone.')
    return (f'<p>The legacy CobolSharp engine, deleted ahead of v1.0 (kb/Work R69 §1). Cluster '
            f'<span class="mono">{e(rt["lead"])}</span>: <strong class="num">{n(rt["total"] - rt["unfinished"])}</strong> '
            f'of {n(rt["total"])} notes landed, {n(rt["waiting"])} unfinished note(s) waiting on another note. {tag}</p>\n'
            f'  <div class="tablecard"><table><tbody><tr><td>{note_list(rt["notes"])}</td></tr></tbody></table></div>')


def render_external(plan: dict) -> str:
    rows = "\n        ".join(
        f'<tr><td class="r num">{num}</td><td class="mono">{e(nid)}</td><td>{e(title.split(": ", 1)[-1])}</td>'
        f'<td>{status_pill(s)}</td></tr>' for num, nid, s, title in plan["slices"])
    c = plan["counts"]
    return (f'<p>The owner-approved §8.13 external-repository design, landed slice by slice; R1 takes it as given. '
            f'<strong class="num">{n(c["external_landed"])}</strong> of {n(c["external_total"])} slices landed.</p>\n'
            f'  <div class="tablecard"><table>\n'
            f'      <thead><tr><th class="r">Slice</th><th>Note</th><th>What it lands</th><th>Status</th></tr></thead>\n'
            f'      <tbody>\n        {rows}\n      </tbody>\n    </table></div>')


def render_review(plan: dict, series: list[dict]) -> str:
    ev = "<br>".join(f'{e(label)} <span class="mono">{e(r["sha"])}</span> <span class="dim">{e(r["date"])}</span>'
                     if r else f'{e(label)} <span class="dim">no record</span>' for label, r in plan["evidence"])
    r1 = (f'design §8: <span class="mono">{e(plan["r1_status"])}</span>' if plan["r1_status"]
          else '<span class="dim">§8 not on this tree yet</span>')
    evidence = {"R0": ev, "R1": r1}
    phase_rows = "\n        ".join(
        f'<tr><td class="mono">{e(p["key"])} <span class="dim">{e(p["name"])}</span></td>'
        f'<td>{status_pill(phase_state(p)) if not p["notes"] else note_list(p["notes"])}</td>'
        f'<td>{evidence.get(p["key"], "")}</td></tr>' for p in plan["phases"])
    rv = plan["review"]
    cols = rv["statuses"]
    grid_rows = "\n        ".join(
        f'<tr><td>{e(k)}</td>' + "".join(f'<td class="r num">{n(c[s])}</td>' for s in cols)
        + f'<td class="r num">{n(sum(c.values()))}</td></tr>' for k, c in rv["grid"].items())
    col_tot = "".join(f'<td class="r num">{n(sum(c[s] for c in rv["grid"].values()))}</td>' for s in cols)
    gate_of = {g["id"]: g["label"] for g in plan["gates"] if g.get("id")}
    blockers = ("\n        ".join(
        f'<tr><td class="mono">{e(b)}</td><td>{e(gate_of.get(b) or title[:110])}</td><td class="r num">{n(k)}</td></tr>'
        for b, title, k in rv["blockers"])
        or '<tr><td colspan="3" class="dim">No unfinished note of the cluster waits on another note.</td></tr>')
    recent = series[-8:]
    trend_rows = "\n        ".join(
        f'<tr><td class="mono">{e(p["date"])} {e(p["sha"])}</td><td class="r num">{n(p["open"])}</td>'
        f'<td class="r num">{n(p.get("waiting", 0))}</td><td class="r num">{n(p["landed"])}</td>'
        f'<td class="r num">{n(p["total"])}</td></tr>' for p in recent)
    return f"""<p>The comprehensive architecture review (<span class="mono">DESIGN-architecture-review.md</span> §3), started ahead of zero GAP by kb/Work R69 §2: R0 and R1 now, R3's leaf waves and the Delete program (cluster <span class="mono">{e(ledger_plan.REVIEW_PROGRAM)}</span>) between fix-lane trains, R2 and R3 over binding and code generation once GAP is near zero. Every wave is behavior-neutral by contract, so none of it moves the GAP.</p>
  <div class="tablecard">
    <table>
      <thead><tr><th>Phase</th><th>Notes in the register</th><th>Evidence</th></tr></thead>
      <tbody>
        {phase_rows}
      </tbody>
    </table>
  </div>
  <h3>R3 · the program's waves (cluster {e(ledger_plan.REVIEW_PROGRAM)}), by wave kind</h3>
  <p>One note is one wave; its kind is the title prefix <span class="mono">scripts/arch/file_census_notes.py</span> files it under. <strong class="num">{n(rv["total"] - rv["unfinished"])}</strong> of {n(rv["total"])} landed; {n(rv["waiting"])} unfinished note(s) wait on another note.</p>
  <div class="tablecard">
    <table>
      <thead><tr><th>Wave kind</th>{"".join(f'<th class="r">{e(s)}</th>' for s in cols)}<th class="r">Total</th></tr></thead>
      <tbody>
        {grid_rows}
      </tbody>
      <tfoot><tr><td>Total</td>{col_tot}<td class="r num">{n(rv["table_total"])}</td></tr></tfoot>
    </table>
  </div>
  <div class="tablecard" style="margin-top:14px">
    <table>
      <thead><tr><th>Waits on</th><th>Gate or note</th><th class="r">Notes</th></tr></thead>
      <tbody>
        {blockers}
      </tbody>
    </table>
  </div>
  <div class="tablecard" style="margin-top:14px">
    <table>
      <thead><tr><th>Program notes, last {len(recent)} recorded points</th><th class="r">Unfinished</th><th class="r">Waiting</th><th class="r">Landed</th><th class="r">Total</th></tr></thead>
      <tbody>
        {trend_rows}
      </tbody>
    </table>
  </div>
  <p>Program notes are the phase notes plus both clusters' notes ({e(ledger_plan.RETIREMENT)}, {e(ledger_plan.REVIEW_PROGRAM)}), counted once. The series is the trend file's <span class="mono">program</span> list, a point appended whenever those counts move: a numeric table rather than a second chart, because its scale and cadence share nothing with the GAP line's.</p>"""


def render_gates(plan: dict) -> str:
    rows = []
    for i, g in enumerate(plan["gates"], 1):
        if "gap" in g:
            what = f'GAP <span class="num">{n(g["gap"])}</span>' + (
                " — the owner judges “near”" if g["threshold"] is None else f' — passes at {n(g["threshold"])}')
        elif g["id"]:
            what = f'<span class="mono">{e(g["id"])}</span>'
        else:
            what = '<span class="dim">its note is not filed yet</span>'
        rows.append(f'<tr><td class="r num">{i}</td><td>{e(g["label"])}</td><td>{what}</td>'
                    f'<td>{status_pill(g["status"])}</td></tr>')
    return ('<p>The points where one lane waits for another, in order. Each is measured from its note, or from the '
            'inventory for the GAP gates.</p>\n  <div class="tablecard"><table>\n'
            '      <thead><tr><th class="r">#</th><th>Gate</th><th>Measured by</th><th>State</th></tr></thead>\n'
            '      <tbody>\n        ' + "\n        ".join(rows) + '\n      </tbody>\n    </table></div>')


def render_pacing(pacing: dict | None) -> str:
    if pacing is None:
        return ('<p>Not available: the machine that rendered this page has no coordination directory with a meter '
                'reading (<span class="mono">readings.json</span>). The owner\'s caps are in '
                '<span class="mono">scripts/orchestrator/model_rules.json</span>.</p>')
    r = pacing["reading"]
    return f"""<p>The owner's rules (kb/Work R69 §5-§6): {e(pacing["cap_source"])}. The newest meter reading, noted <span class="mono">{e(r["noted_at"][:16].replace("T", " "))} UTC</span>, against the caps in <span class="mono">model_rules.json</span>.</p>
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
    head, pts, plan, pacing = ctx["head"], ctx["trend"], ctx["plan"], ctx["pacing"]
    pct = 100.0 * inv["closed"] / inv["rows"]
    kinds = " · ".join(f'{n(v)} {k}' for k, v in inv["kind"].most_common())
    top = " · ".join(i["id"] for i in wk["actionable"][:3])
    parked = ", ".join(i["id"] for i in wk["owner_parked"]) or "none"
    a4 = cf["a4"]
    a3 = cf["a3"]
    bat_gap = gap_at(str(bat.get("sha", "")))
    gap_delta = None if bat_gap is None else inv["gap"] - bat_gap
    pc = plan["counts"]

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
        would_close=n(inv["closed"] + inv["resolved_owed"]),
        would_pct=f'{100.0 * (inv["closed"] + inv["resolved_owed"]) / inv["rows"]:.0f}',
        doc_rows=n(inv["doc_rows"]), doc_verdicted=n(inv["doc_verdicted"]), doc_closed=n(inv["doc_closed"]),
        clause_rows=clause_rows(inv["buckets"]),
        work_items=n(wk["items"]), work_open=n(wk["status"]["open"]), work_landed=n(wk["status"]["landed"]),
        work_half=n(wk["status"]["half"]), work_owner=n(wk["status"]["owner"]),
        work_actionable=n(len(wk["actionable"])), work_top=e(top), work_parked=e(parked),
        work_kinds=" · ".join(f'{n(v)} {k}' for k, v in wk["kind"].most_common()),
        a1_items=n(a1["items"]), a1_scope=n(a1["scope"]), a1_discharged=n(a1["discharged"]),
        a1_remaining=n(a1["remaining"]), a1_withdrawn=n(len(a1["unreachable"])),
        a1_pct=f'{100.0 * a1["discharged"] / a1["scope"]:.1f}',
        a1_findings=("no findings: every §7 determination names an A.1 item whose element it matches"
                     if not a1["findings"] else f'{len(a1["findings"])} FINDING(S) — run the audit'),
        a3_rows=n(cf["a3_rows"]), a3_claimed=n(a3["claimed"]), a3_partial=n(a3["partial"]),
        a3_not=n(a3["not claimed"]), a3_na=n(a3["n/a"]),
        a4_rows=n(cf["a4_rows"]), a4_claimed=n(a4["claimed"]), a4_partial=n(a4["partial"]),
        a4_not=n(a4["not claimed"]),
        a4_claimed_list=e(" · ".join(cf["a4_by"]["claimed"])),
        a4_partial_list=e(" · ".join(cf["a4_by"]["partial"])),
        a4_not_list=e(" · ".join(cf["a4_by"]["not claimed"])),
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
        gap_delta_word=("not recorded" if gap_delta is None else "unchanged" if gap_delta == 0 else f"{gap_delta:+d}"),
        p_landed=n(pc["landed"]), p_total=n(pc["total"]), p_waiting=n(pc["waiting"]),
        p_pct=f'{100.0 * pc["landed"] / pc["total"]:.1f}' if pc["total"] else "0",
        lane_cards=lane_cards(inv, plan, pacing),
        lane_retirement=render_retirement(plan), lane_external=render_external(plan),
        lane_review=render_review(plan, ctx["program_series"]), lane_gates=render_gates(plan),
        pacing=render_pacing(pacing),
        in_flight=ctx["in_flight"],
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
  <p class="eyebrow">The completion plan · kb/Work R69 · ISO/IEC 1989:2023 · Four editions</p>
  <h1>{title}</h1>
  <p class="asof">As of <b>{head_date}</b> · measured tree <b class="mono">{head_sha}</b> · last comprehensive battery <b>#{bat_n}</b> on <b class="mono">{bat_sha}</b> — <b>{bat_since} commits back</b>, {bat_since_code} of them touching <span class="mono">src/</span> or <span class="mono">tests/</span></p>
  <p class="mission">v1.0 is <strong>a conforming implementation of the whole of ISO/IEC 1989 in each of its four editions</strong> (85 / 2002 / 2014 / 2023), as §4.2.1 defines conformance, measured by one instrument: every normative rule as a row of the traceability inventory, at <strong>zero GAP</strong>. The completion plan the owner decided on 2026-10-06 (kb/Work R69) runs six lanes toward it at once: the legacy engine's retirement, conformance to zero GAP, the external repository, the architecture review, the gates between them, and the owner's pacing. This page measures each lane from the register and the evidence folders.</p>
</header>

<section aria-label="Headline">
  <div class="meter-row">
    <div class="meter-card">
      <div class="meter-head"><span class="t">Conformance: rules closed</span><span class="v num">{closed} of {rows} · GAP {gap}</span></div>
      <div class="meter"><div class="fill" style="width:{pct}%"></div></div>
      <p class="note">v1.0's conformance half is GAP at zero.</p>
    </div>
    <div class="meter-card">
      <div class="meter-head"><span class="t">Re-architecture program: notes landed</span><span class="v num">{p_landed} of {p_total} · {p_waiting} waiting</span></div>
      <div class="meter"><div class="fill" style="width:{p_pct}%"></div></div>
      <p class="note">The retirement cluster, the review's phase notes and the review program's waves, counted once.</p>
    </div>
  </div>
</section>

<section aria-label="Lanes">
  <h2>The plan, lane by lane</h2>
  <div class="cardgrid">
    {lane_cards}
  </div>
</section>

<section aria-label="Lane 1">
  <h2>1 · Legacy retirement</h2>
  {lane_retirement}
</section>

<section aria-label="Lane 2">
  <h2>2 · Conformance to zero GAP</h2>
  <div class="hero">
    <div class="hero-figure">
      <span class="big num">{closed}</span>
      <span class="of">of {rows} rules closed</span>
      <span class="pct">{pct}%</span>
    </div>
    <p class="hero-sub">One row per normative rule of the standard ({kinds}). A row closes only when its verdict resolves — CONFORMS, or a documented-non-support determination — <em>and</em> a spec-derived witness covers it. Adjudicating a rule <strong>opens</strong> work; only a witness closes it, so the second band below is the page's real backlog.</p>
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
    <div class="tile">
      <div class="label">Annex A.1 remaining</div>
      <div class="value num">{a1_remaining}</div>
      <div class="note">of {a1_scope} obligations in scope; the declared v1.0 documentation gap</div>
    </div>
  </div>

  <div class="trend">
    <div class="trend-head">
      <span class="t">GAP burn-down, per GAP-moving landing</span>
      <span class="v num">{trend_from} → {trend_to} · {trend_points} points</span>
    </div>
    {trend_svg}
    <p class="note">Each point is a landing that moved the inventory, measured from that commit's own <span class="mono">traceability-inventory.json</span>. A flat step is real and worth reading: correcting an already-CONFORMS row moves nothing, and the derived-verdict lane resolves verdicts without closing rows — a declined row closes on its <em>witness</em>. Series: <span class="mono">docs/rearchitecture/evidence/ledger-trend.json</span>, appended by <span class="mono">gen_ledger.py</span>.</p>
  </div>
</section>

<section aria-label="Lane 3">
  <h2>3 · The external repository</h2>
  {lane_external}
</section>

<section aria-label="Lane 4">
  <h2>4 · The architecture review R0-R5</h2>
  {lane_review}
</section>

<section aria-label="Lane 5">
  <h2>5 · The gates between the lanes</h2>
  {lane_gates}
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

<section aria-label="Remaining work">
  <h2>Conformance detail · the three veins of remaining work</h2>
  <div class="cardgrid">
    <div class="card">
      <h3>1 · Adjudication mass</h3>
      <p><strong>{unadjudicated} rows</strong> have never been read against the standard. Adjudication <em>opens</em> more than it closes — it maps the territory; fixing and witnessing finish it. Under owner decision PB278 it runs as its own lane rather than between fixes.</p>
    </div>
    <div class="card">
      <h3>2 · The witness bottleneck</h3>
      <p>Every resolved verdict needs a spec-derived witness — NIST and characterization runs deliberately don't qualify. <strong>{resolved_owed} rows</strong> ({conforms_owed} CONFORMS + {dns_owed} documented-non-support) wait on nothing but that: witnessed, they take closed rows from {closed} to <strong>{would_close}</strong> — {would_pct}% of the standard — without a line of compiler change.</p>
    </div>
    <div class="card">
      <h3>3 · Documentation obligations</h3>
      <p>Annex A.1's implementor-documentation register plus the version-change ledger — the paper half of §4.2.16, and a declared v1.0 gap. <strong>{a1_remaining} of {a1_scope}</strong> obligations remain; each is a determination to be made, not a recording exercise.</p>
    </div>
  </div>

  <div class="meter-row" style="margin-top:14px">
    <div class="meter-card">
      <div class="meter-head"><span class="t">Annex A.1 documentation obligations discharged</span><span class="v num">{a1_discharged} of {a1_scope} · {a1_remaining} remain</span></div>
      <div class="meter"><div class="fill" style="width:{a1_pct}%"></div></div>
      <p class="note">{a1_items} implementor-defined elements; {a1_scope} carry a documentation duty — {a1_withdrawn} items belong to a declined module, and A.1's own preamble withdraws the duty for those. That subtraction is <em>derived</em> from the same A.4 selectors the inventory uses, never asserted. <span class="mono">audit_annex_a1.py</span> owns the count and reports {a1_findings}.</p>
    </div>
    <div class="meter-card">
      <div class="meter-head"><span class="t">Version-change ledger anchors dispositioned</span><span class="v num">{vcr_done} of {vcr_total} · {vcr_todo} todo</span></div>
      <div class="meter"><div class="fill" style="width:{vcr_pct}%"></div></div>
      <p class="note">P14 Step 1 drives the <span class="mono">&lt;!-- todo --&gt;</span> anchors to zero: each becomes a gated <span class="mono">constructs.json</span> row with a negative witness, or a written disposition. Counted mechanically off the anchors themselves: {vcr_gated} gated · {vcr_ref} ref-only · {vcr_pin} pinned-to-spec · {vcr_todo} todo. The matrix registry stands at {constructs} constructs.</p>
    </div>
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
        <tr><td>Annex A.1 — implementor-defined register</td><td><span class="pill warn">{a1_remaining} REMAIN</span>&ensp;<span class="dim">of {a1_scope} in scope; the declared v1.0 conformance gap</span></td></tr>
      </tbody>
    </table>
  </div>
</section>

<section aria-label="In flight">
  <h2>In flight right now</h2>
{in_flight}
</section>

<section aria-label="Pacing">
  <h2>6 · Pacing</h2>
  {pacing}
</section>


<footer>
  <p><strong>Every number on this page is computed from the tree it names — none is remembered.</strong> Inventory from <span class="mono">tests/version-matrix/traceability-inventory.json</span>, work standing from <span class="mono">kb/Work</span> through <span class="mono">work.py</span>'s own predicate, the plan's lanes from the register's clusters and notes through <span class="mono">ledger_plan.py</span>, the R0 evidence folders and the orchestrator's newest meter reading, gates from plan §0's battery reference, documentation posture from <span class="mono">docs/CONFORMANCE.md</span> §2/§4/§5 and <span class="mono">audit_annex_a1.py --json</span>, the corpus and anchor counts from the trees themselves, the trend series from <span class="mono">docs/rearchitecture/evidence/ledger-trend.json</span>. Rendered by <span class="mono">scripts/spec/gen_ledger.py</span>; the only hand-written section is “In flight right now”. The work register remains <span class="mono">kb/Work/</span> — this ledger is a derived view, never a tracker.</p>
  <p class="mono">Snapshot: {head_date} · measured tree {head_sha} — the last commit that touched an input this page reports, so an unrelated commit cannot make a correct page look stale · {rows} rows · {closed} closed · {gap} GAP · {adjudicated} adjudicated · battery #{bat_n} on {bat_sha} · A.1 {a1_discharged}/{a1_scope} · register {work_items} notes, {work_open} open, {work_actionable} actionable.</p>
</footer>

</div>
"""


# ─────────────────────────────────────────────────────────────────────────────────────────────────────────────

def build(in_flight_path: pathlib.Path | None) -> tuple[str, dict]:
    """The page, and the trend file's two series with whether each gained its current point."""
    import work  # noqa: PLC0415
    inv = measure_inventory()
    head = measure_head()
    bat = measure_battery(head["sha"])
    pts, is_new = trend_series(inv, head, bat)
    plan = ledger_plan.measure(work.load(), ledger_plan.wave_prefixes(), inv["gap"])
    prog_pts, prog_new = program_series(plan, head)
    frag = ""
    if in_flight_path is not None and in_flight_path.exists():
        # VERBATIM. The fragment lands inside <section aria-label="In flight"> after its <h2>; it is narrative,
        # and a renderer that reformatted it would be a second author of the only part a person writes.
        # The ONE exception is the file's own leading comment block: that is instructions TO the author about
        # what may be written here, not content, and it would otherwise ship inside the published page.
        frag = re.sub(r"\A(?:\s*<!--.*?-->)+\s*", "", in_flight_path.read_text(encoding="utf-8"), flags=re.S)
        frag = frag.rstrip("\n")
    ctx = {"inv": inv, "work": measure_work(), "a1": measure_annex_a1(), "conf": measure_conformance(),
           "battery": bat, "corpus": measure_corpus(), "head": head, "trend": pts,
           "plan": plan, "program_series": prog_pts, "pacing": ledger_plan.measure_pacing(), "in_flight": frag}
    return render(ctx), {"points": pts, "is_new": is_new, "program": prog_pts, "prog_new": prog_new}


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--out", type=pathlib.Path, default=DEFAULT_OUT,
                    help=f"HTML to write (default {DEFAULT_OUT.relative_to(REPO)})")
    ap.add_argument("--in-flight", type=pathlib.Path, default=IN_FLIGHT,
                    help="the ONE hand-written section, inserted verbatim (default "
                         f"{IN_FLIGHT.relative_to(REPO)}); pass a missing path to omit it")
    ap.add_argument("--check", action="store_true",
                    help="do not write; exit non-zero if --out differs from what the repo would render now, or if "
                         "the plan's lanes do not read cleanly off the register (ledger_plan.problems)")
    a = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    bad = ledger_plan.live_problems()
    if bad:
        print("⛔ THE PLAN'S LANES CANNOT BE TRUSTED (scripts/spec/ledger_plan.py --self-test):")
        for b in bad:
            print(f"   {b}")
        return 1

    text, tr = build(a.in_flight)
    pts, is_new = tr["points"], tr["is_new"]

    if a.check:
        if not a.out.exists():
            # ⛔ ABSENT IS NOT STALE — the gen_conformance_notes.py contract, and for the same reason: the
            # rendered page is a gitignored build output, so on a fresh clone and in CI it legitimately does
            # not exist. Reporting that as a failure would be a manufactured red
            # (feedback_verdict_evidence_invariant).
            print(f"· {a.out} does not exist (gitignored build output) — nothing to check.")
            return 0
        if a.out.read_text(encoding="utf-8") == text:
            print(f"✓ {a.out.name} matches the repo exactly ({len(text):,} bytes)")
            return 0
        print("⛔ THE CONFORMANCE LEDGER IS STALE — it describes a tree the repo has moved past.")
        if is_new:
            print(f"      the trend series is missing the current point "
                  f"(GAP {pts[-1]['gap']} at {pts[-1]['sha']})")
        if tr["prog_new"]:
            print(f"      the program series is missing the current point "
                  f"({tr['program'][-1]['open']} unfinished of {tr['program'][-1]['total']})")
        print("   Run: python scripts/spec/gen_ledger.py    then publish --out to the artifact's URL")
        return 1

    if is_new or tr["prog_new"]:
        write_trend(pts, tr["program"])
    if is_new:
        print(f"trend  : appended {pts[-1]['sha']} — GAP {pts[-1]['gap']} · closed {pts[-1]['closed']} · "
              f"DNS {pts[-1]['dns']}   ({TREND.relative_to(REPO)}, {len(pts)} points)")
    else:
        print(f"trend  : unchanged — {len(pts)} points, last {pts[-1]['sha'] if pts else '—'}")
    pp = tr["program"][-1]
    print(f"program: {'appended' if tr['prog_new'] else 'unchanged'} — {pp['open']} unfinished · {pp['landed']} "
          f"landed of {pp['total']} at {pp['sha']} ({len(tr['program'])} points)")
    a.out.parent.mkdir(parents=True, exist_ok=True)
    a.out.write_text(text, encoding="utf-8")
    print(f"wrote  : {a.out} ({len(text):,} bytes)")
    print("publish: Artifact tool, action publish, url https://claude.ai/code/artifact/"
          "7677f3c0-d6cf-41a2-a7c3-7d83c5fdbaee — same url, no favicon on redeploy")
    return 0


if __name__ == "__main__":
    sys.exit(main())
