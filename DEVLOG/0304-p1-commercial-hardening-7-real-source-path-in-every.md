## Entry 304 — P1 commercial-hardening #7: real source path in every diagnostic + retire the bare "SEM" code

First of the P1 "diagnostics on invalid input" items from `docs/ARCHITECTURE_ASSESSMENT.md` (the owner
reframed the goal from validation-suite-green to commercial-grade; the assessment's #1 cross-cutting gap is
that the compiler under-invests in diagnosing invalid input). Execution mode (owner decision): **sequential,
guard-gated**, one item at a time with a full guard between each, and any *new strictness* checks **dialect-gated
to named-strict modes first** so the 349 NIST baselines (which run permissive Default) stay green by construction.
The diagnosis was produced by a parallel 7-agent workstream (infra map + the 6 P1 items, each with file:line
targets + a green-test risk assessment); item 7 was the lowest-risk and the *foundation* the others build on, so
it went first.

**Two changes, both semantics-preserving (no diagnostic's firing condition, severity, or line/column changed):**

1. **Real source path everywhere.** The parse stage already reported the true path (`CobolErrorListener` is
   constructed with `sourcePath`), but every *post-parse* diagnostic hardcoded the `"<source>"` placeholder
   because the path was never threaded past parsing — the `AntlrInputStream` is created nameless and
   `SemanticBuilder`/`ReferenceResolver`/`BindingContext` had no file-name field. Fix uses one carrier:
   `SemanticModel.SourceName`, set in `Compilation.BuildSemanticModel` from the `sourcePath` already in scope.
   Every static validator already holds the model (→ `model.SourceName`); every bound-tree binder holds the
   `BindingContext` (→ new `BindingContext.SourceName => Semantic.SourceName`); the two builders that run
   *before* the model exists (`SemanticBuilder`, `ReferenceResolver`) take a `sourceName` ctor arg. Replaced the
   live `"<source>"` literals across 14 files (SemanticBuilder, ReferenceResolver, the 4 static validators,
   BoundTreeBuilder/Validator, and 6 binders). `SourceLocation.None` stays the *only* legitimate `"<source>"`.

2. **Retired the bare "SEM" code.** Three sites emitted diagnostics with the ad-hoc string code `"SEM"` (no
   registry descriptor, so undocumentable/unsuppressable/untestable-by-code): `ReferenceResolver.Error`
   (PERFORM/GO TO target, READ/OPEN/CLOSE file operand), `SemanticBuilder.Error` (SPECIAL-NAMES CURRENCY /
   SYMBOLIC CHARACTERS, SCREEN HIGHLIGHT/USING), and `ParagraphValidator` (phantom keyword-named paragraph).
   Minted descriptors **CBL3120** (`{0} target '{1}' is not a paragraph or section`), **CBL3121**
   (`{0} target '{1}' is not a declared file`), **CBL3122** (phantom paragraph, Warning), and **CBL3123–3127**
   (currency-needs-SYMBOL / currency-symbol-invalid / symbolic-char-count / SCREEN highlight / SCREEN using).
   `ReferenceResolver.Error` and `SemanticBuilder.Error` were refactored to take a `DiagnosticDescriptor`
   + `params object[]` (mirroring the pre-existing `RenamesError` helper), preserving the original message intent.
   The verb (`PERFORM`/`GO TO`/`READ`/`OPEN`/`CLOSE`) is passed as `{0}` so no information is lost.

**Why this is the foundation.** A commercial compiler / IDE / build pipeline needs the real file path in every
diagnostic, and a registry descriptor (vs a bare string) is the unit that can be documented, severity-tuned,
and asserted on. Items 5 (undefined data-name) and 8 (illegal PICTURE) construct diagnostic locations and will
now get the real path for free.

**Allocation gotcha (caught by grounding in the actual file).** The diagnosis agent had proposed `CBL3116` for
the new undefined-data-name code, but `CBL3116` is already taken (GLOBAL-clause error); the real free space in
the scope/symbol family starts at `CBL3120`. Always verify free descriptor numbers against
`DiagnosticDescriptors.cs`, not a plan. **Method note (transparency):** a `replace_all` for the
`tok.Line/tok.Column` `"<source>"` pattern missed the twin site in `CheckRecordKeyNoiseWord` (line 795) because
it has different indentation than line 767; a post-edit `grep "<source>"` sweep caught it. It is a
strict-mode-only RECORD-KEY-leniency location string — provably inert for the NIST corpus (Default mode +
output-only comparison) — but fixed for completeness.

**Left for a separate cleanup:** `FlowAnalysis/PerformRangeChecker` and `ParagraphReachabilityAnalyzer` still use
a bare `"FLOW"` code + `"<source>"`, but they are **dead code** (not wired into `Compilation`); a verify-then-delete
pass (PROMPT.md: zero dead code) is out of item-7 scope.

**Verify:** build 0 warnings / 0 errors; +3 unit tests (`SourcePathDiagnosticTests`: CBL3120 fires with the real
`TEST.cbl` path; no `"SEM"` remains; valid program clean). Full guard **ALL GREEN — 1003 unit (1000 +3) / 347
integration / 349 NIST baselines, 0 FAIL\***.

