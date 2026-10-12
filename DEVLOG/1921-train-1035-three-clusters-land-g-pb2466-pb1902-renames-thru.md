## Entry 1921 — 2026-10-07 14:25 PDT — Train 1035: three clusters land (G PB2466+PB1902 RENAMES THRU parts in storage bytes, A PB833 the §9.1.15 file lock across run units, H PB1939+PB2004 OCCURS DYNAMIC elements image-promoted); PB2484–PB2485 filed; GAP 155 → 153

Wave 1033's groups G, A and H, in that order, one commit each and this entry in the train commit. The train was cut on
train 1033 while train 1034 was in CI, gated there, then rebased onto train 1034 once it landed and gated again.

**G — PB2466 + PB1902: a RENAMES THROUGH span part is a range of STORAGE BYTES.** A REDEFINES view of a different layout
can put a window boundary on the odd byte of a national character (PB1902), or leave an interior byte that only half of
a national character covers (PB2466). Parts were kept in their leaf's character positions, so `RenamesSpanPart.FromBytes`
returned null for such a byte and legal source drew COBOLNET1655 "do not tile". §13.18.45.4 GR2 makes data-name-1 "an
alphanumeric group item" over the record's storage, and §13.18.45.3 SR10 asks only for an integral number of bytes.
Parts are now `(Leaf, Occurrence, StartByte, LengthBytes, Outer)`; `FromBytes`, `Bytes` and `BytesPerPosition` are
gone and `Positions` is the one conversion back to character positions. `ReferenceResolver.SpanLeafPlace` composes a
usage-national cell through the new `NationalBytesPlace` decorator (UTF-16BE pairs, D-N1), a partial part is a byte
`RefModPlace` over it, and the renderer's private national transcoding for the alias is deleted, so one decorator owns
it. The tiler's second copy of the partial-part predicate went too. Both re-probes reproduced on the implementer's
build and print the byte-exact layout after. Golden `2002/pb2466_renames_half_national_byte` covers an interior half
character, a boundary half character and half of a table cell, each read and written. GR-13.18.45.4-2 gained a
witness and stays CONFORMS. The implementer flagged an overlap with the unlanded wave-1032 A branch (`7861c89ef`), which
edits the same tiler; that branch is not in this train, and its successor must keep this side of `Consider`.

