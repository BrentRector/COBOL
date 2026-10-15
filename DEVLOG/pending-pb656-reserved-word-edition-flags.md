## PB656: the 2002/2014 reserved-word flags read against GnuCOBOL; SEND/RECEIVE/END-RECEIVE reserved at 2002, PADDING CHARACTER obsolete at 2002

kb/Work PB656 said `reserved-words.json` and the removed-construct rows disagree: SEGMENT-LIMIT is reserved at 2002
while `segment-limit-removed-2002` removes its clause there. The registrar added that COBOL's provenance ("removed
post-85") contradicts its own r2002/r2014 flags, which left row SR-8.3.2.1-2 PARTIAL.

**Re-probe (worktree build).** `01 SEGMENT-LIMIT` gives COBOLNET0901 at 85/2002, and the clause gives COBOLNET0902 from
2002. `01 COBOL` gives COBOLNET0901 at 85/2002/2014. `01 RECEIVE` compiled at 2002. PADDING CHARACTER compiled
silently at 2002.

**The measurement.** I joined every `removedIn` row against the flags. PADDING already agreed (PB300). SEGMENT-LIMIT
is the only word still reserved at its construct's `removedIn`. The wider sweep found two defects. First, 35 rows were
labelled "removed post-85" although their 2002/2014 flags are set. Second, SEND, RECEIVE and END-RECEIVE had r2002
false while the rest of the communication vocabulary (CD, DISABLE, ENABLE, PURGE, ...) had it true.

**The decision.** The repository holds no 2002 or 2014 text, so CLAUDE.md rule 1's precedence gives these editions to
GnuCOBOL. I surveyed it rather than recalling it: GnuCOBOL 3.2 in WSL, its `cobol2002.conf` and `cobol2014.conf`,
and `cobc -std=…` probes.
- At `-std=cobol2002` GnuCOBOL refuses both the SEGMENT-LIMIT clause and `01 SEGMENT-LIMIT`. A word's reservation
  and its construct's availability are two facts, and nothing in the standard ties them together, so no flag changes.
  The registry row and VCR row 7.8 now record the determination.
- GnuCOBOL refuses `01 COBOL` at 2002 and 2014, so the flags stand and the label was wrong. The generator's
  `$f85 -and -not $f23` branch now names the last reserving edition from the flags it describes.
- `cite.py --check E.1 "substantive changes between the previous COBOL standard and this"` → OK. So Annex E.2 item 25
  speaks about 2014 only. The generator had cleared the trio's 2002 flag on that item. That override is now an
  assertion on the 2014 list, and r2002 is GnuCOBOL's 2002 list (the communication facility, obsolete at 2002).
  `receive-as-user-word` and `end-receive-as-user-word` now carry `introducedIn: 2014`.
- `padding-character-clause` is `obsolete` in `cobol2002.conf`, and cobc warns that it is obsolete. VCR row 7.20 had
  named this as the one-line change: `obsoleteIn: 2002` gives COBOLNET0903 at 2002.

`docs/CONFORMANCE.md` D-RW3 records all four points. VCR rows 7.8, 7.20 and 32 and `VERSION_TEST_MATRIX_DESIGN.md` are
current. The generated outputs (`ReservedWords.Table.cs`, `reserved-words.json`, the construct registry and the VCR
status index) were regenerated, never hand-edited. Before the change, a re-run of the generator reproduced the
committed tables byte for byte.

**Goldens.**
- `conformance:2023/pb656_directive_words_as_user_words` covers §8.3.2.1 2): §8.12 words as system-names,
  data-names, a condition-name, a section-name and a paragraph-name, beside >>DEFINE, >>TURN and >>IF.
- `negative/pb656-cobol-reserved-through-2014` rejects at 85, 2002 and 2014.
- `conformance:2002/pb656_padding_character_obsolete_2002` runs with the warning.
- `negative/user-word-receive` and `negative/user-word-end-receive` now reject at 85, 2002 and 2023.

**Drift test.** `ReservedWordsDriftTests.Provenance_AgreesWithTheFlagsItDescribes` checks each label's claim about
an edition against that edition's flag. Its predicate flags 35 rows on the previous table and none on the
regenerated one. `Table_SanityInvariants` had encoded the old 2002 override. It now counts the item-25 words as those
unreserved at 2014 and the three re-reservations as reserved at 85 and 2002.

Row SR-8.3.2.1-2 moved from PARTIAL to CONFORMS, and GAP went from 101 to 100.
