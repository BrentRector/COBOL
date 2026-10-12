## Entry 326 — Plan to 100% COBOL-85 + multi-version roadmap (M0→M4); Wave-1 audit: the baseline axis is already complete

The owner asked for a plan to bring the compiler to 100% COBOL-85 ISO compliance + implementation, validated by
NIST, executed as parallel workstreams, run autonomously. Two scope forks resolved up front:

1. **Removed-after-'85 modules (Debug/Segmentation/Communication/obsolete) → parse + dialect-flag ONLY, no
   runtime** (owner-locked). Building their runtime serves only the '85 dialect, is non-conformant from 2002 on,
   and violates the zero-legacy-dead-weight doctrine. This is a legitimate conformance *profile*, not a
   compromise — and the decisive evidence is that **all 9 NIST Communication tests are flagging-only `…M`
   modules** (no executable `…A` programs exist), so parse+flag is the *only* thing NIST ever tests for CM.
2. **Multi-version is the overarching mission, not a footnote.** `docs/MULTIVERSION_ROADMAP.md` frames it: one
   compiler for ISO 1985→2023 selected by `--standard`. COBOL-85 = Milestone 1; M0 version engine → M2 (2002) →
   M3 (2014) → M4 (2023). Owner decisions: **engine now / features after M1**, and **non-OO before OO**.

Docs: rewrote `docs/COBOL85_COMPLIANCE_PLAN.md` as the M1 execution plan (three axes — baseline / flagging /
spec) and added the roadmap. A **24-agent Wave-1 audit** (read-only fan-out: 10 verify + 9 spec + 5 flag)
produced three ledgers — `EXCLUSION_LEDGER.md`, `SPEC_GAP_INVENTORY.md`, `FLAG_MANIFESTS.md`. Headline:
**real-GAPs = 0.** Every one of the 52 unbaselined live programs is a documented exclusion — verified to ground,
including that the two IC "baseline candidates" (IC115A/IC206A) are `PROCEDURE DIVISION USING` callee-halves, not
missing features. **So M1's executable-baseline axis is already complete.** The genuine remaining M1 work is the
other two axes: **flagging conformance** (~39 `…M` modules whose flag-behavior is unverified) and **spec
completeness** (~92 under-tested features, heaviest in Intrinsics/RW/Environment). 14 workstreams tracked.

