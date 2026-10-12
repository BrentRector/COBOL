## Entry 851 — 2026-07-16 01:30 PDT — P9 Step 11 — the §13.18.2 ANY LENGTH clause, all three unit-kind legs (method / contained program / function)

**The clause (ISO §13.18.2):** an elementary level-1 LINKAGE entry whose length varies at RUNTIME with the
corresponding argument (GR1). Landed end-to-end on the BASED wiring pattern: **grammar** — `anyLengthClause : ANY
LENGTH` (UNGATED superset, both tokens pre-existing) added to the `dataDescriptionClause` alternatives; **edition
gate** — registry row `any-length-clause-2002` (§13.18.2, introduced 2002, 0900) + the parse-arm
`VisitAnyLengthClause` (recognition — the flag is cleared on SR violations, so a bound-arm gate would drop the 0900);
**binder** — `DataItem.IsAnyLength`; `BindEntry` decodes + validates SR1 (PICTURE exactly one X/N/1, checked on the
WRITTEN character-string), §13.16.3 SR17 (no other clauses beyond PICTURE/USAGE/VALUE), and the level-1 half of SR2 —
all on NEW code **COBOLNET1542** (the prompt-suggested 0850 is TAKEN by the ODO band; 08xx is exhausted, the 1521
15xx precedent); the SR2/SR3/SR4 placement sweeps live where the facts live — `AnyLengthValidateUnit` at the end of
`CallBindLinkage` (linkage-only / elementary / outermost-program + object-paragraph rejection via the new
`DataBinder.UnitIsContained`/`UnitIsFunction` init flags / formal-or-RETURNING reference) and the `OoBindMethodData`
tail (property-method `Accessor != '\0'` + formal/RETURNING). Related SRs: §13.18.44.3 SR16 (REDEFINES of an ANY
LENGTH target), §14.9.4.3 SR11/SR18 (a CALL argument/RETURNING shall not be ANY LENGTH — CALL has no prototype to
pair it with; INVOKE does). **Conformance** — `DescriptionMismatch` gains the ANY LENGTH row of the §14.8.2/§9.3.8.2
tables: PAIR mode (override/implements/universal descriptor) demands the clause MATCH and voids the length compare
when both carry it; ACTIVATION mode (`anyLengthActivationRelax` — INVOKE args :25375d/e, BY CONTENT :25414c,
RETURNING delivery §14.8.3.3 r4/5) relaxes ONLY the length check when the FORMAL/sender side is the ANY LENGTH one;
`ConformanceDescriptor` encodes `S:*` (a universal crossing against an ANY LENGTH formal raises EC-OO-UNIVERSAL —
exactly the :28530 ban). **Emit/runtime** — FUNCTION LENGTH binds the RUNTIME `.Length` channel (never the fold);
MOVE/UNSTRING receivers store at the carrier's current length (`ConvertSource(runtimeWidth:)`); the INVOKE marshaling
skips formal-width normalization for ANY LENGTH formals; the contained-program/function leg passes the **width −1
full-string sentinel** to `CobolArgAdapt.Text` (new GR1 mode: reads = the caller's whole string, zero-length = GR1a;
writes re-fit at the argument's current length = GR1b). **Staged loud (recorded deferral):** ANY LENGTH on a
RETURNING item (SR3b-legal) — the return-value crossing cannot carry the ACTIVATOR's receiver length that GR1 fixes
n from; catalog descriptor `any-length-returning` (0899 family), deferred to its own wave. Goldens: `oo_any_length`
(method), `any_length_contained` (CALL), `any_length_function` (UDF) — each proves LEN=03→LEN=08 tracking; negatives
`any-length-at-85` (0900) / `any-length-bad-picture` / `any-length-outermost` (1542); all three goldens
GreenfieldOnly-excluded from the frozen legacy runner. Battery (targeted): matrix+drift 1569 ✓ · corpus 176 ✓ · Oo
211 ✓ · unit 281 ✓ · characterization 33 byte-exact ✓ · legacy conformance 113 ✓.

