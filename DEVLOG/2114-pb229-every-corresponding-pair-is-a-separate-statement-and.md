## Entry 2114 — 2026-10-16 14:52 PDT — PB229: every CORRESPONDING pair is a separate statement, and an item under a FILLER group corresponds

kb/Work PB229 was filed as an evidence gap on `GR-14.9.2.4-5` (ADD CORRESPONDING's "the results are the same as if
the user had referred to each pair of corresponding identifiers in separate ADD statements"). Re-probed on the
worktree's own build, two of its three residues were live wrong answers.

**One try for every pair.** `CorrespondingEmitter.Emit` wrote its own combine-and-store per pair inside the
statement's single outer try (`ArithmeticEmitter.EmitArith`), so a size error raised while one pair's sum was
EVALUATED abandoned every later pair. Measured with a FLOAT-LONG pair at 1E308 (the binary64 sum raises
EC-SIZE-OVERFLOW) followed by a `PIC 9(3)` pair 5 + 10: `SE` and `B=010`; §14.7.6 runs the SIZE ERROR imperative
"after all of the implied statements are completed", so `B=015`. The fix gives the in-place store one home,
`ArithmeticEmitter.StoreInPlace` (receiver ← receiver op value, inside its own `GuardedStore`, outermost final
transfer), which `EmitInPlace` asks per receiver and the CORRESPONDING emitter now asks per pair. The private copy
also lacked `outermost: true`, so a pair's final transfer was not the one the explicit ADD makes. Sweep: every other
`StoreArith` inside `EmitArith` was already guarded, so this was the one unguarded arm.

**The FILLER subtree.** `CorrMembers` cut off every FILLER group's whole subtree, justified in its own comment by
"the legacy/NIST-proven reading". The rule says otherwise: §14.7.6 rule 1 excludes the item DESCRIBED with FILLER
and compares "the same qualifiers"; §8.4.2.2.1 makes qualifiers superordinate NAMES; a FILLER group has none
(§13.18.20.4); and rule 5 names the groups that cut their subordinates off (OCCURS or REDEFINES). So an item under a
FILLER group corresponds with a namesake beside it on the other side, for MOVE, ADD and SUBTRACT CORRESPONDING
(docs/CONFORMANCE.md §3 D-CORRFILLER). `CorrMembers` now walks through a FILLER group that rule 5 does not exclude
and returns each member with its steps (`CorrMember`), so the pair's storage path still names the FILLER member.
PB1111's rule-6 count already read FILLER subordinates this way; the two agree now. CorrEligible's doc comment,
stranded above CorrMembers as a second stacked summary, is back on CorrEligible, and the D-CORR66 references that
said "CONFORMANCE.md §7" now say §3, where the entry is.

**Goldens**, each value derived from the rules: `conformance:85/pb229_corresponding_filler_group_rule4` (rule 4
OCCURS and REDEFINES on either side, rule 1 through one and two FILLER levels and under a named group, rule 5 through
FILLER OCCURS and FILLER REDEFINES groups, over ADD, SUBTRACT and MOVE) and
`conformance:2002/pb229_corresponding_size_error_per_pair` (with the phrase: one `SE`, erring receivers unchanged,
the clean pair stored; without it under EC-SIZE checking: one declarative after all pairs naming the LAST erring
pair's condition, EC-SIZE-TRUNCATION for pairs A B C and EC-SIZE-OVERFLOW for C B A; SUBTRACT the same).

**Rows.** `GR-14.9.2.4-5` PARTIAL → CONFORMS (GAP 71 → 70); witnesses on GR-14.7.6-1/4/5 and GR-14.9.44.4-5, and
GR-14.7.6-1's statement restated. Self-review: the rule now lives in one place per question (the in-place store,
the member walk, the rule-5 predicate shared by the FILLER walk and the THROUGH-alias window); no allocation on a
hot path beyond the bind-time step arrays.

