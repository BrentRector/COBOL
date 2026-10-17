*>--- conformance-case
*> id: pb212-perform-varying-alphanumeric
*> rules: [SR-14.9.28.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.28.3 SR2: "Each identifier shall reference a numeric
      *> elementary item described in the data division." A PIC X induction
      *> variable used to compile and throw at run time (the PB210-PB212
      *> sibling sweep).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB212N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 INDEXED BY IX.
             10 K PIC 9.
       01 P PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM VARYING P FROM 1 BY 1 UNTIL P > "2"
               DISPLAY "X"
           END-PERFORM.
           STOP RUN.
