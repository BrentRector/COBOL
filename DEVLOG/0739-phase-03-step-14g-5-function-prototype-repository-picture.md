## Entry 739 — 2026-07-10 12:03 PDT — PHASE-03 Step 14g.5: FUNCTION-PROTOTYPE + REPOSITORY + PICTURE skeletons migrated — the 14g DATA/PIC/OO migration is COMPLETE (binder holds only the UDF exception)

The LAST gate-migration sub-commit: three more edition gates move into `VersionConformancePass`, and — the milestone —
the grep-assert now holds: **the ONLY `ConstructRegistry.Check` outside the pass is the single documented UDF
exception** (`StatementBinder.Udf.cs:63`). Every DATA/PICTURE/OO introduction gate is now in the two-arm pass.

- **FUNCTION-ID … IS PROTOTYPE (§11.5) → BOUND-arm.** In `VersionConformancePass.Run`, `if (unit.IsPrototype)
  Check(FunctionPrototype2002,…)` over `group.Units`. `CallUnit.IsPrototype` is set at unit creation and every unit
  (top/nested/function) is a CallUnit in `group.Units`, so it is scope-exact + drop-proof; the former `CallMakeUnit`
  Check is removed. (One of the two genuine bound-arm additions in 14g, alongside the 8 USAGE gates.)
- **REPOSITORY CLASS/INTERFACE/PROPERTY (§12.3.8) → PARSE-arm.** `ParseArm.VisitRepositoryEntry`, mirroring the
  binder's PROPERTY→INTERFACE→CLASS order + name-embedding where-strings; the PROPERTY name still registers in
  `DataBinder`. Like the SPECIAL-NAMES/file-control gates, REPOSITORY is config-scoped, so a CLASS's class-level
  REPOSITORY inherits the flagged OO-env double/zero-bind (DEVLOG 738) — the parse-arm fires the spec-correct 1×.
- **External-float (symbol E, §13.18.40.4 GR13b) + national-edited (§13.18.40.4 GR10) PICTURE skeletons → BOUND-arm via
  `PicInfo.SkeletonGate`.** The recon called for a parse-arm raw-picture scan (the category is RECOVERED to Alphanumeric,
  erasing the identity), but that would RE-IMPLEMENT both PicInfo's picture expansion AND GateData's where-strings —
  fragile twice over. Instead a new `PicInfo.SkeletonGate` (a `Constructs.*` id) stamped on the recovered PicInfo carries
  the exact detection forward; `GateData` fires the 0900 (the same proven pattern as 14g.1's National PICTURE gate,
  covering report print items too). `NotImplementedSkeleton` split into a `Check` + `StagedNotImplemented`; the two
  PICTURE callers keep the ≥2002 0899 via `StagedNotImplemented` and drop the inline Check.

**Dead-code cleanup (enables the grep-assert):** `SkeletonUsage` + `NotImplementedSkeleton` are DELETED — dead since the
14g.1 USAGE-gate migration made every usage keyword LIVE (nothing constructs a skeleton usage; `ParseUsage` has no
skeleton path), which is why their `ConstructRegistry.Check` lingered outside the pass. `StagedNotImplemented` (the 0899
half) stays. `LoudGuardTests` external-float leg rewritten off the Analyze-layer 0900 to assert the moved
`PicInfo.SkeletonGate` carrier (the 14g.1 pattern; the ≥2023 0899 leg is unchanged).

Byte-neutral: characterization 32 byte-exact. New `RepositoryPrototypeEditionTests` (Conformance, 7 exact-count
witnesses: prototype 1×@85 / 0×@2002, each REPOSITORY specifier 1×, each PICTURE skeleton 1×); `DataSkeletonEditionTests`
still green (0900@85 via GateData, 0899@2002+ via StagedNotImplemented).

Battery: greenfield conformance **3156** (3149 + 7) · unit **223** · characterization **32** · INV-1-strong @ 2023
permissive **349/349** · FULL legacy guard **NIST 353 MATCH / 0 regressions** (an NC205A DIFF on the first guard run was
the known JOBS=32 parallel-build race — 353 MATCH on re-run). **RESUME AT Step 15 — PHASE-03 close** (exit-criteria
checklist 1–9; 14g.6 snapshot re-baseline is a NO-OP — characterization stayed byte-exact throughout 14g).

