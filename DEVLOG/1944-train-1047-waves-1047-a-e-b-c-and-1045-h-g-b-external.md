## Entry 1944 — 2026-10-08 21:25 PDT — Train 1047: waves 1047 A, E, B, C and 1045 H, G, B (external repository slice 1)

**Train 1047** carries eight branches in one landing: seven code clusters and N, the previous land unit's
kb/Work-only branch records. That is one over the 4-6 band, on purpose: the land unit carries every ready branch
(O11). Each cluster came in as a patch from its branch. 45B, the largest cluster (71 files, about 5,100 lines, on the
oldest base, 685b44315), came in last. The history is one commit per cluster, then each cluster's merge, gate and
review fixes as its own commit, then the notes. All landing, nothing ejected.

**47A — PB2511 + PB2465 + PB1965** (wave 1047 A; it carries wave 1045 A's PB2511 and PB2465 commits, re-gated, and
supersedes `worktree-wf_03fdfe4f-4ae-1`, every line of which is on the train head). PB2511: a Format 2 SORT whose
table subject is reference-modified is refused (§8.4.3.3.3 SR5 NOTE). PB2465: a constant's LENGTH OF / BYTE-LENGTH
OF measures its operand's COMPLETED description (`CompleteDescriptionAhead`). PB1965: every COBOL word key is the
Annex C fold (`CobolNames.UpperFold`), never the host's `ToUpperInvariant`. **Review: two confirmed defects, both
fixed in the train.** (1) PB2465's early completion replaced an entry's PICTURE profile before the pipeline expanded
SAME AS. `01 S SAME AS E.`, with E under GROUP-USAGE NATIONAL and measured by a constant first, copied E's
inherited usage: `BYTE-LENGTH(S)` printed 6. Main printed 3, and §13.18.49.4 GR1/GR3 give 3. Clause copies now go
through `CopyClauseDescription`, which restores the written profile. The new golden
`2002/pb2465_same_as_after_length_constant` prints `KE=6 KG=6` / `S=3 E=6 T=6` (main printed `KE=3 KG=3`, the
PB2465 defect). (2) The PB1965 sibling: `PointerImage.NameBase` keyed pointer images by `ToUpperInvariant`. It now
uses `UpperFold`, the fold ProgramTable compares by. Two more review findings: a member of a TYPE'd item cannot be
named in `CONSTANT AS LENGTH OF M OF V` (COBOLNET1547 on main and the train, rejects legal source), and the new
fold drift test does not scan the runtime. Both need ids (below). Batches w1045a-pb2511, w1045a-pb2465 and
w1047a-pb1965 re-applied on the merged tree changed 0 rows. PB1941 and PB2051 stay open.

**45H — PB1951 + PB1956** (wave 1045 H). The Format 2 table SORT of a BASED/EXTERNAL table gathers every managed
slot at every inner OCCURS occurrence (`SlotWindow.InnerLevelsOf`), and an EXTERNAL element holding variable-length
components moves as its component carrier (§14.9.40.4 GR24). PB1956 is retired: report writer statements are not
§9.1.13.1 I-O statements. Review: no wrong answer. Its latent `DynMaxOf` defect is filed as PB2833.

