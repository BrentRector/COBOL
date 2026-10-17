#!/usr/bin/env python3
"""Turn the grammar↔spec audit's findings into a `record_verdicts.py` batch — plan §5b step 4.

    python scripts/spec/grammar_findings_to_batch.py docs/rearchitecture/spec-reconciliation/grammar-*.json \
        --out scratchpad/batch-grammar.json
    python scripts/spec/grammar_findings_to_batch.py ... --coverage      # what the pass did NOT adjudicate
    python scripts/spec/grammar_findings_to_batch.py --self-test

⛔ WHY. `.claude/workflows/spec-grammar-conformance.js` scopes itself as "1,659 items: 321 general formats +
1,338 syntax rules"; the traceability inventory holds FMT 322 + SR 1,352 = 1,674 rows. **Those are the same
territory counted twice** (the deltas are plan §0's recorded denominator corrections), so running them as two
efforts means auditing ~1,670 rules twice. This converter makes ONE pass emit both: the grammar report the
workflow already writes, and an inventory batch.

⛔ AND WHY IT REFUSES TO EXPAND. A grammar finding is about a CONSTRUCT; an inventory row is about a RULE. It
would be trivial — and wrong — to take one finding for §14.9.1 and stamp it across all 19 of that clause's
rules. Nineteen rules would acquire a verdict from one observation, which is manufacturing coverage, and the
inventory would then read as adjudicated territory nobody examined. So a finding must name the `rule_ids` it
actually checked, and rules it did not name STAY A GAP. `--coverage` prints exactly what was left, because a
pass that silently looks complete is the failure this whole session has been closing.

THE VERDICT MAPPING, and the one that is deliberately absent:

    MATCHES          -> CONFORMS          (code-location = the .g4 site; still needs a spec-derived test to CLOSE)
    DIVERGES         -> DIVERGES          (code-location + notes: the divergence kind and the exact ISO syntax)
    NOT-IMPLEMENTED  -> NOT-IMPLEMENTED   (notes: no grammar rule implements the construct)
    UNCLEAR          -> nothing at all

⚠ UNCLEAR EMITS NO RECORD. The workflow defines it as "no decisive repro — do NOT guess", which is the absence
of an adjudication, not a kind of one. Writing it as NEEDS-OWNER-DECISION would file an agent's uncertainty as
a question for the owner and take the row out of the queue; leaving the row untouched keeps it in the queue,
where it belongs.
"""
from __future__ import annotations

import argparse
import glob
import json
import pathlib
import sys

from inventory_schema import Schema, load_catalog as load_rules, load_schema

VERDICT_MAP = {
    "MATCHES": "CONFORMS",
    "DIVERGES": "DIVERGES",
    "NOT-IMPLEMENTED": "NOT-IMPLEMENTED",
}
#: Only FMT and SR rows are in this audit's territory. A grammar pass has nothing to say about a GENERAL rule
#: (what the implementation DOES) or an intrinsic's argument/returned-value rules — those are the GR vein.
GRAMMAR_KINDS = {"FMT", "SR"}


def load_catalog() -> dict[str, dict]:
    """The rule catalog by id, read through `inventory_schema` — the one loader of the catalog file."""
    return {r["id"]: r for r in load_rules()}


def convert(findings: list[dict], catalog: dict[str, dict], schema: Schema) -> tuple[list[dict], list[str]]:
    """(records, problems). A finding that cannot be converted is a PROBLEM, never a silently dropped row.

    A finding's `editions` (a string in any spacing or order, or a list) is written in the field's one spelling
    by `Schema.spell_editions` (kb/Work PB3172); an edition the schema lacks or names twice is a problem.
    """
    records: list[dict] = []
    problems: list[str] = []
    seen: dict[str, str] = {}
    for f in findings:
        where = f"{f.get('section', '?')}/{f.get('construct', '?')}"
        verdict = f.get("verdict")
        if verdict == "UNCLEAR":
            continue                                            # deliberate: see the module docstring
        if verdict not in VERDICT_MAP:
            problems.append(f"{where}: unknown grammar verdict {verdict!r}")
            continue
        ids = f.get("rule_ids") or []
        if not ids:
            problems.append(f"{where}: verdict {verdict} carries NO rule_ids — nothing can be recorded from it, "
                            f"and expanding it across the section's rules is not allowed")
            continue
        given = f.get("editions") or ""
        names = [str(e) for e in given] if isinstance(given, list) else schema.split(str(given), schema.editions_sep)
        try:
            editions = schema.spell_editions(names)
        except ValueError as e:
            problems.append(f"{where}: {e}")
            continue
        # The band itself is the one validator's question (named, a run through the newest edition: kb/Work
        # PB3173), asked here so a finding with no band is a problem the converter names, not a refused batch.
        if err := schema.editions_error(editions):
            problems.append(f"{where}: {err}")
            continue
        for rid in ids:
            if rid not in catalog:
                problems.append(f"{where}: rule-id {rid!r} is not in the catalog")
                continue
            kind = catalog[rid]["kind"]
            if kind not in GRAMMAR_KINDS:
                problems.append(f"{where}: {rid} is a {kind} rule — a grammar pass cannot adjudicate it "
                                f"(FMT/SR only; GR is the semantic vein)")
                continue
            if rid in seen and seen[rid] != verdict:
                problems.append(f"{rid}: two findings disagree ({seen[rid]} vs {verdict})")
                continue
            seen[rid] = verdict
            rec = {"rule-id": rid, "verdict": VERDICT_MAP[verdict],
                   "code-location": f.get("grammar_site", ""), "test-ref": "",
                   "editions": editions, "notes": ""}
            if verdict == "DIVERGES":
                rec["notes"] = (f"grammar {f.get('divergence_kind', 'other')}: "
                                f"ISO requires {f.get('iso_syntax', '(unstated)')}; "
                                f"grammar says {f.get('grammar_says', '(unstated)')}").strip()
            elif verdict == "NOT-IMPLEMENTED":
                rec["notes"] = f"no grammar rule implements {f.get('construct', 'the construct')}"
            records.append(rec)
    return records, problems


