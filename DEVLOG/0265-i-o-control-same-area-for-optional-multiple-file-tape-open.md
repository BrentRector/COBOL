## Entry 265 — I-O-CONTROL: SAME (AREA/FOR optional) + MULTIPLE FILE TAPE + OPEN REVERSED → SQ206A

Three I-O-CONTROL / OPEN parse forms (all storage/tape hints — parsed, no semantic effect on disk files):

- **SAME clause (§12.4.6.4)** — the `sameClause` required the word `AREA`, rejecting SQ206A's
  `SAME SQ-FS1 SQ-FS2`. Per the spec format (Format 1 `<u>SAME</u> AREA FOR file-1 …` — only SAME is
  underlined) **AREA and FOR are optional words**, so the SAME AREA clause may be written `SAME file-1
  file-2`; this is spec-conformant, not a leniency. Reworked to `SAME (RECORD | SORT | SORT-MERGE)? AREA?
  FOR? fileName (COMMA? fileName)*` (added a `SORT-MERGE` token). Also: the I-O-CONTROL paragraph holds
  multiple clauses terminated by **one** period (§12.4.6) — SQ206A writes two SAME clauses before a single
  period — but `ioControlParagraph` required a period after each clause. Reworked to
  `I_O_CONTROL DOT (ioControlClause DOT?)*` (period optional after each clause).
- **MULTIPLE FILE TAPE clause** (obsolete — removed from later standards): added `MULTIPLE`/`TAPE`/`POSITION`
  tokens + `multipleFileClause : MULTIPLE FILE TAPE? (CONTAINS? entry (COMMA? entry)*)?`, parsed and ignored
  (a multi-files-per-reel hint, irrelevant to disk).
- **OPEN … REVERSED / WITH NO REWIND** (obsolete tape positioning, §14.9.25): the `openClause` was
  `openMode dataReference+`, so `OPEN INPUT TFIL REVERSED` parsed REVERSED as a (missing) file → "not a
  declared file". Added `openFileSpec : dataReference (REVERSED | WITH? NO REWIND)?` (new `REVERSED` token);
  `BindOpen` and `ReferenceResolver` read the file from each `openFileSpec`, ignoring the tape phrase.

**SQ206A** (SAME AREA + SAME RECORD AREA): **4/4, 0 FAIL\*** — all auto-verified; the record-area sharing the
test exercises works without special storage handling. Baselined. **SQ303M** now compiles (MULTIPLE FILE
TAPE + OPEN REVERSED parse) but is a **flagging-conformance module** — its PROCEDURE is just
`OPEN INPUT TFIL REVERSED. CLOSE TFIL. STOP RUN.` with "Message expected: OBSOLETE", no CCVS report (0 bytes
output) — so it is excluded from the guard like IF401M/402M/403M (the parse-form fix is the deliverable).

Guard ALL GREEN: 1000 unit / 347 integration / **224 NIST** (94 NC + 42 IF + 12 SM + 4 IC + 51 SQ + 19 RL +
2 IX), 0 regressions — the OPEN-grammar change (`openClause` now `openMode openFileSpec+`) touches every file
test, all unchanged. SQ401M remains COMPILE_FAIL (further non-conforming clauses) and is a flagging module.

