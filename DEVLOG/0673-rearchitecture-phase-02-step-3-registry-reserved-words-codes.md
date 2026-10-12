## Entry 673 — 2026-07-07 21:12 PDT — Rearchitecture PHASE 02 step 3 — registry / reserved-words / codes moved BELOW the frontend into `Cobol.Net.Editions`; `Check` is sink-based

The root-cause fix (P1/P2/P7): the canonical edition metadata now sits in the lowest leaf, so both Frontend and Compiler
consume the SAME registry + severity policy instead of each re-encoding it. Moved out of `src/Cobol.Net.Compiler/Validation/`
into `src/Cobol.Net.Editions/` (namespaces → `CobolNet.Editions`): `EditionCodes`; `ConstructDialectStatus` (the
`ConstructAvailability` verdict enum + the record, split into `ConstructDialectStatus.cs`) and `ConstructRegistry` (the 91
Entries + `Find` + `Check`, split into `ConstructRegistry.cs`); `ReservedWords` + the generated `ReservedWords.Table.cs`
(git-mv, namespace flipped; `gen-reserved-words.ps1`'s `$csOut` retargeted). Only `EditionValidator` stays in `Validation/`
(it is inherently ANTLR-grammar-coupled — `CheckedTokenTypes`, `IsProvableUserWordPosition`).

**`ConstructRegistry.Check` is now layer-neutral + sink-based:** `Check(EditionInfo, IDiagnosticSink, id, where)` — it
evaluates availability at the `EditionInfo`, computes the severity via the new **`EditionSeverityPolicy`** (the ONE
strict/permissive decision, extracted from what `EditionContext.Removed` + the obsolete/intro arms did), and reports an
`EditionDiagnostic` to the sink. The message templates (em-dash and all), the dual-obligation code selection
(`RemovedIn is null ? DiagnosticCode : 0900`), and the strict/permissive routing are reproduced VERBATIM, and the
`EditionContext.Report` bridge renders them back onto the legacy `error/warning {code}:` string channels — so the emitted
diagnostics are byte-identical. Mechanically updated all **53** `ConstructRegistry.Check(X, …)` call sites to
`Check(X.Edition, X, …)` (X = `_edition`/`edition`/`data.Edition`, each IS the sink): EditionValidator ×26, PicInfo ×14, six
StatementBinder partials ×13. Swapped `using CobolNet.Validation;` → `CobolNet.Editions;` in the 7 consumer files (+ the two
drift tests + EditionContextTests) and added the using to EditionValidator.

Byte-stable, greenfield-compiler-only (Frontend + legacy untouched → legacy guard invariant): conformance **2036** · unit
**222** (both unchanged; the ConstructRegistry/ReservedWords drift tests are within the 222). Build 0/0. `ConstructAvailability`
moved to Editions as its permanent home (no transient duplicate). Next: P2.4/step 5 generates Entries + `Constructs.*` +
`GateId` from `constructs.json`.

