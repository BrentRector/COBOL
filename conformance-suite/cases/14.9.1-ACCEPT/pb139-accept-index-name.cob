*>--- conformance-case
*> id: pb139-accept-index-name
*> rules: [SR-14.9.1.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.1.2: an index-name is not an identifier; 13.18.38.3 r7 closes the contexts that may
      *> reference one and ACCEPT is not among them. The context diagnostic, not UNDEFINED (kb/Work PB139).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB139N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          02 E PIC X OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT IX
           STOP RUN.
