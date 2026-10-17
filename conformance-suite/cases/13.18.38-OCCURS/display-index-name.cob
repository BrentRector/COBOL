*>--- conformance-case
*> id: display-index-name
*> rules: [SR-13.18.38.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R16 (ledger F11) - an index-name is not an identifier (ISO 8.4.3.1.2) and DISPLAY is
      *> none of 13.18.38.3 r7's five index-name contexts. This compiled clean and aborted at RUN time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R16NEGD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 TE OCCURS 3 TIMES INDEXED BY IX.
             10 TK PIC X(3).
       PROCEDURE DIVISION.
           SET IX TO 1.
           DISPLAY "SRCH=" IX.
           STOP RUN.
