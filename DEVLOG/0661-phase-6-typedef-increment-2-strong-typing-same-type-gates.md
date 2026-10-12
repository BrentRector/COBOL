## Entry 661 — 2026-07-07 13:37 PDT — Phase 6: TYPEDEF increment 2 — STRONG typing (same-type gates + declaration SRs)

Increment 2 of TYPEDEF / the TYPE clause (ISO/IEC 1989:2023 §13.18.58.2 / §13.18.57.3 / §8.5.3.3, COBOL-2002;
data-model **D17**): a STRONG type declaration now enforces its data-integrity restrictions at compile time. Fully
BINDER-ONLY (no grammar, no legacy-guard slice — the greenfield battery suffices); implemented from the D17 design +
the spec sections read verbatim first (rule #1).

**Spec.** A STRONG type is stored IDENTICALLY to a weak one and adds only compile-time checks (§8.5.3.3). Per
§8.5.3.1 an item is strongly typed if it references a STRONG type OR is subordinate to a strongly-typed group; per
§8.5.3.3 ONLY GROUP items are ever strongly typed (an elementary leaf under a strong group is not — its individual
MOVE/compare is unrestricted; a strong record is built up field by field). Two items are the same type (§8.5.3) when
their type declarations are equivalent (within one source element: identically-named) and each occupies the identical
relative position. Gates: MOVE **§14.9.25.3 SR2** (a strong-group RECEIVER wants a same-type group SENDER — Table 16's
Type-name column), comparison **§8.8.4.2.3 SR1** (either operand strong ⇒ both same type), class condition
**§8.8.4.4.3 SR1** (a strong group has its own unique class/category, may not appear), declaration **§13.18.57.3**
SR3 (no RENAMES of a strong item in whole/part) / SR4 (no REDEFINES) / SR6 (a strong TYPE ref only at level 1 or
subordinate to a strong group). ⚠ The D17 design mis-cited the MOVE rule as "§8.8.4.1.1 item 12" (that list is the
COMPARISON permitted-set) — corrected in the deep-dive this change set (rule #4).

**Implementation (all binder).** `DataItem` gains `StrongRoot` (walk to the outermost `StrongType` ancestor),
`IsStrongGroup` (a group with a strong root — the restricted operand form), `IsStronglyTyped` (any node in a strong
subtree — the "in whole or in part" test), and the static `SameStrongType(a,b)` = equal strong-root `TypeName` +
equal relative member-name (`CsName`) path (both operands are clones of one template, so corresponding items share a
path). USE gates → **COBOLNET1533**: `CheckStrongMove` in `BindMove` (SR2); the strong-group same-type check folded
into the ONE `CheckedRelational` chokepoint (SR1 — so it covers IF / EVALUATE pairings+ranges / PERFORM UNTIL /
SEARCH WHEN in one place, feedback_singular_pattern); a strong-group guard atop `CheckClassConditionOperand` (SR1,
shared by the relation-path and EVALUATE-path class conditions). DECL gates → **COBOLNET1532**: SR6 at clone time in
`ExpandType` (level-1-or-under-a-strong-parent — a nested strong ref already sees its strong ancestor set, ExpandType
having run on the outer subject before CloneItem recursed; also catches SR7's 77→group); SR3/SR4 in a new
post-resolution `CheckStrongTypeDeclarations` pass (after ResolveRedefines/ClassifyRedefinesClasses so the graph is
resolved) — a REDEFINES target or a RENAMES span that is `IsStronglyTyped`, with INTERNAL template redefines excluded
by the shared-strong-root test (a template's own REDEFINES clones legitimately).

**Verified by RUNNING (standing lesson — a binder gate can silently over/under-fire).** Every gate exercised on the
CLI at `--std 2002` before encoding: the golden runs (`P2=[010][020]` / `EQUAL` / `UNEQUAL2`), and each of 7 violation
programs emits exactly its intended 1532/1533 — including the relative-path case (`MOVE GA OF R1 TO GB OF R2`, same
type PAIR-T but different positions → 1533) and the positive same-position subgroup MOVE/compare (clean). Caught a
malformed fixture in passing (a TYPEDEF wrongly nested at level 05 tripped 1529 before the intended SR3) — fixed the
test, not the compiler.

**Tests + goldens.** Golden `typedef_strong_ok` (2002 corpus, GreenfieldOnly — the frozen legacy has no TYPEDEF
model; byte-verifies same-type whole-record MOVE + two `IF group = group` compares). `TypedefStrongTests` ×8:
MoveDifferentType / CompareDifferentType / ClassConditionOnStrongGroup (1533), StrongRefInOrdinaryGroup / Redefines /
Renames (1532), MoveDifferentSubgroupPosition (1533 — the relative-path half), + a same-type/same-position/field-op
CompileClean companion (no over-restriction).

**Battery: 2012 conformance (+9) · 213 unit GREEN.** No grammar change ⇒ no legacy guard; `typedef_strong_ok` joins
the legacy runner's GreenfieldOnly exclusion (feedback_legacy_suite_on_shared_corpus). Diagnostic band 15xx:
1529/1530 (inc 1) + 1532/1533 (inc 2) used; 1531/1534/1535 reserved for incs 3–4.

**Named follow-ons (LOUD when implemented, never silently wrong):** CALL/INVOKE-argument + interface same-type
conformance (§14.8 :25298 / §12.4 :12395 — a distinct site; the design scopes inc 2 to MOVE/compare/class-cond);
§8.8.4.2.3 SR4 (a strong group with boolean/object/pointer elements admits equality only) — inc-4 residue 1535;
cross-program EXTERNAL type equivalence (§8.5.3 — `SameStrongType` compares by type-name within one element today).
NEXT: TYPEDEF inc 3 (level-88s inside a TYPEDEF), inc 4 (staged-loud residue + matrix rows), then the adversarial
find→verify review over incs 1–4.