def coverage(findings: list[dict], catalog: dict[str, dict]) -> dict[str, list[str]]:
    """Per section touched: the FMT/SR rules this pass did not ADJUDICATE. The point of the whole exercise is
    that this is VISIBLE — a grammar pass that settled 3 of a clause's 19 rules has not audited the clause.

    ⚠ AN `UNCLEAR` RULE COUNTS AS NOT ADJUDICATED, even though a finding named it. The first version counted
    any named rule as covered, so a pass that returned UNCLEAR for everything would have reported FULL
    coverage and zero records — the precise shape of "looks complete, observed nothing" this session has been
    closing everywhere else. Only a verdict that can become a record counts.
    """
    sections = {f.get("section") for f in findings if f.get("section")}
    named = {rid for f in findings if f.get("verdict") in VERDICT_MAP
             for rid in (f.get("rule_ids") or [])}
    out: dict[str, list[str]] = {}
    for sec in sorted(sections):
        rules = [r["id"] for r in catalog.values()
                 if r["kind"] in GRAMMAR_KINDS and (r["section"] == sec or r["section"].startswith(sec + "."))]
        missing = [r for r in rules if r not in named]
        if missing:
            out[sec] = missing
    return out


def self_test() -> int:
    cat = {
        "SR-14.9.1.3-1": {"id": "SR-14.9.1.3-1", "kind": "SR", "section": "14.9.1.3"},
        "SR-14.9.1.3-2": {"id": "SR-14.9.1.3-2", "kind": "SR", "section": "14.9.1.3"},
        "FMT-14.9.1.2-1": {"id": "FMT-14.9.1.2-1", "kind": "FMT", "section": "14.9.1.2"},
        "GR-14.9.1.4-1": {"id": "GR-14.9.1.4-1", "kind": "GR", "section": "14.9.1.4"},
    }
    base = {"section": "14.9.1", "construct": "ACCEPT", "grammar_site": "Core/X.g4#acceptStatement",
            "editions": "85,2002,2014,2023"}
    cases = [
        ("MATCHES becomes CONFORMS",
         [{**base, "verdict": "MATCHES", "rule_ids": ["SR-14.9.1.3-1"]}], 1, None),
        ("DIVERGES carries its ISO syntax in notes",
         [{**base, "verdict": "DIVERGES", "rule_ids": ["SR-14.9.1.3-2"],
           "divergence_kind": "too-restrictive", "iso_syntax": "ACCEPT x FROM y"}], 1, None),
        ("UNCLEAR emits nothing and is NOT a problem",
         [{**base, "verdict": "UNCLEAR", "rule_ids": ["SR-14.9.1.3-1"]}], 0, None),
        ("a finding with no rule_ids is REFUSED, not expanded",
         [{**base, "verdict": "MATCHES", "rule_ids": []}], 0, "NO rule_ids"),
        ("an unknown rule-id is caught",
         [{**base, "verdict": "MATCHES", "rule_ids": ["SR-99.9.9-1"]}], 0, "not in the catalog"),
        ("a GR rule is refused — a grammar pass cannot adjudicate one",
         [{**base, "verdict": "MATCHES", "rule_ids": ["GR-14.9.1.4-1"]}], 0, "cannot adjudicate"),
        ("two findings disagreeing about one rule is caught",
         [{**base, "verdict": "MATCHES", "rule_ids": ["SR-14.9.1.3-1"]},
          {**base, "verdict": "DIVERGES", "rule_ids": ["SR-14.9.1.3-1"]}], 1, "disagree"),
        ("an edition the schema lacks is a problem, not a record",
         [{**base, "verdict": "MATCHES", "rule_ids": ["SR-14.9.1.3-1"], "editions": "85,1974"}], 0, "not in"),
        ("a finding with no editions is a problem, not a record (kb/Work PB3173)",
         [{**base, "verdict": "MATCHES", "rule_ids": ["SR-14.9.1.3-1"], "editions": ""}], 0, "editions is empty"),
        ("a band that is not a run through the newest edition is a problem",
         [{**base, "verdict": "MATCHES", "rule_ids": ["SR-14.9.1.3-1"], "editions": "85,2023"}], 0, "not a run"),
    ]
    schema = load_schema()
    rc = 0
    print("=== grammar_findings_to_batch --self-test ===")
    for name, findings, want_n, want_problem in cases:
        recs, probs = convert(findings, cat, schema)
        ok = len(recs) == want_n and (want_problem is None or any(want_problem in p for p in probs))
        if want_problem is None and probs:
            ok = False
        print(("  ok: " if ok else "  SELF-TEST FAILED: ") + name)
        if not ok:
            print(f"      records={len(recs)} (want {want_n})  problems={probs}")
            rc = 1
    # a finding's editions, in any spacing, order or as a list, are written in the one spelling (kb/Work PB3172)
    for given in ("2023, 2002, 2014", ["2023", "2002", "2014"]):
        recs, _ = convert([{**base, "verdict": "MATCHES", "rule_ids": ["SR-14.9.1.3-1"], "editions": given}],
                          cat, schema)
        ok = [r["editions"] for r in recs] == ["2002,2014,2023"]
        print(("  ok: " if ok else "  SELF-TEST FAILED: ") + f"editions {given!r} are written in the one spelling")
        if not ok:
            print(f"      got {[r['editions'] for r in recs]}")
            rc = 1
    # coverage must NAME what was left unadjudicated
    left = coverage([{**base, "verdict": "MATCHES", "rule_ids": ["SR-14.9.1.3-1"]}], cat)
    ok = set(left.get("14.9.1", [])) == {"SR-14.9.1.3-2", "FMT-14.9.1.2-1"}
    print(("  ok: " if ok else "  SELF-TEST FAILED: ") + "coverage names the rules no finding adjudicated")
    if not ok:
        print(f"      got {left}")
        rc = 1
    # ⛔ and an UNCLEAR rule is NOT covered — a pass that decides nothing must not report full coverage.
    left = coverage([{**base, "verdict": "UNCLEAR", "rule_ids": ["SR-14.9.1.3-1"]}], cat)
    ok = "SR-14.9.1.3-1" in left.get("14.9.1", [])
    print(("  ok: " if ok else "  SELF-TEST FAILED: ") + "an UNCLEAR rule counts as NOT adjudicated")
    if not ok:
        print(f"      got {left}")
        rc = 1
    print("=== grammar_findings_to_batch --self-test: "
          + ("ALL GREEN" if not rc else "FAILED") + " ===")
    return rc


