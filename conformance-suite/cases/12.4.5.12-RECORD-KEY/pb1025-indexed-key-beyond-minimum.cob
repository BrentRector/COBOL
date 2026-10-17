      *>--- conformance-case
      *> id: pb1025-indexed-key-beyond-minimum
      *> rules: [SR-12.4.5.12.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1025 - ISO 12.4.5.12.3 SR4: "If the indexed file
      *> contains variable-length records, data-name-1 and each
      *> data-name-2 shall be contained within the first n bytes of the
      *> record, where n equals the minimum record size specified for
      *> the file." The minimum here is 10 and the RECORD KEY occupies
      *> bytes 11-12, so the entry is rejected. The rule was unenforced:
      *> the key was sliced from past the end of a 10-byte record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1025NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1025neg.idx"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS FK.
       DATA DIVISION.
       FILE SECTION.
       FD F
           RECORD IS VARYING IN SIZE FROM 10 TO 50 CHARACTERS.
       01 FR.
          05 FH PIC X(10).
          05 FK PIC X(2).
          05 FT PIC X(38).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           CLOSE F
           STOP RUN.
