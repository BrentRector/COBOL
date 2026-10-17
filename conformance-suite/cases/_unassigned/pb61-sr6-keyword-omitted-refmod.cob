*>--- conformance-case
*> id: pb61-sr6-keyword-omitted-refmod
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.2.3 SR6: "If a function's definition permits arguments and a left parenthesis immediately
      *> follows ... intrinsic-function-name-1, the left parenthesis is always treated as the left parenthesis of
      *> that function's arguments." REPOSITORY FUNCTION ALL INTRINSIC lets the word FUNCTION be omitted (SR2),
      *> and UPPER-CASE (15.97) permits an argument - so `UPPER-CASE (1:4)` opens an ARGUMENT LIST, and 1:4 is not
      *> a valid argument (SR8). It is the SR6/SR8 argument-list error, not the data path's "not defined"
      *> (COBOLNET1639 was the pre-PB61 verdict) and not an arity error. Keyword omission is 2002+.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB61SR6KO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T4 PIC X(4).
       PROCEDURE DIVISION.
           MOVE UPPER-CASE (1:4) TO T4
           STOP RUN.
