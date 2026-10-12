## Entry 782 — 2026-07-11 13:35 PDT — P5.11c: `PictureAnalyzer` extracted; `PicInfo` a pure value record; the skeleton scaffolding + reference-identity sentinels DELETED

**What.** PHASE-05 Step 11c (DESIGN-data-model §2.7; DESIGN-module-topology row 20). PicInfo.cs 736 → ~350 lines:
- **`Binding/PictureAnalyzer.cs` (NEW):** `Analyze` (the ~245-line PICTURE scanner — the §13.18.40.3 SR2 whitelist,
  the N/1/E legs, P-scaling, the category classification) + `ParseUsage` (the §13.18.60 keyword map) +
  `ExpandRepeats` + `StagedNotImplemented` move off `PicInfo`, which keeps the analyzed FACTS + the representation
  projections (ClrType, DefaultInitializer, StorageWidth, ProfileInitializer, the PICTURE-less item factories,
  `SignKindFor` — shared by `BinaryItem` + the binder's SIGN inheritance). 3 callers + the LoudGuard tests repointed.
- **Dead skeleton scaffolding DELETED:** `IsUnimplementedSkeleton` was the constant `false` (its comment admitted
  it: the float trio went LIVE at D16/Phase 6a and the FLOAT-BINARY/DECIMAL family gates in ParseUsage) — it,
  `SkeletonReached`, and the 4 guard arms on the storage-mapping switches were unreachable. Likewise
  `ParseUsage`'s `out bool skeleton` overload: NO case ever set it true since the 14g.1 introduction-gate
  migration, so `MakeItem`'s `skeletonUsage ? RecoveryItem : null` arm was dead — both deleted. The
  FloatShort/-Long/-Extended enum docs still said "SKELETON; NOT part of IsFloat" — stale-doc fix to LIVE.
- **The 3 reference-identity sentinels → an explicit discriminant:** `NationalUsagePending`/`BitUsagePending`
  (placeholder Pic shapes adjudicated by `ReferenceEquals` in `ResolveIndexItems`) become
  `DataItem.Pending : PicPending {None, NationalUsage, BitUsage}` — MakeItem writes it (Pic stays NULL, never a
  lie-shape), CloneItem carries it, ResolveIndexItems adjudicates + CLEARS it (group header → the usage sheds per
  §13.18.60.4 GR1 + the SR12/SR5 leaf checks; elementary → 0881 + the recovery shape). `RecoveryItem` becomes the
  documented `PicInfo.Recovery(int length = 1)` FACTORY (a plain value, no identity semantics) — the analyzer's 5
  inline recovery constructions now share it (singular-pattern).
- **DEVIATION (recorded in DESIGN §2.7):** the design sketched a `PicAnalysis` RESULT discriminant
  (`Ok | GroupUsageShed | Recover`) — but the two Pending sentinels never CAME from Analyze: they arise for
  PICTURE-LESS entries (Analyze is not even called), and the group-vs-elementary verdict is unknowable until the
  forest completes. The state was always DataItem state encoded as Pic identity; `DataItem.Pending` is the honest
  home. `Recover` maps to the `PicInfo.Recovery` factory.
- Behavior note: a VALUE clause on an (already-0881-fatal) picture-less NATIONAL/BIT elementary no longer runs
  `ValidateValueCategory` against the phantom alphanumeric sentinel shape — the compile fails identically via
  the 0881; one secondary diagnostic on a dead entry may differ.

**Verify.** Sln Debug + Release clean; 3166 conformance · 281 unit (incl. LoudGuardTests over the extracted
analyzer) · 32 characterization; legacy guard 353 MATCH.

