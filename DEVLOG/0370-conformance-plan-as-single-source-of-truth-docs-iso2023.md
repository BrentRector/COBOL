## Entry 370 — Conformance plan as single-source-of-truth: `docs/ISO2023_CONFORMANCE_PLAN.md`

Ran a 15-area parallel spec-conformance audit (workflow `iso2023-conformance-audit`: 15 agents vs
`specs/ISO_COBOL.md` + the actual compiler, ~1.29M tokens, synthesized into a ranked backlog) and turned it into
a **durable, authoritative work-breakdown** — `docs/ISO2023_CONFORMANCE_PLAN.md`. The owner noted the gap
analysis had been redone ≥3 times; this doc is now the SSOT so future sessions **work from it instead of
re-auditing**. `docs/MULTIVERSION_ROADMAP.md` now points to it for detail; memory `MEMORY.md` marks it SSOT.

The audit surfaced concrete **correctness defects** (not just missing features), which the plan ranks first:
- a user-function call in any context OTHER than a whole-source MOVE/COMPUTE silently yields **0** (falls through
  to the intrinsic path) — and `FUNCTION FOO(5)` / `FOO(A+1)` (literal/arith args) also return 0;
- `USAGE BINARY-CHAR/SHORT/LONG/DOUBLE` silently mis-types;
- only 2 of the 8 `ROUNDED` modes are implemented;
- `INITIALIZE … TO VALUE / THEN TO DEFAULT / WITH FILLER` phrases are dropped;
- three preprocessor spots crash on valid source (mid-file `>>SOURCE FORMAT`, copybook conditional compilation,
  unhandled directive tokens).

The plan also catalogs the large remaining subsystems (national data, boolean/bit, pointers+ALLOCATE/FREE, the
EC/exception framework, VALIDATE, OO COBOL; M3 dynamic tables/TYPEDEF/JSON-XML; M4 deltas), an execution-wave
order, the production/commercial axes (diagnostics, dialect strictness, conformance corpus, perf, tooling), and
the **gated post-conformance milestones** — the full architectural review and the project/exe rename
(`CobolSharp` → **`COBOL.NET`**, executable **`cobol.exe`**; memory `project_post_conformance_goals`).

Docs-only; guard unaffected (1047 / 455 / 364). Next: execute Wave 1 from the plan (UDF general inline form +
literal/arith args — the silent-0 correctness fix).

