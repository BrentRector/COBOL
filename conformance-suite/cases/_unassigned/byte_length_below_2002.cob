*>--- conformance-case
*> id: byte_length_below_2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.14 BYTE-LENGTH is a COBOL-2002 introduction (byte-length ≠ FUNCTION LENGTH, D7;
      *> PHASE-11-scout-notes.md spec:byte-length). Below 2002 the D8 catalog window rejects the reference
      *> BY NAME — COBOLNET1502 (IntrinsicBinder window gate).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11BLW85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(5).
       01 N-2  PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N-2 = FUNCTION BYTE-LENGTH(WS-X)
           STOP RUN.
