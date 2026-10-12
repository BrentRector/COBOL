## Entry 991 — 2026-07-22 21:12 PDT — §24 V5: EXIT SECTION implemented (with a spec-correction the design doc got wrong)

EXIT SECTION was bound as `BoundUnsupported` (ControlFlowBinder). Implemented it end-to-end: a new `BoundExitSection`
node (with the section's `EndPc`), the SR9 placement check (COBOLNET0827 when not in a section), the two mandatory
source-gen sibling visitors (BoundStores, UsageCollectionPass), and the emit — plus the 2002 introduction gate
(constructs.json `exit-section-2002` + `VersionConformancePass.VisitExitStatement` + `gen-constructs.ps1` regen).

**The scout earned its keep — a spec-correction to the design doc.** `COBOLNET_CONTROL_FLOW_DESIGN` D8 said
"EXIT SECTION → `pc = lastParaInSection+1; break;` (direct port of legacy)". That is spec-WRONG: §14.9.14.4 GR7 says
EXIT SECTION transfers to the section end "preceding any return mechanisms for that section" — it must FIRE the
section's PERFORM/SORT/USE return. A bare pc-move from a MID-section paragraph of a `PERFORM SECTION` leaves the
bounded dispatch's `__atExit` tail-check false (it captures the CURRENT pc, not the section end), so it falls into the
FOLLOWING section. Correct emit: `__pc = SectionEndPc+1; if (__exitPc == SectionEndPc) return __pc; break;` — the
explicit return replicates the tail-check for the section-end case (PERFORM SECTION / PERFORM…THRU the section end /
SORT-or-USE / the top-level end wall). The legacy carries the identical latent bug (net, not authority). D8 was
rewritten clean (spec-derived + a Rejected-alternative entry; no addendum — owner directive).

Tests (`SectionDifferentialTests`, SPEC-PINNED not differential — the legacy shares the bug): mid-section EXIT SECTION
under PERFORM → the PERFORM returns (W2 skipped); top-level → falls through to the next section; SR9 rejection. 10/10.
Gate: version-matrix 1835 (auto-derived reject@85 / accept@2002+ + registry-drift) · characterization 33/33
byte-identical (EXIT SECTION was unused — additive) · full Conformance + GnuCOBOL differential. Noted a sibling gap
(EXIT PARAGRAPH / EXIT PERFORM are also 2002 introductions but likewise ungated — the code-vs-spec audit should catch
it; a §24 follow-up).

