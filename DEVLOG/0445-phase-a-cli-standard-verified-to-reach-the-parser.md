## Entry 445 — Phase A: CLI `--standard` verified to reach the parser DialectLevel + a regression net; parallel-dev harness decision

Started MASTER_PLAN **Phase A** (enable maximum parallelism). First item: the suspected **CLI dialect bug** — the
plan flagged that `cobol --standard cobol2002|2014|2023` "may not reach the parser `DialectLevel`" (only the
conformance-harness path was known-good).

**Verified — the bug does not exist in current code (refuted, like the M2-PRE-1 over-claim).** Traced the path:
`Program.RunCompile` maps `--standard <ver>` → `CompilationOptions.Dialect`; `Compilation.Compile` reads
`Options.Config.ParserLevel` (`DialectConfig.ParserLevel` = `(int)Version`, 85 for Default) and sets
`parser.DialectLevel` (the ONLY assignment site, `Compilation.cs:319`). This is the SAME path the conformance
harness drives (`EndToEndTestBase` sets `Options.Dialect`). The earlier `Options.Config.ParserLevel` plumbing had
already closed the gap the audit saw. Confirmed empirically through the real `cobolsharp.dll` CLI:
- `goback_returning.cob` (gated `{is2002()}?`): `--standard cobol85` → exit 1 (parse-rejects `RETURNING`);
  `--standard cobol2002` → exit 0 and the program runs to the expected `SUM=0042 / SUM2=0013`.
- a minimal `DELETE FILE` program (gated `{is2023()}?`): rejected under cobol85/2002/2014, accepted under cobol2023.

**Locked it in (regression net).** Added two `[Theory]` cases to `CliExitCodeTests` (the end-to-end-through-
`Program.Main` suite): `Cli_Standard_GatesGoback2002Feature` (the 2002 boundary) and
`Cli_Standard_GatesDeleteFile2023Feature` (the 2023 boundary), each asserting a version-gated GRAMMAR feature is
accepted ONLY from the standard that introduced it — pinning every `--standard` value to the correct parser level.
Grammar gates (parse-time `{isYYYY()}?` predicates) make the result purely the dialect threshold, independent of
semantics/codegen. Unit 1196 → 1204 (+8); guard ALL GREEN (1204 / 509 / 364, `guard-fast.sh`).

**Parallel-dev harness decision (early-resolve #1).** Adopted the **patch-integrate** pattern over worktrees: in
this repo `Workflow isolation:'worktree'` branches from a STALE commit (`feedback_worktree_workflows_stale`), so
the main loop does all WRITES on `main` sequentially, guard-gated, while fanning out parallel READ-ONLY agents
(investigation / design / adversarial review) freely; genuinely independent features are built one-at-a-time on
`main` (or produced as diffs and applied + guard-checked one at a time). Its proof — two independent features
concurrently — comes naturally in the Phase-C conformance fan-out.

