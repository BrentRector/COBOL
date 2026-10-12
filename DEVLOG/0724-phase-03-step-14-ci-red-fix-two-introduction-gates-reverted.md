## Entry 724 — 2026-07-09 17:02 PDT — PHASE-03 Step 14 CI-RED FIX: two introduction gates reverted to bind-time (ALLOCATE, UDF) + a process lesson

**CI had been RED since 14b** (only 14a was green). Two conformance tests failed on the GitHub runners while my LOCAL
battery was green — a discrepancy with two distinct causes, one a real bug and one a real PROCESS ERROR.

**① The process error (transparency, memory `feedback_test_after_every_change`): my local `dotnet test --no-build`
runs used a STALE compiler.** `dotnet build src/Cobol.Net.Compiler` updates `src/…/bin` but does NOT re-copy the
compiler DLL into the TEST project's `bin`; a subsequent `dotnet test --no-build` then runs the test assembly against
whatever compiler was copied there at its LAST full build (≈ the 14a state). So every "battery green" claim for 14b–14f
was exercising the OLD compiler, not my changes — which is exactly why they looked byte-identical locally while CI (a
clean checkout + full build) revealed the real behavior. **LESSON, now standing: verify against a FRESH build —
`dotnet build CobolSharp.sln` (which propagates the compiler DLL into every test bin) BEFORE `dotnet test --no-build`,
or run `dotnet test` without `--no-build`.** (The relocations WERE mostly correct — CI showed only 2 failures / 3112
pass — but two were missed.)

**② The real bug (an architectural finding): INTRODUCTION gates fire on the construct's RECOGNITION, not its bound
node.** `EditionGateDiagnosticTests.Allocate_At85` (a below-2002 `ALLOCATE … RETURNING W` where W is not a pointer) and
`UdfInvocationTests.BinderGate_0900_At85_CallerOnly` (a below-2002 reference to an UNDEFINED REPOSITORY function) both
expect the `COBOLNET0900` edition diagnostic ALONGSIDE the construct's semantic error (0869 / 1505). A below-edition
construct is an edition violation *independent of whether it also has a semantic error* — but the bound-arm gate keys on
the distinctive node (`BoundAllocate` / the hoisted `BoundCallProgram`), which the binder never produces when the
construct errors first (`ALLOCATE` returns a `BoundNop`; the UDF hoist is skipped). So the bound-arm SILENTLY DROPS the
0900 on those error paths (the "A-caveat" I had documented — but it was NOT untested). This is a PATTERN: every relocated
introduction/removal STATEMENT gate has the same latent flaw whenever the construct can bind to `BoundUnsupported`/
`BoundNop` on a semantic error (UNLOCK-of-undeclared-file, OPEN-SHARING-of-undeclared-file, …), just untested.

**Immediate fix (byte-identical, greens CI): `Allocate2002` + `UserFunctionInvocation2002` reverted to their bind-time
`ConstructRegistry.Check`** (removed from the pass; the binder Check fires at RECOGNITION, before the semantic check, so
BOTH the 0900 and the 0869/1505 fire — exactly the pre-14b/14f behaviour). **Root-cause fix (Step 14h): move ALL
introduction/removal gates to the presence-based POST-BIND PARSE-ARM** — a parse-tree walk (over `BoundRunUnit.Tree`,
running AFTER bind so the semantic errors also accumulate) gates on the construct's syntactic presence, immune to the
error path. This is what `EditionValidator` already is; 14h absorbs it after bind and the intro gates join it. (Only the
genuinely-semantic gates — MOVE-category, the attribute phrases that ARE the construct — stay bound-arm.) Verified
against a FRESH solution build: the 2 tests pass, conformance 3114 · characterization 32 · unit 227 GREEN.

