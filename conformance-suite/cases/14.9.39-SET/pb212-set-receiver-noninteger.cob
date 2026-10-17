*>--- conformance-case
*> id: pb212-set-receiver-noninteger
*> rules: [SR-14.9.39.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR1: "Identifier-1 shall reference a data item of
      *> class index or an integer data item." PIC 9(4)V99 is neither; it
      *> used to store the occurrence number as 000200.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB212N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 INDEXED BY IX.
             10 K PIC 9.
       01 R PIC 9(4)V99.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 2.
           SET R TO IX.
           DISPLAY R.
           STOP RUN.
