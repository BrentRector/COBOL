## Entry 895 — 2026-07-18 16:49 PDT — PHASE-13 Wave G — 8 pin-to-spec behavior-row dispositions (CONFORMANCE.md) + a false-claim correction

Recorded the CLASS B/C behavior-row determinations from the remaining-waves scout in `docs/CONFORMANCE.md` §3 — each
verified spec-first (the 3 decisive citations grep-confirmed in the spec before recording, since the scout had just
been wrong about §8.4.2.3): VCR 22 I-O status '07' restricted to OPEN/CLOSE (already ⊆ CLOSE, no gate), VCR 24 '37'
insufficient authority (§9.1.13.6 item 6b — "may"/processor-dependent, emitted all editions, not gated), VCR 78 '39'
fixed-attribute conflict (documented non-support — no persisted attribute catalog), VCR 33 transfer-of-control
sections (already pc-range targets), VCR 37 WRITE-no-EOP fall-through (natural default; no FLAG-14 option exists —
the task hint was wrong), VCR 14 >>EVALUATE combined-condition (impl already matches the 2023 AND-truth GR6/GR10),
VCR 17 figurative ALL unspecified length (§8.3.3.6.4 GR3b/c, already defined), VCR 20/49 UPPER/LOWER-CASE mappings
(§15.97.4 r4 makes the correspondence IMPLEMENTOR-DEFINED absent a locale — the decisive citation the audit didn't
quote; .NET invariant tables, the 2023 annex code-point changes not separately tuned).

**Doc-drift caught + corrected:** CONFORMANCE.md §3 falsely claimed I-O status '04' (record-length mismatch) "is
reported" — but the runtime has no '04' constant and the record-sequential READ fits to the fixed width without
flagging a mismatch (grep-confirmed 0 hits; FileStatus.cs has no '04'). Corrected the bullet to the honest current
state (not yet emitted; VCR 21 pending — it is an organization-dependent implement wave, not a trivial fix, and its
blast radius lands on the flake-prone SQ suite).

Doc-only; no code/battery. Disjoint from the in-flight Wave F (which touches DIAGNOSTICS.md + the design doc, not
CONFORMANCE.md). The VCR row-flips (VERSION_CHANGE_REFERENCE dispositions) are P14 matrix-closure bookkeeping. Diag
band reconciliation for the Wave G code items (deferred): 1570 free (main session used none), 1571 taken by Wave F,
so VCR 34→1570 / VCR 27→1572 when those CLASS A items land.

