## Entry 513 — 2026-06-09 13:59 PDT — COBOL.NET: lock 4 more byte-matching NC programs (NC118A/119A/177A/205A) — corpus sweep, +0 features

Resuming the G5 NC corpus drive, I re-ran the compile/run/diff sweep across all 95 NC programs to pick the next
target (the resume-prompt method). Result: **89/95 compile**, and of those **12 already byte-match the golden** —
four MORE than the eight locked into `NistDifferentialTests`. **NC118A, NC119A, NC177A, NC205A** are nucleus
arithmetic / data-movement / conditional programs (MOVE / PERFORM / GO TO / ADD / SUBTRACT / IF) that the feature
surface built for the first eight already greens — they had simply never been added to the net. Locked all four in
as permanent `[InlineData]` regressions (verified through the authoritative in-process differential harness, not just
the CLI sweep). **No new feature was needed** — this is pure regression coverage banked from the sweep.

The sweep also mapped the road ahead (for the next slices): **8 compile-but-MISMATCH** programs, closest first —
NC106A (diff 10), NC176A (diff 10), NC219A (diff 16), NC114M / NC116A / NC171A (diff 34), NC134A (118), NC124A
(850); **67 RUNERR** (compile but hit a runtime loud guard for an unimplemented verb — the INSPECT / EVALUATE /
SEARCH / STRING / UNSTRING / INITIALIZE / ACCEPT backlog); **6 CMPL_FAIL** (backend C# type-mismatch — NC104A /
107A / 108M / 222A / 247A / 252A, deeper codegen, e.g. the known NC252A numeric Tier-B-view `string == long`).

**Tests:** conformance 283 → **287** (the 4 new NC golden programs); 14 unit; the 8 prior green NC programs
unaffected. Greenfield-only. NEXT = target the closest MISMATCH (NC106A / NC176A) — implement the union of what
they need and finish at a new green program.

