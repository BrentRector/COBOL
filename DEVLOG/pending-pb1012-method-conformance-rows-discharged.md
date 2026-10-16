## PB1012 discharged: the §9.3.8.2.3 rows were already closed, and "the same type" for rule 7 is §8.5.3.1 equivalence

Wave 1091 group F. PB1012 was filed on 2026-09-22 as an analysis note. It said the §9.3.8.2.3 rows GR-9.3.8.2.3-1 to -7 were GAP and needed a golden and verdict round, and that rule 7 ("If either of the corresponding formal parameters or returning items in interface-1 or interface-2 is a strongly-typed group item, both are of the same type", cite OK §9.3.8.2.3 7)) first needed an adjudication, because "an interface cannot declare a TYPEDEF".

**Re-probe.** On this branch's build of 2516379e2, all seven rows were already CONFORMS/OK. Rule 1 was closed by PB1051, rule 2 by PB1497, rules 3 and 6 by PB1166, and rule 5 (with the L2.1 to L2.3 rows PB55 folded into it) by PB1497/PB1499. Rules 4 and 7 had picked up spec-derived witnesses in golden-lane-1 wave 1 (3031e2e90), but their notes still said "test-ref empty". Every existing negative witness was rerun and still refused with its rule-cited message.

**Rule 7 needed no adjudication.** §10.6.2 SR4 e) (cite OK: "The data division may contain only a linkage section") gives a method prototype a linkage section, and a `TYPEDEF STRONG` entry is legal there. "The same type" across two source elements is §8.5.3.1 type-declaration equivalence (cite OK: "Two type declarations are considered equivalent when they have the same type-name, both have the same presence or absence of the EXTERNAL clause and the STRONG phrase"), and that is what `OoConformance.StrongTypeMismatch` implements through `StrongTypeModel.SameType`. The standard decides the question, so docs/CONFORMANCE.md needs no entry.

**Sibling sweep.** `MethodConformanceMismatches` is the one rule set, and it has four askers: object IMPLEMENTS (§11.8.3 SR2), FACTORY IMPLEMENTS (§11.4.3 SR2), the override (§11.7.3 SR9) and an interface that inherits one method-name from two interfaces (§11.6.3 SR5). Each was probed on rule 7 and each refused the mismatched pair: STRONG on one side only in either direction, the same type-name with PIC X(4) vs X(5), and TT vs TU with one layout. Each accepted equivalent declarations. No defect was found.

**Goldens.** `conformance:2002/pb1012_strong_type_override_factory` is the positive: a FACTORY IMPLEMENTS and an override, each with equivalent strong types, an identical PIC 9(3)V99 formal and an identical PIC X(6) RETURNING item. It compiles and runs. `conformance:negative/pb1012-strong-type-override-layout-differs` expects COBOLNET0829 and `conformance:negative/pb1012-strong-type-factory-strong-one-side` expects COBOLNET0841. Until now rule 7 was witnessed only on the object IMPLEMENTS asker.

**Rows.** Batch `w1091f-pb1012` restates rows 4 and 7 with their current witnesses, so their notes no longer say "test-ref empty", and adds the positive golden to rows 3 and 6. GAP stays at 98. PB1012 is `landed` with `closes_rows: []` and a reason that names the landings that had already closed its rows.

No compiler change, no diagnostic codes used (COBOLNET3854 to 3856 returned).

**Self-review.** The diff adds only goldens, manifest rows, a verdict batch and the note. Each golden comment has its citations re-checked with `cite.py --check`. Each golden has a unique PROGRAM-ID and class names, and each expected output is derived from the source. Each negative refuses with exactly one diagnostic, from the rule it names. Semgrep verify: PASS, no counts changed.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261016T131204Z-691b30). The three new cases passed in leg 1. Linux gate: the lander's (batched gating).
