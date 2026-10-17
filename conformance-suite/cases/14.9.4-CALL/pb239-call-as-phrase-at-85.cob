*>--- conformance-case
*> id: pb239-call-as-phrase-at-85
*> rules: [FMT-14.9.4.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB239 (the FMT-14.9.4.2 re-measure) - the CALL AS phrase
      *> selects ISO 14.9.4.2 Format 2, the program-prototype CALL, which
      *> arrived in COBOL-2002 with program prototypes (8.9 reserves NESTED
      *> from 2002). An 85 compile accepted this program silently; it is now
      *> an introduction gate (COBOLNET0900). Nothing else here is post-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239A85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "ABCDEF".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB239A8I" AS NESTED USING X
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239A8I.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(6).
       PROCEDURE DIVISION USING L.
       MAIN.
           EXIT PROGRAM.
       END PROGRAM PB239A8I.
       END PROGRAM PB239A85.
