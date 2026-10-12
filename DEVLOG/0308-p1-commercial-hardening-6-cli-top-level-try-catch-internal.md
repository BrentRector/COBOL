## Entry 308 — P1 commercial-hardening #6: CLI top-level try/catch → internal-compiler-error, exit 70

Fifth P1 item. `Program.Main` had no top-level exception handler — only the emit phase
(`Compilation.EmitAssembly`) was wrapped (COBOL0600). An unexpected exception in any other phase
(preprocess, lex/parse, semantic, binder/IR) escaped `Main` uncaught and terminated the process with a raw
CLR crash dump + an abnormal-termination exit code, instead of a diagnosable error.

**Change (CLI only).** Wrap the `Main` dispatch body in `try/catch`. The try contains the existing dispatch
*unchanged*, so every already-diagnosed failure still returns 0 (success) / 1 (error) via the normal path
and never enters the catch. The catch (an *unexpected* exception = an internal compiler bug) writes a
diagnosable `COBOL0600: Internal compiler error: <type>: <message>. Please report this.` to stderr, plus the
best-effort source path (`TryFindSourceArg`) and the stack trace, and returns a dedicated **exit 70**
(BSD `EX_SOFTWARE`) — distinct from the 0/1 contract, so a code-gen defect is reportable rather than an
opaque abort.

**Note on dialect reach.** The CLI defaults to `--standard cobol85` = StrictCobol85 (only `--nist` drops to
permissive Default). So a normal `cobolsharp foo.cob` invocation *does* exercise the strict-gated P1
diagnostics (CBL3128/CBL0814/CBL3620) — the staging protects the NIST guard, not ordinary CLI users.

**Testing.** The normal try-body path is exercised by the guard's 349 CLI compiles (all exit 0) plus the
new subprocess tests `CliExitCodeTests` (valid program → 0 with "Compiled successfully"; unknown option → 1,
*not* 70 — the catch must not intercept a diagnosed failure). The exit-70 path has no deterministic trigger
to assert from a test now that the known crash sources are handled (item 8's level-number `int.Parse`
OverflowException was the prime one) — it is covered by inspection; the catch is a minimal, standard wrapper.

**Note (separate concern):** this is the *compiler's* Main. A runtime exception in the *emitted* program
runs in a different process and needs its own handler — that is item 10.

**Verify:** build 0 warnings; +2 subprocess unit tests. Full guard **ALL GREEN — 1021 unit (1019 +2) / 347
integration / 349 NIST baselines, 0 FAIL\***.

