## Entry 1160 — 2026-08-03 21:41 PDT — PB29: a guard keyed on what a block is CALLED can never find a block called something else — the hole is 100 clauses, not 21

**PB29 asked for the EXTRACTOR to be fixed rather than §8.8.1 to be added by hand, and that instruction is what
made the measurement worth taking.** The entry's own count is exactly right for its subject: §8.8.1's five
rule-bearing clauses carry 7 + 3 + 4 + 3 + 4 = **21** ordinals and the catalog holds none of them. The content-keyed
scan then measured the same blind spot across the whole standard: **100 clauses, 460 ordinals.**

**WHY BOTH EARLIER FIXES MISSED IT, STRUCTURALLY.** The denominator has been short twice — 3,790 → 3,846 (heading
spellings the literal map did not know) → 3,861 (§13.18.40.5/.6, typed by §5.3) — and each time the repair
extended a guard keyed on the heading TEXT. `RULE_SHAPED` reports a title containing the word "rule" that `KINDS`
cannot resolve. §8.8.1.2 is titled *"Native, standard-binary, and standard-decimal arithmetic"*. There is no
spelling of that guard which finds it. So the new one asks the other question — **does this clause's BODY carry an
ascending `N)` list that nothing harvested?** — and that question is answerable for every clause in the standard,
including ones nobody has thought about yet.

**⚠ 460 IS AN UPPER BOUND, AND SAYING SO IS THE POINT.** The standard numbers ordinary prose enumerations exactly
as it numbers rules. §8.3.2.2 "User-defined words" contributes 15 ordinals and they are the KINDS of user-defined
word, not fifteen requirements; §9.3.6 "Method invocation" contributes 23. Against that, §14.7.5 "SIZE ERROR
phrase and size error condition" (16) and §8.8.1.2 (7) are plainly normative — PB28 proved two of §8.8.1.2's are
violated by the compiler today. **Admitting all 460 would be the mirror image of the short denominator**, and this
project has already paid twice for a number that was wrong in one direction. So the scan MEASURES and does not
decide: every clause is committed to `docs/rearchitecture/spec-unharvested-rule-blocks.json` with
`disposition: "pending"`, which is a worklist row and never a verdict.

**THE MANIFEST IS THE MECHANISM, not a report.** Per the verdict-evidence invariant (`DESIGN-test-build-ci.md`
§3.10), the scan compares against a COMMITTED manifest rather than a remembered number, so `--check` fails on
three distinct drifts: a clause newly carrying rules, a clause that stopped, and a count that moved. **Proven by
making it fail** — dropping §8.8.1.2 from the manifest and setting §14.7.5 to 99 were both reported exactly
(`+ §8.8.1.2 (7 ordinals) … NEW, undeclared` and `~ §14.7.5 99 → 16`), then restored. A guard nobody has watched
fail is a guard nobody has tested, and this file has now produced three of those in two sessions.

**⛔ WHAT IS DELIBERATELY NOT DONE, AND WHY IT IS NOT MINE.** No clause was admitted to the denominator. Admitting
one CHANGES THE DENOMINATOR, and the denominator is the definition of v1.0 (D13) — the §13.18.40.5/.6 admission
was taken as an owner decision on precisely that ground, and this is the same decision at 100× the scale. The bare
question is in plan §0 and the fix-queue entry. Until it is taken, §11 **A3** stays blocked exactly as it was: it
would otherwise audit "the intermediate-results model vs §8.8.1 end-to-end" against a denominator that omits
§8.8.1.

**The catalog is byte-unchanged** — this commit adds a detector and a manifest, not rules. The extractor's single
pre-existing parse gap (RV §15.4 "Returned values", recorded in plan §0 as unowned) is unaffected and still the
only one.

