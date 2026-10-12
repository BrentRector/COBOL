## Entry 1335 — 2026-08-19 14:39 PDT — PB111 LANDED: a CLASS-ID's OBJECT-COMPUTER clauses (PROGRAM COLLATING SEQUENCE, CHARACTER CLASSIFICATION) were a Roslyn CS0103 on the emitted class — `ObjectComputerEmit` is the ONE renderer of a runtime-module type's OBJECT-COMPUTER members (program, instance and factory classes); a method resolves its classification as an activation LOCAL

**Found by flipping the axis the goldens held fixed.** Every T5 golden put `CHARACTER CLASSIFICATION` on a PROGRAM. A
CLASS-ID with the same clause (`scratchpad/probe-oo-cls.cob`: a FACTORY method `MOVE FUNCTION UPPER-CASE("i") TO R`
under `CHARACTER CLASSIFICATION IS TR`) failed in the BACKEND — `error CS0103: The name '__CLASSIFY' does not exist in
the current context` — and the sibling probe with `PROGRAM COLLATING SEQUENCE IS REV` failed the same way on
`__COLLATE`, which dates from PB101: `ProgramEmitter.EmitProgramClass` declared the three OBJECT-COMPUTER members
inline for a program class, `OoEmitter.EmitTypeHalf` declared none, and the renderers (`EmitCore.CollateArg`,
`IntrinsicRenderer`, `ConditionRenderer`) reference the names whenever the unit's DataBinder carries the clause —
which a class's does (§11.3: its ENVIRONMENT DIVISION applies to its methods). Two emitters, one contract written
down once.

**The fix is the shape, not the patch.** `CodeGen/Roslyn/ObjectComputerEmit.cs` — `EmitMembers(data, w,
classificationField)` is THE renderer of a type's OBJECT-COMPUTER members, called by the program emitter and by
`EmitTypeHalf` for BOTH class halves: `__COLLATE` / `__COLLATE_NAT` are per-type constants everywhere; the
`__CLASSIFY` FIELD exists for a program type only, because a program activation has its own instance (a RECURSIVE
program and every function get a fresh instance per activation — `ProgramTable`), so the field assigned in
`__Activate`'s prologue (`ClassificationPrologue`) IS per activation — while a METHOD is re-entered on the SAME
object, so each method body resolves its own activation LOCAL (`ClassificationLocal`: `var __CLASSIFY = …` at the
top of the body, captured by the method's `__MDispatch` local function; an inner invocation's resolution never leaks
into the outer's — §12.3.6.4 GR8 / §14.6.6 r2, a method being a runtime element). The generated-source `using
CobolNet.Runtime.Globalization` header counts class units too. `DispatchEmitter`'s prologue and `ProgramEmitter`'s
field block now call the helper — one spelling of the resolve call.

**Tests.** Golden `2023/pb111_class_object_computer`: CLS-UP = 305 (U+0130 — a factory method under the class's
Turkish classification), CLS-REL = GT (an object method's `"A" < "B"` under the class's reversed PCS) while the
invoking program's PRG-REL = LT natively — the class's sequence governs the method's comparisons and not the
caller's. kb/Work PB111 (landed). Gates: wave-local GREEN (filter `~CorpusRunner&~pb111|~Oo|~Locale|~Collat|~Program|~Class`: Conformance 2120/2120 · full Unit 4471/4471 · Char 33/33); the inventory rows GR-12.3.6.4-8 / -11 and GR-14.6.6-2 carry the class golden; battery #26 owes this tree.

