#!/usr/bin/env python3
"""Emit a verdict BATCH FILE from a `derived-verdicts` selector — the Python half of the shared predicate.

    python scripts/spec/derive_verdict_batch.py --list
    python scripts/spec/derive_verdict_batch.py screen-handling-only validate-only -o batch.json
    python scripts/spec/derive_verdict_batch.py --all -o out/a4-derived.json --notes-from decision
    python scripts/spec/derive_verdict_batch.py --self-test   # prove every ARM FIELD can fail

⛔ WHY THIS SCRIPT EXISTS AT ALL, AND WHY IT IS NOT A SECOND COPY OF THE PREDICATE. `inventory-schema.json`'s
`derived-verdicts` $comment has claimed since kb/Work PB198 that "the batch generator (Python) and the drift test
(C#) read the SAME predicate" — the whole justification for writing a selector as DATA rather than as code. That
was **false**: a grep for `derived-verdicts` across the repo returned this schema, `DerivedVerdictDriftTests.cs`
and some prose, and NOTHING under `scripts/`. The PB198 batch of sixteen rows was produced by hand, so the second
reader that makes the data-shape worth its cost did not exist, and three of the six A.4 module refuters found
that independently on the same day. The evaluator lives in `inventory_schema.py` (`DerivedSelector`); this file
is only a CLI over it, and the C# `DerivedVerdictDriftTests.Select` is the other engine. Neither carries a
selector.

⛔ THE SAFETY PROPERTY THIS SCRIPT OWNS, AND IT IS THE ONLY ONE `record_verdicts.py` CANNOT. A derived verdict is
sound exactly when the determination it derives from makes every selected rule UNREACHABLE — so a selector that
lands on a row already adjudicated CONFORMS, PARTIAL or DIVERGES has not found a new row, it has CONTRADICTED a
human reading of the code, silently, in a batch of three hundred. `record_verdicts.py` reports such a rewrite
(`⚠ n row(s) RE-ADJUDICATED`) but still applies it, which is right for an adjudication batch and wrong for this
one. Here it is a HARD FAILURE and nothing is written. A selected row may be blank, or may already carry the
selector's own verdict (agreement — the PB198 cross-check running in the reassuring direction); anything else
means the selector or the determination is wrong, and the answer is to re-derive, never to overwrite.

⛔ AND `--self-test` IS PART OF THE MECHANISM, NOT A CONVENIENCE. Two of the arm fields (`requirement`,
`determination-prefix`, added for kb/Work PB280 Q1) cannot be falsified against the live catalog and register —
all 30 A.1-optional items and all 47 §7 rows agree today, so a predicate that had dropped one of them selects
the same rows and leaves the C# drift test green. `--self-test` drives each axis against a FABRICATED catalog
and a FABRICATED register, one broken thing at a time, and `DerivedVerdictDriftTests` shells it every build.

⛔ AND THE ROWS DO NOT CLOSE. DOCUMENTED-NON-SUPPORT `resolves`, but `state_for` still demands a SPEC-DERIVED
test, because the schema closes a declined row only on evidence that the documented posture is what ACTUALLY
happens (see the schema's $verdict-vs-state). Every row a derived batch stamps stays a GAP until its module's
witness lands. This script prints the projected GAP delta — expected ZERO — so that is visible at generation
time rather than discovered as a disappointment after the merge.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys
from collections import Counter

from inventory_schema import DerivedSelector, load_catalog, load_inventory, load_schema

RECORD = "python scripts/spec/record_verdicts.py --dry-run {0}"


def self_test() -> int:
    """Prove every SELECTOR AXIS can fail, and for its own reason (`feedback_green_gates_arent_evidence`).

    ⛔ WHY THIS EXISTS AND WHY IT IS NOT THE C# DRIFT TEST'S JOB. `DerivedVerdictDriftTests` asserts each
    selector against the LIVE catalog and the LIVE register, which is the right check for a selector that has
    landed — and it is powerless over an axis the live data cannot separate. The two axes kb/Work PB280 Q1 added
    are exactly that case: today all 30 A.1-optional items and all 47 §7 rows agree, so every one of the four
    combinations that ought to discriminate collapses onto the same two rows, and a predicate that had dropped
    `requirement` altogether, or matched any determination at all, would select the same set and stay green.
    A MISSING observation is not a NEGATIVE one (`feedback_verdict_evidence_invariant`), so the axes are driven
    here against a FABRICATED catalog and a FABRICATED register, one broken thing at a time.
    """
    print("=== derive_verdict_batch --self-test ===")
    entry = {
        "verdict": "DOCUMENTED-NON-SUPPORT",
        "arms": [{"kinds": ["DOC"], "requirement": ["optional"], "determination-prefix": ["Not provided"]}],
    }
    rules = [
        {"id": "DOC-A.1-1", "kind": "DOC", "section": "A.1", "requirement": "optional", "text": "opt, declined"},
        {"id": "DOC-A.1-2", "kind": "DOC", "section": "A.1", "requirement": "required", "text": "req, declined"},
        {"id": "DOC-A.1-3", "kind": "DOC", "section": "A.1", "requirement": "optional", "text": "opt, provided"},
        {"id": "DOC-A.1-4", "kind": "DOC", "section": "A.1", "requirement": "optional", "text": "opt, no row"},
        {"id": "SR-1.1-1", "kind": "SR", "section": "1.1", "text": "an ordinary syntax rule"},
    ]
    register = {
        "DOC-A.1-1": "**Not provided.** the element is not implemented",
        # ⛔ THE ROW THE REQUIREMENT AXIS IS THE ONLY DEFENCE AGAINST. A REQUIRED item whose determination says
        # "Not provided." is not a derived row at all — it is a conformance DEFECT, and stamping it
        # DOCUMENTED-NON-SUPPORT would document non-conformance as if the owner had licensed it. A.1's preamble
        # licenses the decline for an OPTIONAL element and for a required one whose FEATURE is optional; it
        # licenses nothing here.
        "DOC-A.1-2": "**Not provided.** the element is not implemented",
        "DOC-A.1-3": "**Provided.** the value is 31 characters",
        "SR-1.1-1": "**Not provided.** a rule is not an A.1 item, and this row should not exist",
    }
    cases = [
        ("control: an OPTIONAL item §7 records as Not provided is selected", entry, ["DOC-A.1-1"]),
        ("the emphasis strip is real — a plain cell selects too",
         entry, ["DOC-A.1-1"], {**register, "DOC-A.1-1": "Not provided. no bold markers here"}),
        ("…and so does one written with single asterisks",
         entry, ["DOC-A.1-1"], {**register, "DOC-A.1-1": "*Not provided.* one marker"}),
        ("the REQUIREMENT axis discriminates — dropping it takes the required item too",
         {**entry, "arms": [{"kinds": ["DOC"], "determination-prefix": ["Not provided"]}]},
         ["DOC-A.1-1", "DOC-A.1-2"]),
        ("the KIND axis discriminates — dropping it takes the syntax rule",
         {**entry, "arms": [{"requirement": ["optional"], "determination-prefix": ["Not provided"]}]},
         ["DOC-A.1-1"]),
        ("…and dropping BOTH takes everything the register says 'Not provided' about",
         {**entry, "arms": [{"determination-prefix": ["Not provided"]}]},
         ["DOC-A.1-1", "DOC-A.1-2", "SR-1.1-1"]),
        ("the DETERMINATION axis discriminates — dropping it takes every optional item",
         {**entry, "arms": [{"kinds": ["DOC"], "requirement": ["optional"]}]},
         ["DOC-A.1-1", "DOC-A.1-3", "DOC-A.1-4"]),
        ("an item with NO §7 row is never selected — a missing determination is not a negative one",
         entry, ["DOC-A.1-1"], {k: v for k, v in register.items() if k != "DOC-A.1-4"}),
        ("a determination that merely CONTAINS the phrase is not one that begins with it",
         entry, [], {**register, "DOC-A.1-1": "**Provided.** The BEFORE ADVANCING form is Not provided."}),
        ("the prefix match is case-insensitive on both sides",
         entry, ["DOC-A.1-1"], {**register, "DOC-A.1-1": "**NOT PROVIDED.** shouted"}),
    ]
    rc = 0
    for case in cases:
        name, raw, want = case[0], case[1], case[2]
        reg = case[3] if len(case) > 3 else register
        got = DerivedSelector("self-test", raw, reg).select(rules)
        if got == want:
            print(f"  ok: {name}")
        else:
            print(f"  SELF-TEST FAILED: {name} -> selected {got}, wanted {want}")
            rc = 1

    # ── kb/Work R43 (2026-09-24): the LEAD-IN axis and the NOTE cut. Neither can be falsified against the live
    # catalog by the drift test alone — the lead-in arm selects the three §9.3.6 exact-match criteria whichever
    # way a nested list is handled, and only one live row carries a NOTE that matters — so both are driven here.
    listed = [
        {"id": "GR-1.1-1", "kind": "GR", "section": "1.1", "sublist": 1,
         "text": "Rule A. For each parameter the following criteria are considered an exact match:"},
        {"id": "GR-1.1-L2.1", "kind": "GR", "section": "1.1", "sublist": 2, "text": "criterion one, with a list:"},
        {"id": "GR-1.1-L3.1", "kind": "GR", "section": "1.1", "sublist": 3, "text": "an inner item"},
        {"id": "GR-1.1-L2.2", "kind": "GR", "section": "1.1", "sublist": 2, "text": "criterion two"},
        {"id": "GR-1.1-2", "kind": "GR", "section": "1.1", "sublist": 1,
         "text": "Rule B names a widget. NOTE 1 Implementors are encouraged to support a gadget."},
    ]
    lead_cases = [
        ("the LEAD-IN axis selects a list's items by the sentence that introduces them",
         {"verdict": "X", "arms": [{"lead-in": "considered an exact match:"}]}, ["GR-1.1-L2.1", "GR-1.1-L2.2"]),
        ("a nested list does not inherit its parent list's lead-in",
         {"verdict": "X", "arms": [{"lead-in": "with a list:"}]}, ["GR-1.1-L3.1"]),
        ("a NOTE cannot EXCLUDE a rule",
         {"verdict": "X", "arms": [{"pattern": "widget"}], "excludes-patterns": ["gadget"]}, ["GR-1.1-2"]),
        ("a NOTE cannot SELECT a rule",
         {"verdict": "X", "arms": [{"pattern": "gadget"}]}, []),
        ("an arm's OWN exclusion does not reach its sibling arm",
         {"verdict": "X", "arms": [{"pattern": "criterion", "excludes-pattern": "two"},
                                   {"lead-in": "considered an exact match:"}]},
         ["GR-1.1-L2.1", "GR-1.1-L2.2"]),
        ("…and it does drop what its own arm took",
         {"verdict": "X", "arms": [{"pattern": "criterion", "excludes-pattern": "two"}]}, ["GR-1.1-L2.1"]),
    ]
    for name, raw, want in lead_cases:
        got = DerivedSelector("self-test", raw, register).select(listed)
        if got == want:
            print(f"  ok: {name}")
        else:
            print(f"  SELF-TEST FAILED: {name} -> selected {got}, wanted {want}")
            rc = 1

    # ── kb/Work PB1536 Q1 / R43 item 3: the CONDITIONALLY-REQUIRED class. Its selector (`a1-condition-absent`)
    # is the optional selector's twin over a different requirement value and a different opening; the live data
    # cannot separate the two axes (every 'Condition absent.' row is conditional), so they are driven here.
    conditional = rules + [
        {"id": "DOC-A.1-5", "kind": "DOC", "section": "A.1", "requirement": "conditionally required",
         "text": "cond, absent"},
        {"id": "DOC-A.1-6", "kind": "DOC", "section": "A.1", "requirement": "conditionally required",
         "text": "cond, present"},
    ]
    cond_register = {**register,
                     "DOC-A.1-5": "**Condition absent.** no non-COBOL program can be called",
                     "DOC-A.1-6": "**Provided.** a .NET host activator is recognized",
                     # a REQUIRED item written 'Condition absent' is a defect in prose, never a derived row
                     "DOC-A.1-2": "**Condition absent.** a required item cannot be conditional"}
    cond_entry = {"verdict": "DOCUMENTED-NON-SUPPORT",
                  "arms": [{"kinds": ["DOC"], "requirement": ["conditionally required"],
                            "determination-prefix": ["Condition absent"]}]}
    cond_cases = [
        ("control: a CONDITIONAL item §7 records as 'Condition absent.' is selected", cond_entry, ["DOC-A.1-5"]),
        ("the REQUIREMENT axis keeps the REQUIRED item out",
         {**cond_entry, "arms": [{"kinds": ["DOC"], "determination-prefix": ["Condition absent"]}]},
         ["DOC-A.1-2", "DOC-A.1-5"]),
        ("the DETERMINATION axis keeps the conditional item whose condition HOLDS out",
         {**cond_entry, "arms": [{"kinds": ["DOC"], "requirement": ["conditionally required"]}]},
         ["DOC-A.1-5", "DOC-A.1-6"]),
        ("the optional selector's opening does not reach a conditional item",
         entry, ["DOC-A.1-1"]),
    ]
    for name, raw, want in cond_cases:
        got = DerivedSelector("self-test", raw, cond_register).select(conditional)
        if got == want:
            print(f"  ok: {name}")
        else:
            print(f"  SELF-TEST FAILED: {name} -> selected {got}, wanted {want}")
            rc = 1

    # An arm with no positive field selects the entire catalog; the constructor refuses it. Driven because the
    # guard's own field list grew by two here, and a guard that forgot one would be silently permissive.
    try:
        DerivedSelector("self-test", {"verdict": "X", "arms": [{"excludes-kinds": ["FMT"]}]}, register)
        print("  SELF-TEST FAILED: an arm with only negative fields was accepted")
        rc = 1
    except SystemExit:
        print("  ok: an arm with only negative fields is refused")

    print("=== derive_verdict_batch --self-test: "
          + ("ALL GREEN (every axis proven able to fail)" if not rc else "FAILED") + " ===")
    return rc


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("selectors", nargs="*", help="derived-verdicts entry name(s)")
    ap.add_argument("--all", action="store_true", help="every selector in the schema")
    ap.add_argument("--list", action="store_true", help="name the selectors and their measured populations")
    ap.add_argument("--self-test", action="store_true",
                    help="prove every selector AXIS can fail, against fabricated data")
    ap.add_argument("-o", "--out", type=pathlib.Path, help="write the batch file here")
    ap.add_argument("--notes", default="", help="notes text for every record (default: the selector's decision)")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    if args.self_test:
        return self_test()

    schema = load_schema()
    rules = load_catalog()
    inventory = {r["rule-id"]: r for r in load_inventory()}

    if args.list:
        for name, sel in schema.derived.items():
            ids = sel.select(rules)
            held = sum(1 for i in ids if (inventory.get(i, {}).get("verdict") or "") == sel.verdict)
            print(f"  {name:<28s} {len(ids):5d} rules  ({len(ids) - held} to stamp, {held} already at the verdict)"
                  f"  -> {sel.verdict}")
        return 0

    names = list(schema.derived) if args.all else args.selectors
    if not names:
        ap.error("name at least one selector, or pass --all / --list")
    if unknown := [n for n in names if n not in schema.derived]:
        sys.exit(f"unknown selector(s) {unknown} — known: {sorted(schema.derived)}")

    records: list[dict[str, str]] = []
    conflicts: list[str] = []
    orphans: list[str] = []
    held: Counter[str] = Counter()
    emitted: Counter[str] = Counter()
    anchored: Counter[str] = Counter()
    seen: dict[str, str] = {}

    # Select ONCE per selector. Each pass is 4,311 rules × the arms' regexes, so re-running it inside the record
    # loop turns a two-second script into a quadratic one — measured, on the first run of this file.
    selected = {name: schema.derived[name].select(rules) for name in names}

    for name in names:
        sel = schema.derived[name]
        for rid in selected[name]:
            row = inventory.get(rid)
            if row is None:
                # The catalog and the inventory disagree about what rules exist. That is a rebuild, not a batch.
                orphans.append(f"{rid} (selected by {name})")
                continue
            if rid in seen and seen[rid] != name:
                conflicts.append(f"{rid}: selected by BOTH {seen[rid]} and {name} — two determinations, one row")
                continue
            seen[rid] = name
            had = row.get("verdict") or ""
            if had and had != sel.verdict:
                conflicts.append(f"{rid}: already {had} — a derived selector must not overwrite an adjudicated "
                                 f"verdict ({name})")
                continue
            if had == sel.verdict:
                held[name] += 1
            emitted[name] += 1
            # ⛔ THE REGISTER ANCHOR IS ADDED, AND IT IS THE ONE FIELD THAT MAY BE — because it is COMPUTED from
            # the row's own rule-id rather than chosen (`kinds.<kind>.anchor-template`). A row whose verdict
            # OBLIGES the anchor and does not carry it is refused outright by record_verdicts.py, so a generator
            # that only carried fields forward could not emit a legal record for it at all; and typing the
            # anchor into a batch by hand is precisely the mis-filing kb/Work A11 recorded. Which rows are
            # obliged is the schema's call, not this script's: an item an A.4 module WITHDREW has no §7 row and
            # is exempt, an OPTIONAL element §7 records as "Not provided." has one and is not (PB280 Q1).
            locations = schema.split(row.get("code-location", "") or "", schema.code_location_sep)
            probe = {**row, "verdict": sel.verdict}
            if (anchor := schema.anchor_for(probe)) and schema.anchor_obliged(probe) and anchor not in locations:
                locations.append(anchor)
                anchored[name] += 1
            records.append({
                "rule-id": rid,
                "verdict": sel.verdict,
                # ⛔ EVERY OTHER FIELD IS CARRIED FORWARD, NEVER SYNTHESISED. A batch record REPLACES every
                # STATEMENT field (`editions`, `notes`, `derivation`), so a generator that filled only `verdict`
                # would silently blank them. The two WITNESS fields MERGE since kb/Work PB959 (record_verdicts.
                # `merged`), so carrying them forward is a no-op union — kept so the record reads whole, and
                # because a selector that CHANGED a verdict would re-site `code-location` to exactly what it
                # names here. Two of these are worth stating outright:
                #  · `notes` — the eleven rows already at this verdict carry per-ROW forensic prose written by
                #    hand ("the DISPLAY Format-2 omitted-COLUMN default …"). The selector's `decision` string is
                #    strictly weaker than that, so an existing note WINS and the decision only fills a blank.
                #    Overwriting them would have traded eleven adjudications for one boilerplate sentence.
                #  · `editions` — the row's own band, NEVER defaulted to all four. FORMAT and SELECT WHEN are
                #    2002-and-later, so "85,2002,2014,2023" on an A.4.8 row would assert that a rule which does not
                #    exist at COBOL-85 is DECLINED there. The band is decided from the standard on every row
                #    (kb/Work PB3173), so a derived verdict restates it and never re-decides it.
                "code-location": schema.code_location_sep.join(locations),
                "test-ref": row.get("test-ref", ""),
                #  · `derivation` — carried for the same reason as `test-ref`: it is what CLOSES a row that no
                #    test can close (kb/Work PB386), so dropping it here would silently re-open every such row
                #    the next time a selector re-stamped its verdict.
                "derivation": row.get("derivation", ""),
                "editions": row.get("editions", ""),
                "notes": row.get("notes", "") or args.notes or sel.decision,
            })

    if orphans:
        print(f"⛔ {len(orphans)} selected rule(s) have NO INVENTORY ROW — rebuild first "
              f"(python scripts/spec/build_inventory.py):")
        for o in orphans[:10]:
            print(f"   {o}")
        return 1
    if conflicts:
        print(f"⛔ {len(conflicts)} CONFLICT(S) — NOTHING was written. A derived verdict claims its rules are "
              f"unreachable; a row that already carries a different verdict is a contradiction to resolve, "
              f"never a row to overwrite:")
        for c in conflicts:
            print(f"   {c}")
        return 1

    for name in names:
        sel = schema.derived[name]
        print(f"  {name:<28s} {emitted[name]:5d} record(s)  "
              f"({held[name]} already at {sel.verdict}, held not changed"
              + (f", {anchored[name]} given their computed register anchor" if anchored[name] else "") + ")")

    would_close = sum(1 for r in records
                      if schema.state_for({**inventory[r["rule-id"]], **{k: r[k] for k in r if k != "rule-id"}})
                      == "OK")
    print(f"\nrecords        : {len(records)}")
    print(f"rows already at the derived verdict : {sum(held.values())}  (no-ops, kept for the drift test)")
    print(f"projected GAP delta : -{would_close}   "
          f"({'as expected — a declined row closes on its WITNESS, not on its verdict' if not would_close else
             'unexpected: check the test-ref that closed it'})")

    if not args.out:
        print("\nno --out given; nothing written.")
        return 0
    args.out.parent.mkdir(parents=True, exist_ok=True)
    payload = {"batch": "derived:" + "+".join(names), "records": records}
    args.out.write_text(json.dumps(payload, indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
    print(f"\nwrote {args.out}")
    print("⛔ NOW VALIDATE IT:\n    " + RECORD.format(args.out))
    return 0


if __name__ == "__main__":
    sys.exit(main())
