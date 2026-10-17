*>--- conformance-case
*> id: pb3153-method-constant-hides-object-index-name
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3153 review round 3 (OoMethodDataScope.Declares asks
      *>   the method's constant entries). ISO 11.7.4 GR5: "If a given
      *>   user-defined word is defined in the data division of this
      *>   method definition and in the data division of the containing
      *>   object definition, the use of that word in this method refers
      *>   to the declaration in this method" -> OK 11.7.4 5). SHOWA's QI
      *>   is its own constant-name, written "as if literal-1 ... were
      *>   written where constant-name-1 is written" (13.10.4 GR1 -> OK
      *>   13.10.4 1)), so SET QI TO 4 names no receiving item:
      *>   COBOLNET1548, as in a program whose only QI is a constant.
      *>   Before the fix SET QI TO 4 moved the object's index-name QI.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3153IP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3153IC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3153IC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3153IC "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWA"
           STOP RUN.
       END PROGRAM PB3153IP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3153IC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 G PIC 9 OCCURS 5 INDEXED BY QI.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 QI CONSTANT AS 2.
       PROCEDURE DIVISION.
           SET QI TO 4
           DISPLAY "A".
       END METHOD SHOWA.
       END OBJECT.
       END CLASS PB3153IC.
