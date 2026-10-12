## Entry 1708 — 2026-09-25 12:34 PDT — PB1590: no test asserts a wall-clock ceiling — six tests re-shaped, a drift test forbids the pattern

Owner: "THIS IS THE SECOND TIME THIS FAILED DUE TO A FEW SECONDS OVERRUN." `DeepNestingTests` timed a whole compile
(Roslyn included) against 20 s and went red on a loaded hosted Windows runner (26.4 s for a 1-second compile) with no
compiler change — twice. The first red was noted and re-run; that was the mistake. Fixed at the root, and swept:

- **DeepNestingTests** counts the work the DEVLOG 502 memo bounds — `PhysicalModel.ListBuilds` (one physical-field list
  per group + the root forest) through the `AsyncLocal` seam `PhysicalModel.Observer` — at depths 8 and 16: ≤ depth + 1,
  and exactly +8 between them. Planted regression (memo disabled): **749** lists at depth 8 against 9, red in 1 s (small
  depths so a regression FAILS instead of hanging CI; the 49-level CCVS programs stay in the NIST leg).
- **CobolTimingTests**: CONTINUE AFTER's suspension is REPORTED through the new `AsyncLocal` seam
  `CobolTiming.SuspensionObserver` instead of slept; "no suspension" is asserted, plus a control (2.9 s → exactly 2000 ms).
- **PictureCompositionTests** scale gate: the absolute 5-second ceiling became a same-run GROWTH RATIO (best-of-three at
  3 000 vs 30 000 symbols, < 30×; linear ≈ 10×, quadratic ≈ 100×).
- **CldrLocaleLoaderTests** (zh, 20 s) and the legacy **FixedFormTests** (two 1-second ceilings): ceilings removed.
- **NoWallClockAssertionDriftTests** (new): any elapsed-time comparison in tests/ is red — it found the PictureComposition
  ceiling the first grep missed.
- **Review skills**: the project `review` skill's full-code dimension now flags fixed time limits in tests; the public
  BrentRector/claude-skills `review` skill and `pr-test-analyzer` agent (new fifth check, Determinism) carry the same
  rule (e132f31). Memory: feedback_no_wall_clock_tests.
- Gate: Conformance filter (Drift|EditionGate|CorpusRunner) 3600/0, Unit 29310/0 (first run red on
  TestRepoDriftTests — the new test found the repo root itself; now uses TestRepo), Characterization 33/0.

