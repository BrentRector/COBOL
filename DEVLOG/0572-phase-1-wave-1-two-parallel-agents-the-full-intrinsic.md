## Entry 572 — 2026-06-11 00:12 PDT — Phase 1 Wave 1 (two parallel agents): the FULL intrinsic-function catalog (§15) + the Tier-C mixed-usage record codec → 46 programs locked (948 conformance; 303 NIST)

**Two implementation agents on disjoint file sets, in parallel, from their decision-complete scout briefs
(`/e/tmp/phase1-briefs/intrinsics-brief.md`, `tier-c-codec-brief.md`); orchestrator-verified and integrated.**

**Phase 1B — intrinsic functions (the whole 1989 Intrinsic Function Module, 42 IF programs).** The greenfield
had ZERO intrinsic binding (every IF program died on the same two loud binder cells; the shared frontend
already parsed everything — no grammar change, no legacy exposure). Landed per the LIVE deep-dive
`COBOLNET_INTRINSICS_DESIGN.md`: the ONE declarative `IntrinsicCatalog` (~94 rows with IntroducedIn/RemovedIn
edition windows — post-85 rows bind loud pending their editions), `StatementBinder.Intrinsics` (SUB-token
recursive-descent argument parser: nested FUNCTION, OF/IN + nested subscripts, signed/decimal/string literals;
table(ALL) expansion per §15.3; MAX/MIN category polymorphism; the D7 LENGTH fold; D8 edition gating
COBOLNET1501–1504; the D6 injectable compile clock), the catalog-driven `IntrinsicRenderer` (numeric channel
with the H1 working-scale floor; STRING channel via OperandText so `IF FUNCTION CURRENT-DATE >= T` compares in
the collated string channel; WHEN-COMPILED baked as the §15.99.3 r2 compile-time constant), and the
`CobolIntrinsics.{Float,Exact,Text}` + `CobolDate` runtime families (ONE `FromDouble` quantization —
Math.Round, NaN→0 = the §15.3 EC default; MOD per the §15.64.4 sign table; MEDIAN §15.61.4 r2 exact ×10/2;
ORD-MAX/MIN tie=first; the spec-form one-sequence RANDOM §15.75.3; NUMVAL/NUMVAL-C scaled-long parsers;
CHAR/ORD PCS-relative with the `__COLLATE` conditional; 1601-01-01 epoch dates §15.5.2). 50 new
spec-pinned/differential facts in `IntrinsicFunctionDifferentialTests`.

**Phase 1E — the Tier-C byte-island record codec (4 ST programs).** The encoding decision: a fixed-point
BINARY/PACKED leaf's record image is its fixed-width ZONED DIGIT image with a trailing-overpunch sign — ISO
§13.18.60 USAGE GR4 makes the representation implementor-defined, and the digit image extends the settled
§14.4 image rule with zero runtime changes. `PicInfo.ImageSignKind` (the ONE image-sign mapping),
`DataItem.IsImageCapable` (float/COMP-5/INDEX stay loud — the narrowed island), generated `AsImage`/`FromImage`
fixed-point arms in FieldEmitter, the `EmitImageInto`/`EmitGroupMove`/`EmitStoreFileStatus` predicate flips,
SORT key descriptors carrying the IMAGE sign kind (never BinaryMinus — the silent mis-sort hazard), the
SAME-RECORD-AREA COMP-leaf class relaxed to Tier B with image-stored windows (ST134A), and `MixedGroupImage`
RETIRED (its signed variable-width latent bug died with it; the obsolete interim-rejection unit test replaced
by two tests pinning the new tier shape). 10 new spec-pinned facts in `MixedUsageRecordImageDifferentialTests`.
Full ST-family collateral sweep: all 29 GREEN.

**Locked: IF101A–IF142A (42) + ST108A/ST127A/ST133A/ST134A. 948 conformance + 16 unit green (one obsolete
unit test legitimately superseded); 303 NIST programs locked. The ST suite is COMPLETE (29/29); the IF suite
is COMPLETE (42/42).** Census list extended with the IF suite (403 programs).

