## Entry 1024 — 2026-07-26 16:14 PDT — PHASE A LANDED: the spec has 3,210 normative rules. P14 finally has a denominator

`session-probe` has said `invent: not built yet` for every session of PHASE 14. That line is not cosmetic — without
an enumeration of the normative rules there is no denominator, so no conformance percentage, no GAP count, and no
burn-down. Every "we are close" statement about conformance has been unfalsifiable. That ends here.

**The spec transcription is regular enough to parse directly.** Rule blocks are anchored, numbered headings
(`## 8.4.3.13.3 Syntax rules`) followed by ordinals, so `scripts/spec/extract_rule_catalog.py` walks the 53,187
lines and emits `docs/rearchitecture/spec-rule-catalog.json` — one entry per rule with its id, section, kind,
ordinal, subject (the nearest non-rule-block ancestor section, i.e. the construct being ruled), printed page, and
text.

**THE DENOMINATOR IS 3,210 NORMATIVE RULES** across 465 rule blocks:

    1331  SR  syntax rules          §7   221     §12   264
    1528  GR  general rules         §8   250     §13   875
     179  AR  argument rules        §10   14     §14  1134
     172  RV  returned value rules  §11   93     §15   351

§13 (data division clauses) and §14 (procedure division statements) are two thirds of the standard between them,
which is where the P14 effort will actually go.

**The completeness self-check earned its place immediately.** The design doc warns that an omitted rule is silent
false confidence — the catalog would report a denominator smaller than reality and every later percentage would be
wrong in the flattering direction. So the extractor asserts that every rule-block heading yields at least one rule
and reports any that yield zero as a PARSE GAP rather than dropping it silently.

The first run reported **2,974 rules and 11 gaps**. Four were legitimate — §5.3.2-5.3.5 are the CONVENTIONS clause,
prose *defining* what a syntax/general/argument/returned-value rule is, correctly containing no rules. The other
seven were a real bug: **the transcription numbers rules BOTH `1)` and `1.`**, and my ordinal pattern matched only
the parenthesis form. §8.8.3.2, §11.9.11.2, §13.18.24.3 and §14.9.30.3 were being read as empty.

Fixing the pattern recovered **236 rules — 2,974 → 3,210, a 7.9% undercount.** Had the check not existed, the
project would have carried a denominator that was quietly short by a twelfth, and every conformance percentage
computed from it would have been optimistic by exactly that much, permanently and invisibly. This is the same
lesson as Entry 1023's dead Semgrep rules, arriving twice in one session from opposite directions: **a clean
result is not evidence; only a check that can fail is evidence.**

`--check` now exits non-zero on any unexpected gap, so it can be wired as a drift test when the spec submodule
moves.

**What this does NOT yet do.** This is Phase A only — the denominator. Phase B (map each rule to its implementing
code and assign a spec-verified verdict) and Phase C (close every DIVERGES / NOT-IMPLEMENTED / untested-CONFORMS)
are the actual work, and Phase B is the agent-parallel one: 3,210 rules fanned out by clause. The traceability
inventory that `session-probe` looks for does not exist until Phase B starts populating it. But the number that
every later measurement divides by is now real, checked, and in the repo.

