*>--- conformance-case
*> id: pb2586-unstring-alphabetic-sender
*> rules: [SR-14.9.48.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.48.3 SR2 (kb/Work PB2586): "Identifier-1, identifier-2, identifier-3, and identifier-5 shall
      *> reference data items of category alphanumeric or national." Category alphabetic (PIC A) is a category
      *> of its own (8.5.2.1 Table 2), so a PIC A sender offends. The screen matched PicCategory.Alphanumeric,
      *> which the storage model also uses for PIC A, and the program compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2586N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC A(5) VALUE "AB CD".
       01 B PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           UNSTRING A DELIMITED BY SPACE INTO B
           STOP RUN.
