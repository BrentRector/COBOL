## Entry 614 — 2026-07-05 00:30 PDT — Phase 4 M2-DATA-1: the BINARY-CHAR/SHORT/LONG/DOUBLE family goes native

The reconciliation audit (DEVLOG 610) flagged the M2-DATA "done" catalog marks as legacy-only mirages:
the fixed-width binary usages were implemented in the retired byte engine and staged **loud** (COBOLNET0899)
in the greenfield. This closes the unlettered "Phase 4 misc" row — `USAGE BINARY-CHAR / BINARY-SHORT /
BINARY-LONG / BINARY-DOUBLE [SIGNED|UNSIGNED]` are now first-class native integers.

**Spec basis (ISO/IEC 1989:2023 §13.18.60.4 GR12/GR21, §13.16.3 SR8).** GR12 defines these as PICTURE-less
integer numeric items given by a *minimum range* (SIGNED −2^(k−1)..2^(k−1)−1, UNSIGNED 0..2^k−1 for
k = 8/16/32/64 bits; the implementor may allow wider); GR21 makes the representation/length implementor-defined
with SIGNED and UNSIGNED sharing width; §13.16.3 SR8 **prohibits a PICTURE** on the family (like INDEX / POINTER /
OBJECT REFERENCE). The spec gives *no* implied digit count — DISPLAY width is our documented implementor choice.

**Design (decision-complete, recon-verified).** Realized as native two's-complement integers on the existing
COMP-5 **BinaryCapacity** discipline:
- `PicInfo.BinaryItem(usage, signed)` synthesizes a PICTURE-less `PicCategory.Numeric` item with the fixed byte
  width (1/2/4/8 → `StorageWidth`), `BinaryCapacity` truncation, `Signed` per SIGNED(default)/UNSIGNED, and an
  implied DISPLAY digit count = the decimal width of the range's max magnitude: **CHAR 3 / SHORT 5 / LONG 10 /
  DOUBLE 19 (signed) · 20 (unsigned)**. Storage stays `long` (Digits ≤ 18) / `Int128` (BINARY-DOUBLE) by the
  existing digit tier — the Int128 substrate already carries the unsigned 8-byte range (0..2^64−1), so **no new
  native `ulong` storage type was added** (one refinement vs the D6 sketch).
- `Usage.BinaryChar/Short/Long/Double` left the `IsUnimplementedSkeleton` guard; `ParseUsage` now returns the
  specific member (introduction gate 0900 below 2002, silent at 2002+ — the POINTER/OBJECT-REFERENCE pattern);
  `DataBinder` reads the `binarySign` sibling and synthesizes via `BinaryItem`, rejects a PICTURE with the new
  **COBOLNET0870** (§13.16.3 SR8), and sheds a synthesized profile off group headers. BINARY-* joined COMP-5 in
  the Tier-C REDEFINES loud-guard (a native binary leaf can't form a digit-image pun).

**The load-bearing runtime fix — BinaryCapacity was a documented STUB.** `CobolNum.Store`/`TryStore` *skipped*
truncation for `BinaryCapacity` ("COMP-5 wraps by width — later slice"): an over-range COMP-5/BINARY store was
silently unbounded. Implemented for real: `WrapBinary` (Store — native two's-complement wrap, the width analog of
high-order digit truncation; unsigned stores the magnitude per §14.9.25 GR8) and `InBinaryRange` (TryStore — the
ON-SIZE-ERROR range check), both keyed off `NumProfile.StorageLength`, branching signed vs unsigned. In-range
values are the identity, so **COMP-5 is not regressed** (the one greenfield COMP-5 golden, `PIC S9(4) COMP-5
VALUE −300`, is in-range) — and COMP-5's own stubbed overflow is now correct as a side benefit.

**Verification.** `binary_usage.cob` enabled → byte-exact against its committed golden (BC=127 / BCNEG=−128 /
BCU=255 / BS=32000 / BL=2000000000 / BD=9000000000000000000 / COMP=0000001000). New `BinaryCapacityTests` (24
runtime cases: signed wrap, unsigned magnitude-mod, TryStore SIZE-ERROR boundaries, in-range identity, the 8-byte
unsigned-above-long case) and `BinaryUsageDataTests` end-to-end (`COMPUTE` the same 200 into signed vs unsigned
BINARY-CHAR → `S=-056 U=200`, proving the emitter threads BinaryCapacity into a real store). `DataSkeletonEdition`/
`LoudGuard` flipped from skeleton-0899 to live (positive compile + 0900-at-85 + 0870 PICTURE-prohibited);
`constructs.json` row activated (compiles 2002+, 0900 at 85). **Battery: Unit 123/123 · Conformance 1610/1610
(+18) · 0 regressions.** Greenfield-only change (no shared frontend/grammar, no legacy) — the grammar already
carried `binarySign`, so the greenfield suites are the gate.

**Process.** A 6-scout read-only recon Workflow (spec / grammar / binder / runtime / registry / docs) front-loaded
the decision-complete design — the spec scout surfaced the §13.16.3 SR8 PICTURE prohibition I would otherwise have
missed. Transparency: two scouts (grammar, registry-tests) returned degraded stub outputs under the structured-output
schema (a `"test"` placeholder), so those two subsystems were read firsthand instead — no design impact, but a
reminder that schema-forced returns can silently degrade and must be cross-checked.

