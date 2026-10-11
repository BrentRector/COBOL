## Entry 666 — 2026-07-07 17:47 PDT — Rearchitecture PHASE 00 (migration safety net) STARTED — steps 1–2

Began EXECUTING the rearchitecture roadmap (`docs/COBOLNET_REARCHITECTURE_PLAN.md`) at Phase 00 — the migration safety
net (characterization harness, oracle bake-out, corpus consolidation, ref caching) that de-risks every later phase. No
compiler behavior change except one behavior-neutral perf edit.

**Step 1 (reconciled).** The phase called for a new `docs/rearchitecture/ROADMAP.md` migration SSOT, but that role is
already filled by `docs/COBOLNET_REARCHITECTURE_PLAN.md` (STATE banner + P0–P16 index + exit criteria). Per the
singular-pattern principle, did NOT fork a second doc — flipped the master plan's banner to "P0 IN PROGRESS" and
recorded the reconciliation in PHASE-00's STATUS line; DOC_INDEX already carries the master-plan row.

**Step 2 (the one production edit — perf-only, behavior-neutral).** `RoslynBackend.ReferenceAssemblies()` now caches
its ~180-entry `MetadataReference` set in a process-lifetime `static Lazy<ImmutableArray<MetadataReference>>` instead of
rebuilding (~180 `MetadataReference.CreateFromFile`) on every compile — the in-process battery compiles thousands of
times per run. **MEASURED:** the Conformance suite (2028 tests) ran in **1m24s**, down from ~2m13s–3m04s earlier this
session (uncached) — a ~40–55% test-execution speedup. Behavior-neutral: all 2028 green, identical verdicts.

Battery: 2028 conformance GREEN (cached). NEXT: P0 step 3 — the `Cobol.Net.Tests.Characterization` project skeleton.

