*>--- conformance-case
*> id: pb61-sr6-function-keyword-refmod
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.2.3 SR6 in the FUNCTION-keyword form for a function that REQUIRES its argument: UPPER-CASE
      *> (15.97) permits arguments, so the '(' after its name is ALWAYS the argument list and `1:4` is not an
      *> argument (SR8). Before PB61 the empty argument list bound FIRST and reported "takes 1 argument(s); 0
      *> given" (COBOLNET1504) - an arity error about a list the user never wrote - and only RANDOM (MinArgs 0)
      *> reached the SR6 verdict. SR6 is decided from the DEFINITION, before any argument binds.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB61SR6KW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T4 PIC X(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION UPPER-CASE (1:4) TO T4
           STOP RUN.
