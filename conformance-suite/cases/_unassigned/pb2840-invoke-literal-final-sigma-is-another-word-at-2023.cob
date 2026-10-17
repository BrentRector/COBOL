*>--- conformance-case
*> id: pb2840-invoke-literal-final-sigma-is-another-word-at-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2840 - at 2023 FINAL SIGMA and SIGMA are two words (Annex
      *> E.2 item 14 deleted (03C2,03C3)), so INVOKE O "Xς" names no
      *> method of a class that defines only Xσ: 14.9.23.3 SR4 b)
      *> "literal-1 shall be the name of a method contained in the
      *> instance interface". Before 2023 the same source compiles (the
      *> positive golden 2002/pb2840_literal_names_edition_spelled).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2840X.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2840XC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB2840XC.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB2840XC "NEW" RETURNING O.
           INVOKE O "Xς".
           STOP RUN.
       END PROGRAM PB2840X.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2840XC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. Xσ.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "METHOD SIGMA".
       END METHOD Xσ.
       END OBJECT.
       END CLASS PB2840XC.
