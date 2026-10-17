*>--- conformance-case
*> id: test_validators_below_2002
*> rules: [RV-15.93.4-1, RV-15.95.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.90 TEST-DATE-YYYYMMDD / §15.91 TEST-DAY-YYYYDDD / §15.93 TEST-NUMVAL / §15.94
      *> TEST-NUMVAL-C / §15.95 TEST-NUMVAL-F are COBOL-2002 introductions (§15.90/§15.91 carry direct
      *> in-spec 2002 attribution, D.31.3.1; PHASE-11-scout-notes.md spec:validators). Below 2002 the D8
      *> catalog window rejects each reference BY NAME — COBOLNET1502 (IntrinsicBinder window gate).
      *> ⚠ TEST-NUMVAL-F was the one member of the quartet this fixture did not name, so its gate was
      *> unmeasured while its three siblings' was pinned (kb/Work PB256 — the edition-gate half of the
      *> §15.95 rows' evidence).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11TESTV85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R2 PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(20240229)
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(2024366)
           COMPUTE R2 = FUNCTION TEST-NUMVAL("123.45")
           COMPUTE R2 = FUNCTION TEST-NUMVAL-C("$1,234.56")
           COMPUTE R2 = FUNCTION TEST-NUMVAL-F("1.5E+3")
           STOP RUN.
