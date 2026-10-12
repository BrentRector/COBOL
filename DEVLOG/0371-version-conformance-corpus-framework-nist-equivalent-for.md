## Entry 371 — Version-conformance corpus framework (NIST-equivalent for 2002/2014/2023)

Owner directive: post-1985 COBOL versions need NIST-level conformance + regression tests, and "creating and
running the test cases should be part and parcel of each feature addition." NIST CCVS covers only '85. Built the
equivalent for 2002/2014/2023 as the **lowest-friction framework possible**: a **file-based corpus** at
`tests/conformance/<version>/`, where each test is a `<name>.cob` program + a `<name>.out` (expected stdout).

`ConformanceTests` (integration project) is an xUnit `[Theory]` with `[MemberData]` that **auto-discovers** every
`.cob`/`.out` pair under `tests/conformance/{2002,2014,2023}/`, compiles each under that version's `--standard`
dialect (the directory selects the dialect), runs it, and asserts normalized stdout. So **adding a conformance
test is just dropping two files** — no test code — and it runs inside `scripts/guard.sh` automatically. Output
comparison is CRLF==LF and trailing-whitespace-insensitive.

Seeded `2002/` with four verified M2 features: `udf_invocation` (FUNCTION user-name(args) in COMPUTE+MOVE →
`C=0042`/`M=0042`), `conditional_compilation` (>>DEFINE/>>IF → `DEBUG-ON`), `options_paragraph` (`OPTOK`),
`repository_paragraph` (`REPOK`). `tests/conformance/README.md` documents the convention + the per-feature rule;
`docs/ISO2023_CONFORMANCE_PLAN.md` §0 + §5 record it as standing process; memory
`feedback_conformance_tests_per_feature` makes it a cross-session rule.

**Standing rule from here:** every 2002/2014/2023 feature ships a conformance test in the same commit; backfill
already-landed M2 features over time.

Guard ALL GREEN: **1047 unit / 459 integration / 364 NIST** (+4 conformance cases), 0 regressions.

