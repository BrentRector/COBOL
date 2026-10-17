*>--- conformance-case
*> id: pb2586-unstring-alphabetic-function-slice-sender
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.48.3 SR2 (kb/Work PB2586, review R1-1): FUNCTION CONCAT over arguments that are all
      *> class alphabetic returns an alphabetic value (15.18.4 rule 3). A reference-modified slice of a
      *> PIC A item stays alphabetic (8.4.3.3.4 GR6, exhaustive exception list; kb/Work PB73), so a CONCAT
      *> of two such slices is alphabetic and SR2 refuses it exactly as it refuses the slice itself.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2586N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC A(5) VALUE "AB CD".
       01 B PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           UNSTRING FUNCTION CONCAT(A(1:2) A(3:3)) DELIMITED BY SPACE INTO B
           STOP RUN.
