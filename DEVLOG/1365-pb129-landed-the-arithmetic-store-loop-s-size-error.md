## Entry 1365 — 2026-08-29 03:02 PDT — PB129 LANDED: the arithmetic store loop's size-error discipline — per-receiver guards, the remainder's two GR6c halves, and the _outermost leak that made multiplying by one change the value

Four mechanisms from batch 8, one campaign. (1) EmitArith's single statement-wide try let one receiver's
PROHIBITED/overflow raise abandon every receiver to its right, where §14.7.7 r4b proceeds to the next
resultant and §14.7.5 storing rule 2 keeps the others' values — each receiver's compute+store now sits in
its own GuardedStore try (the outer try keeps only the initial evaluation's raises; the unchecked fast path
is byte-identical). (2) DIVIDE's REMAINDER stored unconditionally after the quotient's size error and was
formed from the UNCAPPED subsidiary quotient — both GR6c halves now hold: identifier-4 stores only when the
condition is not raised, and the remainder's quotient is capped at identifier-3's digit count
(CobolNum.CapDigits — the first probe put the cap on the quotient STORE too, which silenced the quotient's
own overflow; the cap now feeds the back-multiply only). The back-multiply rides Combine — checked, and
Dec-lane-aware — instead of a raw Int128 splice. (3) NumericRenderer's _outermost flag leaked through
BoundNegate/BoundPower/BoundComputedOperand — `(1/3) ** 0` under PROHIBITED reinstated the exact pre-CA5
spurious raise, and `(A/B) ** 2` ≠ `1 * (A/B) ** 2`; all three recursions now clear it. Goldens
pb129_store_discipline + pb129_remainder_no_phrase pin every disposition from both sides (phrase and
no-phrase). GR-14.9.12.4-4/-5/-6 → CONFORMS, GR-8.8.1.2-1 → CONFORMS.

