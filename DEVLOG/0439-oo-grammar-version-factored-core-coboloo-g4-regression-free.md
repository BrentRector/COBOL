## Entry 439 — OO grammar: version-factored (Core/CobolOO.g4) + regression-free; root cause of the 438 regression found

Per the owner directive **"keep the grammar files factored appropriately for spec versions"** ([[feedback_grammar_version_factoring]]),
redid the OO slice-1 grammar — this time as a version-factored fragment, added **INCREMENTALLY with guard-fast after
each hook** (the DEVLOG-438 §6.5 discipline).

- **New `Core/CobolOO.g4` fragment** (imported into `CobolParserCore`) holds ALL OO rule BODIES: classDefinition /
  classIdParagraph / objectParagraph / methodDefinition / endClassHeader / invokeStatement (+ invokeTarget /
  invokeMethodName / invokeUsing / invokeArgument / invokeReturning) / objectReferenceUsage. `INVOKE` moved here OUT
  of the JSON/XML (2014) fragment where it had been mis-filed.
- **Minimal `{is2002()}?`-GATED hooks** in the core rules: `compilationGroup += classDefinition`; `usageKeyword +=
  objectReferenceUsage`; the `statement` invokeStatement hook already existed. + an `OBJECT` lexer token (corpus-clean).

**ROOT CAUSE of the 438 regression FOUND + FIXED:** the earlier hooks were **UNGATED** (the OO alternatives were
viable for COBOL-85), so the top-level `(programUnit | classDefinition)` prediction and the **optional-tail**
`OBJECT REFERENCE className?` conflicted on ordinary '85 programs (NC174A/211A/225A/250A `IS [NOT] NUMERIC`). Gating
each hook `{is2002()}?` makes NIST/85 parsing **identical** (OO alternatives excluded), and `objectReferenceUsage` is
now two explicit alternatives (no optional tail). **guard-fast ALL GREEN at each of the 3 incremental steps**
(1196/509/364). Also found: the `GenerateIfNewer` regen can leave a **corrupt partial generated `.cs`** on a
file-lock/interrupt (the timestamp check misses it → spurious CS1002/CS1513); the fix is `rm -rf Generated` for a
clean regen on a grammar change.

The full OO program now **parses to the semantic gap** (CBL3128 GREETER-undefined + INVOKE-not-bound) — the grammar
is complete. Test held in `tests/conformance/2002/_pending_oo/` until the OO semantics land. **Next: OO symbols +
class emission (per-instance `ProgramState`) + INVOKE** (tasks 2–4).

