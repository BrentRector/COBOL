      *> kb/Work PB1744 - ISO 12.3.8.3 SR13 (cite.py OK) prohibits only "the names of
      *> the intrinsic functions" as user-defined words in the scope of a REPOSITORY
      *> paragraph that specifies FUNCTION ALL INTRINSIC. The parameter-name ELT is
      *> no intrinsic-function-name, so this parameterized class is legal; its
      *> expansion PB1744W-TXT (12.3.8.4 GR5) runs a method that writes UPPER-CASE
      *> without the FUNCTION keyword, which the ALL INTRINSIC specifier admits.
      *> Expected: TXT / ABC.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1744TXT INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "TXT".
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1744TXT.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1744W INHERITS FROM BASE USING ELT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS ELT
           FUNCTION ALL INTRINSIC.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ITEM USAGE OBJECT REFERENCE ELT.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE OBJECT REFERENCE ELT.
       PROCEDURE DIVISION USING X.
           SET ITEM TO X.
       END METHOD PUT.
       METHOD-ID. SHOW-IT.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 W PIC X(3).
       PROCEDURE DIVISION.
           INVOKE ITEM "SHOW".
           MOVE UPPER-CASE("abc") TO W.
           DISPLAY W.
       END METHOD SHOW-IT.
       END OBJECT.
       END CLASS PB1744W.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1744M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1744TXT
           CLASS PB1744W
           CLASS PB1744W-TXT EXPANDS PB1744W USING PB1744TXT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE OBJECT REFERENCE PB1744TXT.
       01 H USAGE OBJECT REFERENCE PB1744W-TXT.
       PROCEDURE DIVISION.
           INVOKE PB1744TXT "NEW" RETURNING T.
           INVOKE PB1744W-TXT "NEW" RETURNING H.
           INVOKE H "PUT" USING T.
           INVOKE H "SHOW-IT".
           STOP RUN.
       END PROGRAM PB1744M.
