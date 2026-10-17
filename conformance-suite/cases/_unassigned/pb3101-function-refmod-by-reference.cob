*>--- conformance-case
*> id: pb3101-function-refmod-by-reference
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3101 - a reference-modified argument BY REFERENCE to a user-defined FUNCTION's elementary
      *> formal. 8.4.3.2.3 SR13 imports 14.8.2, and 14.8.2.3.2 rule 2 (whose list names "a function") requires the
      *> same PICTURE clause; the slice 8.4.3.3.4 GR5 creates has none (docs/CONFORMANCE.md D-RM1), so it does
      *> not conform to the PIC X(5) formal -> COBOLNET2470. (Before the fix it ran and printed F=ABCDE.)
      *> cite.py --check 8.4.3.2.3 "the rules for conformance specified in 14.8.2, Parameters and 14.8.3,
      *>   Returning items, apply" -> OK 8.4.3.2.3 13)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. N3101F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC X(5).
       01 R PIC X(5).
       PROCEDURE DIVISION USING BY REFERENCE A RETURNING R.
           MOVE A TO R
           GOBACK.
       END FUNCTION N3101F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101FM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION N3101F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y PIC X(8) VALUE "ABCDEFGH".
       01 R5 PIC X(5).
       PROCEDURE DIVISION.
           MOVE FUNCTION N3101F(Y(1:5)) TO R5
           DISPLAY "F=" R5
           STOP RUN.
       END PROGRAM N3101FM.
