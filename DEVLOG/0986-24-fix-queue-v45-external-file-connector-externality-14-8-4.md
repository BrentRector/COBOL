## Entry 986 — 2026-07-22 14:55 PDT — §24 fix-queue: V45 EXTERNAL file-connector externality (§14.8.4.2 conjunct 1) — both faces LANDED

New batch after the EC-seam batch: the ref-mod-externality residue. ISO §14.8.4.2 has TWO conjuncts for an external
file connector's FILE STATUS / RELATIVE KEY / LINAGE items: (1) they SHALL BE external data items (externality); (2) they
shall refer to the same corresponding storage across runtime elements (consistency). Only conjunct 2 was enforced.

**V45-externality (compile-time, `BinderDriver.CheckExternalFileConsistency`):** a per-connector externality conjunct is
now enforced ABOVE the `conns.Count < 2` early-out, so a LONE-program external file whose FILE STATUS / RELATIVE KEY /
LINAGE data-name operand is non-external is rejected (it was silently accepted before — the early-out skipped
single-describer connectors entirely). ONE shared clause-parameterized diagnostic **COBOLNET1624** (the §14.8.4.2 sentence
names all three items together), inheriting the sibling checks' 2023-gate + Removed-freedom `severity` (Error strict /
Warning permissive — per Annex E.2 item 9, the external-item conformance-checking mechanism is the 2023 addition). The
count<2 early-out is KEPT, now guarding only the unchanged 1573/1575 consistency conjunct.

**V45-sentinel (runtime, `ExternalTable.DataMismatch`):** the check compared refs with plain `!=`, so two cross-compiled
non-external describers both carrying the `"!"` sentinel compared EQUAL and missed the violation. Fixed with an OR-prefix
`AnyNonExternalRef(prior) || AnyNonExternalRef(desc)`, where `IsNonExternal(r) => r?.Split(';').Contains("!")` treats
`"!"` as a whole `;`-token (so an embedded LINAGE token counts; a dotted identity or an `"=<int>"` literal never contains
`"!"`/`";"`). A null ref (clause unspecified) never raises.

**The design-validation Workflow (`wf_08814087-0e0`) made the load-bearing scope correction:** the initial "add the LINAGE
leg to BOTH conjuncts" was WRONG — adding LINAGE to the count≥2 CONSISTENCY conjunct would compile-reject
`ec_external_data_mismatch.cob`, whose whole design routes its LINAGE mismatch to the RUNTIME EC. So LINAGE goes into the
EXTERNALITY conjunct ONLY; the proposed COBOLNET1625 was dropped. The in-group **LINAGE consistency (§13.4.5.4 GR2(c)) is a
separate LONGSTANDING (85/2002/2014, absent from Annex E.2) always-Error requirement** not enforced at compile time today —
recorded as a NEW §24 fix-queue item (V51), NOT folded into V45 (which is 2023-gated externality).

Gate (wave-local): CLI-probed all five compile legs (FILE STATUS / RELATIVE KEY / LINAGE non-external → COBOLNET1624
Error at strict, Warning at --permissive; FILE STATUS + LINAGE positive controls clean) · 3 negatives
(`external-{file-status,relative-key,linage}-externality-at-2023`, .err COBOLNET1624) + 2 positive controls
(`external_{file_status,linage}_externality_ok`) + 4 `ExternalTableTests` (both-"!" FILE STATUS, embedded-"!" LINAGE token,
matching-external + unspecified no-raise) · characterization **33/33 byte-identical** · greenfield Unit **575/575** ·
external Conformance 30/30 (incl. `ec_external_data_mismatch` unchanged — the golden the mistaken fix would have destroyed)
· legacy Integration 186/186 (the 2 positives compile on the frozen legacy compiler — clean external files, no exclusion).
DIAGNOSTICS.md regenerated. The comprehensive gate runs before merge.

