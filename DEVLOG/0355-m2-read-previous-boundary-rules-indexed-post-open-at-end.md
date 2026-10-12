## Entry 355 — M2: READ … PREVIOUS boundary rules (indexed) — post-OPEN AT END + post-START-EQUAL inclusive

M2 #10. READ … PREVIOUS (reverse sequential, COBOL-2002) had two wrong boundary behaviors in
`IndexedFileHandler.ReadPrevious`, both fixed using the existing `_readNextInclusive` flag (set by START, cleared
by OPEN/READ):
- **Post-OPEN** (ISO §14.9.30 GR21 d.3 / §9.1.13.4; Annex E.2 item 22): a READ PREVIOUS with no FPI established
  (`_currentKey == null`) must raise the AT END condition (status 10). It returned the highest-key record.
- **Post-START KEY = EQUAL k** (GR21 d.2): the first READ PREVIOUS must return the record at the FPI (key ≤ k,
  i.e. k itself), not its strict predecessor. Added an inclusive branch (when `_readNextInclusive`) returning the
  record AT `_currentKey`.

Tests (`SpecFixTests`): `ReadPrevious_AfterOpen_RaisesAtEnd` (OPEN INPUT + READ PREVIOUS → "ATEND 10"),
`ReadPrevious_AfterStartEqual_ReturnsTheEqualKey` (START EQUAL 20 + READ PREVIOUS → "GOT 20", not 10). Runtime-only
(READ … PREVIOUS already parses + flows ReadDirection.Previous → handler.ReadPrevious); the RelativeFileHandler
analog and the cobol85-rejection flagging are follow-ups.

Guard ALL GREEN: **1047 unit / 433 integration (+2) / 364 NIST**, 0 regressions — but a guard RE-RUN was needed:
ReadPrevious_AfterStartEqual flaked once on parallel-load (passes 3/3 in isolation), the SECOND such transient
file-I/O flake this session (cf. FileIO_Start, DEVLOG 352). A guard-reliability hardening (limit integration-test
concurrency) is the next task.

