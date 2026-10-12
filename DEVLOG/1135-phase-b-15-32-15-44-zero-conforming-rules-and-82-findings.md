## Entry 1135 - 2026-07-30 18:21 PDT - Phase-B 15.32-15.44: zero conforming rules, and 82 findings that are seven defects

The next contiguous block of the traceability review — EXCEPTION-STATEMENT, EXCEPTION-STATUS, EXP, EXP10,
FACTORIAL, FIND-STRING, the four FORMATTED-* functions, FRACTION-PART, HIGHEST-ALGEBRAIC, INTEGER. 67 rules, one
adjudicating agent and one INDEPENDENT refuting agent per function, 26 agents, no failures.

**Not one rule came back CONFORMS.** 31 DIVERGES, 22 PARTIAL, 14 NOT-IMPLEMENTED. The design doc warns that an
all-CONFORMS report is a red flag; this is the opposite extreme and it is worth saying plainly what it means and
what it does not. It does NOT mean these functions are broken end to end — most of them compute correct values
for ordinary arguments, which is exactly why the defects survived. It means the ARGUMENT RULES and FORMAT RULES
around them are largely unimplemented: the functions work and their guardrails do not.

**GAP did not move, and that is correct.** 3806 before, 3806 after. Adjudicating a clause that DIVERGES does not
close its rows — fixing it does. A batch that moved the burn-down by adjudicating would be measuring the wrong
thing, and it is worth having seen the number stand still to know the mechanism is honest.

**82 findings, seven root causes.** The playbook's "cluster before triaging" earned its place again — batch 1's
42 rows were three causes for 31 of them, batch 2's 41 were 34 of one. Here the largest column, 19 findings
across the four FORMATTED-* functions, is ONE missing recognizer: `CobolDate.Tokenize` checks per-character
classes and per-field widths and never asks "is this string one of the formats §15.5 defines". Fixing that
retires nineteen findings. The second largest, 18 findings over 10 subjects, is one grammar fact: `functionCall`
is reachable from exactly FOUR places, so every other identifier-N SENDING position rejects a function-identifier
that §8.4.3.1.2 Format 1 plainly makes legal there. That is PB10 and it is the red line.

**Two clusters are residues of fixes that landed TODAY**, which is a useful check on how complete "landed" was:
PB12 is PB1's designed residue (the argument-class table only screens functions with a row, and these have none —
though two findings show a row is not sufficient, because the class lattice cannot express two of §8.5.2.1
Table 2's classes and the screen is structurally unreachable for every phrase-keyword intrinsic), and PB14 is the
PB2 shape on the Dec axis where PB2 fixed only the Real arm of the same dispatch. Neither was a regression; both
were the part of the problem the earlier fix did not claim.

**The recording mechanism refused a bad merge, which is the second time today a guard paid off before a test
ran.** `record_verdicts.py` rejected the first attempt: 10 test-refs were raw file paths (`tests/…/X.cs#Method`)
instead of the schema's `<kind>:<ref>` form. All-or-nothing, nothing written — so the inventory did not end up
half-adjudicated with the landed rows indistinguishable from reviewed work. Normalised and re-merged.

**Evidence persisted rather than summarised.** All 82 findings are in
`docs/rearchitecture/evidence/PHASE-B-15.32-15.44-findings.md` verbatim. The workflow's temp directory does not
survive the session, and a cluster is a plan, not a repro — the queue entry tells you what to fix, the ledger
tells you how to see it fail.

