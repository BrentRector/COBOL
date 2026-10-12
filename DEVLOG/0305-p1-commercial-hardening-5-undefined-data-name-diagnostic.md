## Entry 305 — P1 commercial-hardening #5: undefined data-name diagnostic (CBL3128), strict-gated + Default-flip dry-run

Second P1 item: close (in strict mode for now) the assessment's #1 commercial gap — `MOVE 5 TO
NONEXISTENT-ITEM.` compiles clean and exits 0 today, because the binder demotes any unresolved name to an
alphanumeric literal (`ExpressionBinder`) and lowering silently drops a literal target
(`DataMovementLowerer`). The fix is ONE centralized pass, not the ~66 scattered binder `return null` sites
(project doctrine: fix the pattern, not the instance).

**Design.** Extend the existing Pass-2 `ReferenceResolver` (it already validates PERFORM/GO TO/file refs).
An `_inProcedureDivision` flag (set in `VisitProcedureDivision`) scopes the check to PROCEDURE DIVISION
operands; `VisitDataReference` checks the BASE name. The grammar's `dataReference` is
`LINAGE_COUNTER ((OF|IN) cobolWord)? | cobolWord dataReferenceSuffix*`, so `ctx.cobolWord()` is the single
base (OF/IN qualifiers are nested `qualification` nodes → never independently flagged) and LINAGE_COUNTER is
skipped as a special register. A name counts as "defined" if untyped `Scope.Resolve` finds it in the
data/global/procedure scope (covers data items, level-88, index-names, file connectors, paragraphs) OR it is
a SPECIAL-NAMES name that lives in no Scope — mnemonics + switch ON/OFF conditions + symbolic characters +
CLASS + ALPHABET — threaded in via the new `Compilation.CollectSpecialNames` (these parse as data
references, e.g. `SET sw TO ON`).

**Dialect-gated (owner decision).** CBL3128 fires only under named-strict modes (`>= StrictCobol85`); Default
and `--nist` keep the lenient behavior, so the 349 NIST baselines are unaffected *by construction* — the gate
returns before emit. Staged rollout: prove the corpus clean, then flip Default→error in a follow-up.

**Default-flip dry-run (the key evidence).** Compiled all 349 baselined NIST programs in StrictCobol85 and
collected CBL3128 (temporary harness, removed before commit). Result: **348/349 clean**; exactly ONE program
(IC228A) flags 4 names (GLO-DATA-1..4). That is a true FALSE POSITIVE, not a test bug: those are `IS GLOBAL`
items declared in a CONTAINING program and inherited by the contained one (DEVLOG 236). `InheritGlobalItems`
runs in `Compile` AFTER `BuildSemanticModel` (which contains ReferenceResolver), so at resolution time the
contained program's scopes don't yet hold the ancestor globals. ⇒ **The single blocker for a Default-flip is
the GLOBAL-inheritance ordering**: the flip follow-up must make the undefined-name check aware of inherited
globals (thread ancestor GLOBAL names in, or run the check as a post-inheritance pass). Until then the gate
keeps it harmless. This is exactly what the staged dry-run is for — it converted "is the flip safe?" into a
single concrete, scoped fix.

**Known limitation (documented, not a regression).** Subscript/index operands inside `(...)` are
SUBSCRIPT-mode tokens (`SUB_IDENTIFIER`), not `dataReference` nodes, so an undefined *subscript* variable is
not yet caught by this pass — a separate enhancement, lower priority than the headline MOVE/DISPLAY/IF case.

**Verify:** build 0 warnings; +6 unit (`UndefinedDataNameTests`: strict MOVE/DISPLAY to undefined → CBL3128;
Default → none; valid / qualified-base / duplicate-subordinate-name → none). Full guard **ALL GREEN — 1009
unit (1003 +6) / 347 integration / 349 NIST baselines, 0 FAIL\***.

