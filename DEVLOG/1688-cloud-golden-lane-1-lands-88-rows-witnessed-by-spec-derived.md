## Entry 1688 — 2026-09-24 18:18 PDT — Cloud golden lane #1 lands: 88 rows witnessed by spec-derived goldens (GAP 1627 → 1538)

**What.** The owner authorized overrunning the day's plan allowance to spend the remaining cloud credit on significant
COBOL work; a fresh credit-billed orchestrator (session_01NEXbLrn1FYhz3CpJvSuNr6) ran golden lane #1 on
`claude/golden-lane-1` over the 397 rows verdicted CONFORMS with no witness (393) or DOCUMENTED-NON-SUPPORT owing one
(4), grouped into 39 subject-family inputs. Per batch: a writer derived each expected output FROM THE SPEC before
running the compiler (cite.py on every citation), compile-ran it, and kept any spec/compiler disagreement OUT of the
tree as a defect; a refuter re-derived every passing golden; only upheld goldens were integrated, recorded with
`record_verdicts.py`, gated (Conformance Corpus|Negative|VersionMatrix|Intrinsic — 5548/5548 at the last push, each
new golden confirmed RAN by name — and the Unit inventory/manifest/AnnexA1Register filter) and pushed per batch.
**Result: 88 rows witnessed (wave 1: 47 in misc-p1..p6; wave 2: 41 in misc-p7, p8, p9, p10, p12) by 101 new goldens,
GAP 1627 → 1538.** `docs/CONFORMANCE.md` §7 `Pinned by` cells name the new DOC-A.1 witnesses (rows 6, 8, 21, 32, 43,
44, 72, 74, 95, 97) — `AnnexA1RegisterDriftTests` demanded it twice. One integration slip was caught before push: the
integration script matched refuter verdicts by rule id, and one rule had two negatives with different verdicts, so an
overturned negative (`l1c09-factory-implements-missing-method`) landed first; it was removed and the row re-recorded.

**Stopped on budget.** At $50 the orchestrator was told to push wave 1 at once and stop after wave 2; at $22 to stop
now. Batches misc-p11 and waves 3+ (≈ 30 input files, ~300 rows) remain; the wave-2 drafts not integrated are in
`adjudication/golden-lane-1/scratch/` (the pushed snapshot) and `LANDING.md` lists them, so a later session resumes
from there. Credit: $96 → $21 (~$75 for 88 closed rows, ~$0.85 per row).

**Findings.** PB1547 (ADDRESS OF as a receiving operand is rejected with the wrong diagnostic, COBOLNET0901 instead of
§8.4.3.11.3 SR5). PB1523 extended (a GLOBAL clause on a REDEFINES subject crashes the backend). Leads listed in
LANDING.md: INVOKE … "NEW" accepted on a class without INHERITS FROM BASE while INHERITS FROM BASE is refused
(COBOLNET0821); a national STRING receiver; a re-INITIATEd report joining its first line to the previous one.
**Verified here.** The national-STRING-receiver lead reproduces on the merged tree (`NotImplementedCobolFeatureException: STRING INTO receiver ... (usage display required, ISO 14.9.43.3 SR1)` for a legal all-national STRING) - already owned by open note PB1179, whose message also misquotes SR1.

**Gate (merged tree).** Build green; Conformance `Corpus|Negative|VersionMatrix|Intrinsic` **5547/5547** (the cloud saw 5548 before it removed the overturned negative); Unit `SpecTraceabilityInventory|DefectiveRowCoverage|Manifest|AnnexA1Register|WorkRegister` **25/25**; new goldens discovered by name (`l1c01_alt_key_equality_collating`, `l1c01_group_start_typed_national_bit`, `l1c01_native_ordinal_positions_wide` listed). Plan section 0: GAP 1538.

**Landing.** Merged locally (no conflicts); gate on the merged tree reported below; full matrix in push-main's CI.

