#!/usr/bin/env python3
"""Generate the Obsidian conformance view over the traceability inventory — AGGREGATES, not one note per rule.

⛔ DESIGN CORRECTION. The first version emitted one note per rule: 3,481 files created in a single run, an 82%
increase in the vault's markdown, on top of two ~3 MB indexed documents. It wedged Obsidian mid-sync. The
per-rule frontmatter did buy live Dataview filtering, but that is not worth a 5x vault and a client that will not
open — and the queryable SSOT already exists as JSON, which jq and python query far better than Dataview does.

So the vault view is now the BURN-DOWN, at ~13 notes: a dashboard plus one note per top-level clause. It shows
aggregates and points at the inventory for the full list. Deliberately NOT an exhaustive per-rule enumeration —
3,790 table rows spread over 11 notes would recreate the same problem in a different shape, and the tracker's own
rule for enumerating drift-prone items applies here too.

`kb/Conformance/` is a RENDERED VIEW (kb/Work PB3026; scripts/rendered_views.py): gitignored, never committed, rendered
on demand. It went stale once because it was a file someone had to remember to regenerate (2026-08-04: five of twelve
clause notes wrong, §15 saying **533 GAP where the live number was 481**, the dashboard under-reporting real progress
by 52 rows while still read as current). The fix is not a staleness check on a copy: nothing is promised about a copy,
so the session probe RENDERS the view at every session start, and `--check` only proves the inventory still produces
one (the verdict-evidence invariant, DESIGN-test-build-ci.md §3.10: a view that is absent is not stale).

    python scripts/spec/gen_conformance_notes.py --out kb/Conformance   # render (replaces a previous render)
    python scripts/spec/gen_conformance_notes.py --check                # non-zero when the inventory renders no view
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys
from collections import Counter, defaultdict

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import generated_output  # noqa: E402  (the one way a generator clears the directory it writes: never wholesale)

REPO = pathlib.Path(__file__).resolve().parents[2]
INVENTORY = REPO / "tests" / "version-matrix" / "traceability-inventory.json"

KIND_LABEL = {
    "SR": "syntax rules", "GR": "general rules", "AR": "argument rules",
    "RV": "returned value rules", "DOC": "Annex A.1 documentation obligations",
    "FMT": "general formats (syntax diagrams)",
}


def clause_key(c: str):
    return (0, int(c), "") if c.isdigit() else (1, 0, c)


def build_notes(rows: list[dict]) -> dict[str, str]:
    """The note set as {filename: content} — ONE source of truth for both writing and --check.

    Building into a dict rather than writing as we go is what makes staleness detectable at all: a checker that
    re-derived the text separately from the writer would drift from it, and then agree with the wrong thing.
    """
    by_clause: dict[str, list[dict]] = defaultdict(list)
    for r in rows:
        by_clause[r["section"].split(".")[0]].append(r)

    total = len(rows)
    gaps = sum(1 for r in rows if r["state"] == "GAP")
    kinds = Counter(r["kind"] for r in rows)
    notes: dict[str, str] = {}

    for clause, items in by_clause.items():
        k = Counter(i["kind"] for i in items)
        g = sum(1 for i in items if i["state"] == "GAP")
        adj = sum(1 for i in items if i["verdict"])
        subjects = Counter(i["subject"] for i in items)
        note = [
            "---",
            f'title: "§{clause} conformance"',
            f'description: "{len(items)} normative items in clause {clause}; {g} still GAP."',
            f'clause: "{clause}"', f"items: {len(items)}", f"gap: {g}",
            # `adjudicated` and `resolved` are separate on purpose: adjudicating a rule OPENS work (it records a
            # verdict) while only a spec-derived test CLOSES it, so a clause can be fully adjudicated and still
            # be entirely GAP. Collapsing them into one number is what makes a burn-down look stalled.
            f"adjudicated: {adj}", f"resolved: {len(items) - g}",
            "generated: true",
            "tags: [cobolsharp, conformance, generated]",
            "---", "",
            f"# §{clause} — {len(items)} items, {g} GAP", "",
            "> ⚙ **Generated** from `tests/version-matrix/traceability-inventory.json`. Do not edit — record",
            "> verdicts in the inventory and re-render with `python scripts/rendered_views.py conformance-burn-down`.", "",
            "| kind | items |", "|---|---:|",
        ]
        for kk, n in sorted(k.items()):
            note.append(f"| {KIND_LABEL.get(kk, kk)} | {n} |")
        note += ["", "## Largest subjects in this clause", ""]
        for subj, n in subjects.most_common(15):
            note.append(f"- **{subj}** — {n} item(s)")
        note += ["",
                 "## The full list",
                 "",
                 "Deliberately not enumerated here — 3,790 rows across these notes is what wedged the vault the",
                 "first time, and per-item rows drift. Query the inventory instead:",
                 "",
                 "```",
                 "python -c \"import json;rows=json.load(open(r'tests/version-matrix/traceability-inventory.json'"
                 ",encoding='utf-8'));"
                 f"print([r['rule-id'] for r in rows if r['section'].startswith('{clause}.') "
                 "and r['state']=='GAP'][:40])\"",
                 "```", "",
                 "See also [[kb/Conformance/_Dashboard|the conformance dashboard]].", ""]
        notes[f"clause-{clause}.md"] = "\n".join(note)

    adjudicated = sum(1 for r in rows if r["verdict"])
    dash = [
        "---", "title: Conformance Dashboard",
        f'description: "P14 burn-down over {total} normative ISO/IEC 1989:2023 items; v1.0 = zero GAP."',
        f"items: {total}", f"gap: {gaps}", f"adjudicated: {adjudicated}", f"resolved: {total - gaps}",
        "generated: true",
        "tags: [cobolsharp, conformance, generated, dashboard]", "---", "",
        "# Conformance dashboard", "",
        "> ⚙ **Generated** from the traceability inventory. `v1.0 = zero GAP` (owner decision D13).",
        "> This view is AGGREGATES by design — the per-rule detail lives in the inventory JSON, which queries",
        "> better than Dataview and does not cost 3,790 vault files.", "",
        f"**{total} items · {gaps} GAP · {total - gaps} resolved · {adjudicated} adjudicated**", "",
        "> ⚠ ADJUDICATED IS NOT RESOLVED. Recording a verdict OPENS work; only a spec-derived test closes a row,",
        "> so a clause can be fully adjudicated and still entirely GAP. A rising GAP alongside a rising",
        "> adjudicated count is the review working, not regressing.", "",
        "## By kind", "", "| kind | items |", "|---|---:|",
    ]
    for kk, n in sorted(kinds.items()):
        dash.append(f"| {KIND_LABEL.get(kk, kk)} | {n} |")
    dash += ["", "## Burn-down by clause", "", "| clause | items | GAP | adjudicated |", "|---|---:|---:|---:|"]
    for c, i in sorted(by_clause.items(), key=lambda kv: clause_key(kv[0])):
        dash.append(f"| [[kb/Conformance/clause-{c}|§{c}]] | {len(i)} | "
                    f"{sum(1 for r in i if r['state'] == 'GAP')} | "
                    f"{sum(1 for r in i if r['verdict'])} |")
    dash += ["",
             "## Related", "",
             "- [[kb/Conformance Burn-down.base|the sortable Bases view]] — the same data, filterable and sortable",
             "- [[kb/Work.base|the work register]] — every open defect, analysis and decision, ranked by harm",
             "- `docs/rearchitecture/spec-reconciliation/REPAIR-PLAN.md` — the repair order for all 210 defects",
             "- `docs/rearchitecture/DESIGN-spec-conformance-review.md` — the P14 Step-0 methodology", ""]
    notes["_Dashboard.md"] = "\n".join(dash)
    return notes


def _is_view_note(name: str) -> bool:
    """A note this view writes, at any revision of the inventory: the dashboard, or a clause note."""
    return name == "_Dashboard.md" or (name.startswith("clause-") and name.endswith(".md"))


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--out", type=pathlib.Path, metavar="DIR",
                    help="render the view into DIR (a previous render there is replaced); the registry's path is "
                         "kb/Conformance/")
    ap.add_argument("--check", action="store_true",
                    help="render in memory and exit non-zero when the inventory no longer produces a view")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if not INVENTORY.exists():
        sys.exit(f"inventory not found: {INVENTORY}\nRun: python scripts/spec/build_inventory.py")

    rows = json.loads(INVENTORY.read_text(encoding="utf-8"))
    notes = build_notes(rows)

    if args.check:
        # A rendered view is never compared with a copy on disk (kb/Work PB3026): the copy is not promised to be
        # current, and "absent" and "stale" are both just "not rendered yet". What can go wrong, and is checked, is
        # that the inventory stops producing a view: a row without a field `build_notes` reads raises above, and an
        # inventory with no rows renders a dashboard of zeros, which is no view of the standard (train 1063 review).
        if not rows:
            print(f"the inventory has no rows: {INVENTORY}")
            return 1
        print(f"the inventory renders {len(notes)} conformance notes")
        return 0
    if args.out is None:
        ap.error("one of --out DIR or --check is required")
    return write_view(rows, notes, args.out)


def write_view(rows: list[dict], notes: dict[str, str], out: pathlib.Path) -> int:
    """Write the view into ``out``, replacing a previous render there (the view is wholly derived, so nothing of it is
    kept). ``out`` is a directory the caller named, so only the view's own notes are removed: one holding anything
    else is refused, with nothing deleted (scripts/spec/generated_output.py)."""
    unusable = generated_output.prepare(out, _is_view_note)
    if unusable is not None:
        print(f"cannot render the view into {out}: {unusable}; name a new directory, or one holding only a previous "
              f"render (nothing was deleted)", file=sys.stderr)
        return 2
    out.mkdir(parents=True, exist_ok=True)
    for name, text in notes.items():
        (out / name).write_text(text, encoding="utf-8", newline="\n")

    total = len(rows)
    gaps = sum(1 for r in rows if r["state"] == "GAP")
    print(f"wrote {len(notes)} notes into {out}")
    print(f"  {total} items · {gaps} GAP · {len(notes) - 1} clause notes + 1 dashboard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
