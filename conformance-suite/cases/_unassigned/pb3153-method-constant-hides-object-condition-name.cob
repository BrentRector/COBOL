      *>--- conformance-case
      *> id: pb3153-method-constant-hides-object-condition-name
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3153 review round 3 (OoMethodDataScope.Declares asks
      *>   the method's constant entries). ISO 11.7.4 GR5: "If a given
      *>   user-defined word is defined in the data division of this
      *>   method definition and in the data division of the containing
      *>   object definition, the use of that word in this method refers
      *>   to the declaration in this method" -> OK 11.7.4 5). SHOWA's X
      *>   is its own constant-name, the numeric literal 7 (13.10.4
      *>   GR1), which is none of the simple conditions of 8.8.4.2.1:
      *>   COBOLNET2318, as in a program whose only X is a constant
      *>   (kb/Work PB3191 - it said COBOLNET1639 "not defined", a false
      *>   message: X is declared). Before the PB3153 fix IF X tested
      *>   the object's level-88 X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3153CP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3153CC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3153CC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3153CC "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWA"
           STOP RUN.
       END PROGRAM PB3153CP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3153CC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9 VALUE 1.
          88 X VALUE 1.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 X CONSTANT AS 7.
       PROCEDURE DIVISION.
           IF X
               DISPLAY "A COND"
           END-IF.
       END METHOD SHOWA.
       END OBJECT.
       END CLASS PB3153CC.
