*>--- conformance-case
*> id: external_constant_record
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXTCONREC.
      *> VCR 16 — below COBOL-2023 the "EXTERNAL CONSTANT RECORD requires a
      *> strong TYPE" requirement (ISO 13.16.3 SR13 para 2; Annex E.2 item 10)
      *> does NOT apply: a bare external constant record (no TYPE) is the
      *> legacy accepted form. Per 11.9.10.4 GR7 a CONSTANT RECORD is the one
      *> external item initialized at initial state (unlike a plain external
      *> item, whose VALUE takes effect only via INITIALIZE, 13.18.63 GR4a),
      *> so A initializes to its VALUE and DISPLAY prints it. The reject-at-
      *> 2023 leg (COBOLNET1549) is pinned by the negative corpus.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R IS EXTERNAL CONSTANT RECORD.
          05 A PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY A OF R.
           STOP RUN.
