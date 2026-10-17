*>--- conformance-case
*> id: pb2586-unstring-alphabetic-function-sender
*> rules: [SR-14.9.48.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.48.3 SR2 (kb/Work PB2586): a function-identifier references the unique data item the function
      *> returns. FUNCTION CONCAT over arguments that are all category alphabetic returns an alphabetic value
      *> (15.18.4 rule 3), which is neither category SR2 names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2586N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC A(2) VALUE "AB".
       01 C PIC A(2) VALUE "CD".
       01 B PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           UNSTRING FUNCTION CONCAT(A C) DELIMITED BY "B" INTO B
           STOP RUN.
