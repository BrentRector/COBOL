## Entry 258 — USE-declarative rework: open-mode-scoped dispatch (spec-correct §14.9.49)

Took on the SQ runtime-hang tail (SQ105A/114A/121A). The USE-declarative implementation was incomplete
vs ISO §14.9.49: only **file-name-scoped** declaratives (`USE … ON file-name-1`) were registered and
dispatched; **open-mode-scoped** declaratives (`USE AFTER STANDARD ERROR PROCEDURE ON INPUT/OUTPUT/I-O/
EXTEND`) — which apply to every file open in that mode — were parsed (BindUse captured TargetMode) but
then dropped (VisitDeclarativeSection registered only FileNames). SQ105A relies on a scoped
`USE … ON INPUT` declarative to terminate a no-AT-END read loop at EOF, so it never fired. Reworked the
subsystem properly:
- Runtime (`FileRuntime`): track each file's open-mode scope (`_openModeScope`, set on the OPEN attempt
  so a failed OPEN still routes to the mode's declarative); new `ShouldRunUseDeclarative(file, scope)` —
  fires only when the last I-O status is an exception (NOT a successful 00/02/04/05/07) AND the scope
  applies (scope -1 = file-name; 0/1/2/3 = INPUT/OUTPUT/I-O/EXTEND matched against the file's open mode).
- SemanticModel: `_useDeclarativesByMode` (OpenMode→section) alongside the file-name map;
  `RegisterUseDeclarativeForMode`.
- BoundTreeBuilder.VisitDeclarativeSection: register `bound.TargetMode` (was: only FileNames).
- New `IrCheckUseDeclarative(file, scope)` IR + `EmitCheckUseDeclarative` → ShouldRunUseDeclarative call.
- `FileIoLowerer.EmitUseDeclarative` rewritten: a file-name-scoped declarative takes precedence
  (ISO §14.9.49); otherwise each mode-scoped declarative is dispatched by the file's runtime open mode.
  Single-paragraph sections PERFORM, multi-paragraph PERFORM THRU (factored into a helper).

Full guard ALL GREEN: 1000 unit / 348 integration / 212 NIST, **0 regressions** — no baselined test's
behavior changed (the file-I/O baselines don't depend on mode-scoped declaratives, and the IF tests are
intrinsic-function tests with no declaratives).

**SQ105A still hangs — separate root cause found (transparency).** With declaratives now correct, SQ105A
still loops. A paragraph-execution trace localized it precisely: `PERFORM BAIL-OUT THRU BAIL-OUT-EX`
(a CCVS utility) contains an internal forward `GO TO BAIL-OUT-WRITE` (a paragraph *within* the THRU
range); when that GO TO is taken (a failure being reported), reaching `BAIL-OUT-EX` does NOT return to
the caller — control falls through `CCVS1-EXIT → INITIAL-PARA`, re-running the whole test → infinite
loop. When the GO TO is not taken (fall straight to BAIL-OUT-EX), the THRU returns correctly. So this is
a **PERFORM … THRU return defect in the core paragraph-dispatch engine** when an internal GO TO redirects
within the range — distinct from USE declaratives, and high blast-radius (every program uses the
dispatch). That is the next target. (This rework is a prerequisite — SQ105A's read loop needs the INPUT
declarative once the dispatch loop is fixed.) The three SQ hangs likely share this dispatch defect.