**47E — PB1928 + PB1935 + PB2468 + PB2734** (wave 1047 E). The changes: parenthesized ZERO; a compound or EVALUATE
operand binds as the arithmetic expression it is (EvaluateBinder's copy deleted); COBOLNET3317 for a
reference-modified condition-name; boolean-function routing through parentheses. **Review: one confirmed defect,
fixed in the train.** The class condition is a consumer of the comparison operand that the fix had not reached.
`IF (N) IS NUMERIC` over "AB " answered TRUE from the decoded value (it used to test N's characters), and
`(N) IS FARTHEST-FROM-ZERO` compiled to a run-time abort. §8.8.4.4.2 prints identifier-1, so an arithmetic-expression
subject is now **COBOLNET3318** (47E's range). Function-identifiers and counter registers stay admitted: the first fix
refused LINAGE-COUNTER, and gate 2's `pb1401_computed_numeric_*` goldens caught it. There are two new negatives.
The train also landed **PB1085**: 47E had re-derived the inherited `§12.3.8.2 GR12` everywhere but in
`OoNameResolution.cs`. That is now §12.3.8.3 SR5/SR8 (cite.py OK), and the COBOLNET1501 message's citation is
pinned. Batch t1047-pb1085 moves GR-12.3.8.4-12 to CONFORMS.

**47B — PB2692 + PB2748 + PB2751** (wave 1047 B). Sequential record locks are published across run units (§9.1.16).
A re-OPEN answers '41' first. Physical-file identity is the absolute path under the host case rule. Keyed
WRITE/REWRITE test '44'/'91' after the open mode and before the keys. The merge kept train 1045's PB2693 store format
4 and the dispose-on-failure OPEN in `SequentialConnector.OpenReader/OpenWriter`; `_handle` is set only after the
reader or writer is built. Review: no blocking defect. The `ReadImage` comment overclaimed the '30' mapping and is
corrected; the in-statement reload is filed as PB2842. Four PLAUSIBLE runtime findings are in the report.

**47C — PB2761 + PB2765 + PB2772** (wave 1047 C). CLDR tailoring takes the relation position before the extension,
resolves the exact `[import]` type along the chain, and closes canonically once, in `CollationTable.Rebuild`.
**Review: one confirmed defect, fixed in the train.** Closure step 1 mapped a tailored Hangul syllable's jamo
spelling, but the engine decomposes a syllable before the table, so 가 and ᄀ+ᅡ collated apart. A syllable key is
now passed over (ICU refuses one), and a new unit test pins LV and LVT.

**45G — PB2697 + PB2647** (wave 1045 G). The SET amount at a negative scale, the shared `CobolPtr.Displace`, and SET
SIZE GR37 for NaN. Review: nothing confirmed. CONFORMANCE.md now records the non-finite reading. The CONTINUE AFTER
sibling is filed as PB2841.

**45B — PB2097** (wave 1045 B, slice 1 of DESIGN-external-repository §21). The module namespace, global::-rooted
emitted names, `[assembly: CobolRepository]`, `EnsureRegistered` over `ProgramTable.RegisterModule`, and RuntimeAbi.
Merge: SortEmitter took 45H's new emission global::-rooted. **PublicAPI.Shipped.txt was its base's surface**: on the
train tree, 52 members were RS0016 and 39 were RS0017, and the build failed. It is re-baselined to the merged surface
(nothing has shipped; slice 1 establishes the baseline), and the RuntimeAbiPinDriftTests runtime hash is re-pinned
(F9D6ECD87823E9B7; the call-ABI hash held). Gate reds owned by 45B: `CobolRepositoryAttribute.cs` was in no R2
subsystem, and `CancelCascadeOrderTests` (added on main after 45B's base) registered outside `RegisterModule`.
Review: the cross-module `-`/`_` namespace collision is the same non-injective mapping as PB2839 (evidence added
there). The declared-name path collision is PB2843. Two smaller findings are in the report.

**N** — the eight train-1045 member branches recorded ABANDONED in their lead notes. Notes this train:
PB2833-PB2836 and PB2838-PB2843 filed (PB2836 probed: it did not reproduce, so it is a retired analysis; PB2837 is
PB1085, unused). The owner's 18:28 PDT approval of PB2693's format 4 is recorded in PB701 and PB2693. PB2815 is
retired as a duplicate of PB2806.

**The train.** Whole-population gate runs:
- Run 1: RED, four Unit reds, attributed by the failure message and by the cluster whose diff deleted or added the
  named member: R2 hole and CancelCascadeOrderTests (45B); PredefinedNullContextDriftTests (47E); inventory
  code-locations IsBooleanUserFunction (47E) and ExistingRecordCount (47B). The stale code-locations were retired with
  batch t1047-resite.
- Run 2: RED on the review fixes themselves: the class screen refused counter registers; DescriptionCopyCompleteness
  needs the copy static; the witness-loss audit needs retirements, not edits.
- Run 3: GREEN, Conformance 11,319/11,319, Unit 33,027/33,027, Characterization 36/36.

The Linux gate is GREEN (all four legs). Semgrep counts are unchanged (3/44/2/268). GAP 104 → 103.

**Oracle: DIFFERENT, 3,415 of 7,689, every case explained by class:**
- 3,368 emitted programs differ only by 45B's class (`namespace Cobol.<S>`, global::-rooted names, the registration
  member, `Register()` private). Normalized full-file comparison: identical.
- 1 is PB1965 (`_PRG_PRüFUNG`: the Annex C fold raises only basic letters).
- 1 is a 45H comment.
- 13 negative/matrix diagnostics: the COBOLNET0900 citation suffix, §12.3.8.2 → §12.3.8.4 GR12 (47E).
- 32 added cases: the new goldens.

The baseline is re-recorded at e345db425878.