def main(argv: list[str]) -> int:
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass
    if "--self-test" in argv:
        return self_test()

    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("findings", nargs="+", help="grammar-*.json files written by the workflow (globs ok)")
    ap.add_argument("--out", type=pathlib.Path, help="write the record_verdicts batch here")
    ap.add_argument("--batch", default="grammar-audit", help="batch label")
    ap.add_argument("--coverage", action="store_true", help="also list the FMT/SR rules no finding adjudicated")
    a = ap.parse_args(argv)

    paths = [pathlib.Path(p) for pat in a.findings for p in glob.glob(pat)] or []
    if not paths:
        print("!! no findings files matched", file=sys.stderr)
        return 2
    findings: list[dict] = []
    for p in paths:
        doc = json.loads(p.read_text(encoding="utf-8"))
        findings.extend(doc.get("findings", doc if isinstance(doc, list) else []))
    catalog = load_catalog()
    records, problems = convert(findings, catalog, load_schema())

    print(f"{len(paths)} file(s), {len(findings)} finding(s) -> {len(records)} inventory record(s)")
    if a.coverage:
        left = coverage(findings, catalog)
        total_left = sum(len(v) for v in left.values())
        print(f"\n=== NOT ADJUDICATED by this pass: {total_left} FMT/SR rule(s) across {len(left)} section(s) ===")
        for sec, ids in left.items():
            print(f"  {sec:<14} {len(ids):>3} left  e.g. {', '.join(ids[:4])}")
    if problems:
        print(f"\n⛔ {len(problems)} PROBLEM(S) — nothing is written until these are resolved:")
        for p in problems:
            print(f"    {p}")
        return 1
    if a.out:
        a.out.parent.mkdir(parents=True, exist_ok=True)
        a.out.write_text(json.dumps({"batch": a.batch, "records": records}, indent=1, ensure_ascii=False),
                         encoding="utf-8", newline="\n")
        print(f"\nbatch -> {a.out}\n  next: python scripts/spec/record_verdicts.py --dry-run {a.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
