## Entry 671 — 2026-07-07 20:56 PDT — Rearchitecture PHASE 02 step 2 — `EditionSeverity` + `IDiagnosticSink` / `EditionDiagnostic`

Step 2: the structured-diagnostic value types in `Cobol.Net.Editions`, ahead of moving the registry onto them. Three
new leaf types — `EditionSeverity {Error, Warning}` (the two outcomes the ONE severity policy will produce; distinct from
the frontend's richer Info/Warning/Error), `EditionDiagnostic` (a `readonly record struct` carrying Code/Severity/
ConstructId/Message/Where/Citation — **no** source `Location`, since Editions is below Frontend and cannot see the
frontend `SourceLocation`; the compiler-side adapter's `List<string>` channels don't need it — the shared location type
unifies in P4/P7), and `IDiagnosticSink.Report(in EditionDiagnostic)` (the layer-neutral channel BOTH Frontend and the
Compiler's `EditionContext` adapter will implement). `EditionSeverityPolicy` is deferred to P2.4 where it lands beside the
`ConstructAvailability` verdict enum it switches on (cohesion — it needs the enum, which moves with the registry).

Purely additive leaf types — nothing in the compile pipeline consumes them yet, so compiler behavior is byte-identical
(conformance unaffected). Added `EditionInfoTests` (9 facts) locking `EditionInfo` (MaxDigits 18/31, `Has`, `Of` rejecting
an unknown year, the permissive axis, `Latest`=2023) + the `EditionDiagnostic`/`IDiagnosticSink` round-trip. Verified:
Editions + Unit build 0/0; unit **222** (213 + 9).

