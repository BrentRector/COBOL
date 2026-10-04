      *> reject-at: 2002 2014 2023
      *> kb/Work PB1274 - ISO/IEC 1989:2023 section 11.7.3 SR4 a): "if this method definition is contained in a class
      *> definition, no inherited method shall have the same method resolution signature as the method declared by
      *> this method definition" (unless OVERRIDE is specified).  Section 13.18.42.4 GR1: "If the GET phrase is not
      *> specified, the PROPERTY clause causes a method to be defined for the containing object".
      *>   cite.py: OK  11.7.3 4) a)  /  OK  13.18.42.4 1)
      *> The superclass's PROPERTY clause defines GET PROPERTY N; the subclass redefines it without OVERRIDE.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1274RS INHERITS FROM PB1274RB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1274RB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC 9.
       PROCEDURE DIVISION RETURNING LK.
       MAIN.
           MOVE 9 TO LK.
       END METHOD.
       END OBJECT.
       END CLASS PB1274RS.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1274RB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 1 PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB1274RB.
