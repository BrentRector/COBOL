## Entry 893 — 2026-07-18 14:52 PDT — PHASE-13 EC-BOUND-OVERFLOW (§8.5.1.9.6 GR1) + owner improvements I1–I4 adopted

Two threads landed. (a) The owner reviewed a staged `COBOL_NET_Rearchitecture_Plan_v2.0.md` — rejected (it proposed a
lowered SSA IR + Reflection.Emit-primary CIL that contradict the locked no-lowered-IR / Roslyn-primary / Mono.Cecil-P16
decisions, and misstated the project as "end of Phase 3" when it is at P13/17 with 3634 conformance green); the owner
deleted it. Four refined owner proposals (I1 bound-tree backend-neutrality gate, I2 ICE-free robustness gate, I3
edition-delta coverage report, I4 negative-corpus edition-boundary coverage gate) were evaluated — all sound,
correctly anchored, zero architecture impact — and folded into the roadmap §7 with mechanism refinements
(I2 generate-not-commit; I4 an [Ignore] stub is skipped→green so the gate must be a coverage assertion) + a
reconciliation the review surfaced (the backend seam-proof is PHASE-16 M0 per DESIGN-backend-abstraction.md, NOT the
roadmap-§3 "PHASE-07 Step 13" — P07 closed at Step 12; §3 corrected). Commits `ed491430`/`97254b2e`.

(b) EC-BOUND-OVERFLOW (§8.5.1.9.6 GR1, nonfatal): a dynamic-capacity table's IMPLICIT growth first crossing its
expected capacity (TO) now sets EC-BOUND-OVERFLOW under CHECKING ON. GR1's "already exceeded before an implicit
change ⇒ no exception" is the `_count <= exp` guard (first-crossing only) in `CobolDynTable.RefReceiving`; GrowTo
stays the pure primitive. Added the nonfatal ambient gate `BoundOverflowChecking`/`BoundOverflowError` (mirrors the
DataConversion twin) and GENERALIZED `EcEmitter.EmitChecked` to wrap for the SET of nonfatal ambient gates
{EC-DATA-CONVERSION, EC-BOUND-OVERFLOW} — byte-identical for the existing case (characterization 33). `EcBinder.EcWrap`
adds it conservatively (scout-sanctioned: the raise fires only at a real dyn-table receiving grow-past-expected).
Catalog IntroducedIn 2002→2014 (dynamic tables are §8.5.1.9, COBOL-2014; observably inert). Golden
`tests/conformance/2014/ec_bound_overflow` (FROM 2 TO 4: grow-to-3 no-EC → grow-to-5 first-crossing EC → SET LAST
EXCEPTION TO OFF → grow-to-7 already-exceeded no-EC). Battery: conformance 3634 (+1) · unit 311 · characterization 33.
The EC-BOUND-REF-MOD raise (the fatal twin — §8.4.2.3, the audit's §8.4.2.4 was wrong) is next; its raise needs NO
RefModPlace shape change (the REF-MOD-ZERO-LENGTH directive that does is a split-out follow-on).

Process: owner directed a faster cadence — tiered testing (wave-local targeted gate ~2min; full battery + legacy guard
per batch and mandatory before merge / the P15 legacy cut) + batched greenfield waves, one comprehensive run per batch.

