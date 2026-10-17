*>--- conformance-case
*> id: pb210-go-to-depending-noninteger
*> rules: [SR-14.9.17.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.17.3 SR1: "Identifier-1 shall reference a numeric
      *> elementary data item that is an integer." PIC 9V9 is not an integer;
      *> it used to be truncated (2.7 -> 2) and select the second name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB210N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 INDEXED BY IX.
             10 K PIC 9.
       01 SEL PIC 9V9 VALUE 2.7.
       PROCEDURE DIVISION.
       MAIN-P.
           GO TO MAIN-P MAIN-P DEPENDING ON SEL.
           DISPLAY "FELL".
           STOP RUN.
