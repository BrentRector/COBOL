*>--- conformance-case
*> id: pb212-set-numeric-from-literal
*> rules: [SR-14.9.39.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR4: "If identifier-1 references a numeric data item,
      *> index-name-2 shall be specified." A literal is arithmetic-
      *> expression-1, not index-name-2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB212N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 INDEXED BY IX.
             10 K PIC 9.
       01 N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SET N TO 3.
           DISPLAY N.
           STOP RUN.