**A — PB833: §9.1.15's file lock against other run units is Table 19 itself.** The note said Linux and macOS cannot
express rule 2 ("restricts concurrent access … to input mode") because .NET maps every `FileShare` but `None` onto one
`flock` `LOCK_SH`. The re-probe went wider than the note. `RunUnitFileLockDriftTests` opens one file in one
`FileRegistry` and then in a second (no shared `PhysicalFileTable` and no shared handle, so a second run unit) over
every sharing spelling × open mode × spelling × mode × organization, 768 pairs, and asserts '61' exactly where
`Table19.Conflicts` refuses. It was 15 red on WINDOWS: `OPEN OUTPUT` in a second run unit answered '00' and emptied a
file the first held `SHARING WITH ALL OTHER`, because a share mode refuses by access while Table 19 prints Unsuccessful
in every OUTPUT cell. On Linux it was red wherever a lock must admit a reader and refuse a writer. The note's sketch
(read lock for READ ONLY, write lock for NO OTHER) had already been refuted. `RunUnitFileLock` publishes the
connector's Table 19 COLUMN as a shared one-byte region lock (offset 2^62 plus a wire number per column) and then tests
the columns its request ROW refuses (`Table19.Cell`) for another holder. Publishing first means two simultaneous OPENs
cannot both pass, and one lookup answers both the in-run-unit and the cross-run-unit question. Linux open-file-description
locks (`OfdRegionLocks`, `F_OFD_SETLK`/`F_OFD_GETLK`) carry it: classic `fcntl` locks and `FileStream.Lock` belong to
the process and vanish when ANY descriptor of the file closes, and an OFD lock never reports the asker's own. The first
Linux run found a 1-in-hundreds spurious '61' from the fork window (a `fork` in another thread holds a copy of the
descriptor until its `exec`), fixed by unlocking explicitly before the close. `FileConnector.Open` takes the lock once,
above the organizations: before `OpenCore` on an existing file (§14.9.27.4 GR25), after it on a created one. CLOSE and
an unsuccessful OPEN give it back. On Windows (`HostFile.ShareModesAreMandatory`) an OUTPUT open of an existing file first
asks `HostFile.IsHeldByAnother`, which closed the Windows gap; it is the probe DELETE FILE already uses, so a non-COBOL
reader holding the file (Annex A.1 item 75's implementor-defined interplay) also refuses the truncation. Capability is
measured, never assumed. macOS has no region lock and DOC-A.1-75 says so. GR-9.1.15-2 PARTIAL → CONFORMS and
DOC-A.1-75 DIVERGES → CONFORMS, GAP −2. No single COBOL program can observe a second run unit, so there is no golden;
the 768-pair theorem, `TheCloseGivesTheFileLockBack` and a 200-round barrier race are the witnesses on both hosts. The
implementer's first gate had 8 `UniversalCrossingShapeDriftTests` cases NEVER RAN; that is PB2283's accounting flake,
whose second occurrence is now in that note.

**H — PB1939 + PB2004: an OCCURS DYNAMIC element is image-promoted by the same rule as any leaf.** `StorageFormPass.Classify`
wrapped a dynamic element's BASE form and skipped the promotion every other leaf takes, and `UsageCollectionPass`
skipped `DynTablePlace` as a character channel and as a whole-group operand, so a numeric element could hold only a
value. §14.6.9.2 2) and §14.6.9.4's space fill read zeros (PB1939); `-5` `REPLACING "5" BY "0"` lost §14.9.22.4 GR4 d)'s
retained sign and read `00{` for `00}` (PB2004). The sibling sweep found four more on the pre-fix build: `MOVE SPACES`
to a group element read `000`, `MOVE G TO DN (1)` stopped the run unit as a Tier-C island, a BY REFERENCE fill from a
group read `000` (§14.2.3 GR8), and a ref-mod store read `013` for `1X3` (§8.4.3.3.4 GR6). The fix: one `Promoted`
helper for a leaf and a dynamic element (`CobolDynTable<string>`), `DataItem.StoreAsImage` answering per occurrence,
the two `DynTablePlace` skips removed, `InspectEmitter`'s native-value dynamic arm deleted, and the group-to-elementary
MOVE asking the operand's `Place.ImageCapable`. There is no new mechanism; two skips and an arm are gone. Goldens
`2014/pb1939_dyn_numeric_element_spaces` and `2014/pb2004_dyn_element_character_channels`, each with a 2002 negative;
`2014/dyn_nested_group_move.out` had encoded the defect (`B5=[00]`, now `B5=[  ]` like its fixed-table twin).
GR-14.6.9.2-2 and GR-14.9.22.4-4 gained witnesses and stay CONFORMS.

**The train.** First gate, on train 1033: `=== BUILD-LOCAL GATE: GREEN — Conformance 10,898/10,898 · Unit 32,493/32,493 ·
Characterization 35/35` (run `20261007T205015Z-a81b42`); Linux gate GREEN. The rebase onto train 1034 had one
conflict outside docs: adjacent whole elements of `tests/conformance/2014/manifest.json` (train 1034's
`pb2280_call_variable_length_shapes`, H's two goldens), both kept, 211 → 213. All three verdict batches re-applied to
the merged inventory changed 0 rows. Re-gate on the rebased tree: `=== BUILD-LOCAL GATE: GREEN — Conformance
10,914/10,914 · Unit 32,507/32,507 · Characterization 35/35` (run `20261007T210839Z-9edbf9`); Linux gate GREEN; semgrep
counts equal the baseline; the CI audits are clean. Oracle against `bc1b9318fa82`: DIFFERENT, 10 of 7,435, all
attributed. The 5 CSHARP cases are H's (`dyn_nested_group_move`, `l1_vldi_reduce_regrow`,
`pb1128_inspect_dynamic_table_element`, `pb1144_dyn_move_elements`, `pb1144_dyn_move_recreate`): a promoted numeric
dynamic element becomes a string image carrier (`CobolDynTable<long>` → `CobolDynTable<string>`, record-struct members
`long` → `string`, `FormatImage`/`ParseImage` moving onto the element's stores and reads, INSPECT taking the image
path). That is the fix. G changed no existing case (the decorator renders `NatBytes(Read(x))` exactly as the deleted
arm did) and A is runtime-only. 5 ADDED are the goldens and negatives. The baseline is re-recorded as `90babefc36a7`
(H's commit). Review over the train's diff: 0 findings. The lander weighed one risk and kept it: on Windows a benign
reader such as an antivirus scan now refuses a re-OPEN OUTPUT with '61', as it already refused DELETE FILE.
Leads filed: PB2484 (macOS has no cross-run-unit lock) and PB2485 (no witness uses two real processes). No diagnostic
codes were claimed. GAP 155 → 153.

