*>--- conformance-case
*> id: pb212-set-index-data-from-literal
*> rules: [SR-14.9.39.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR3: "If identifier-1 references a data item of class
      *> index, arithmetic-expression-1 shall not be specified."
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB212N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 INDEXED BY IX.
             10 K PIC 9.
       01 IXD USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IXD TO 3.
           SET IX TO IXD.
           DISPLAY K (IX).
           STOP RUN.
