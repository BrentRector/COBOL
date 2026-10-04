      *> kb/Work PB1274 -- the accessor a PROPERTY clause defines is a
      *> superclass method like any other, so a subclass may OVERRIDE it.
      *> ISO 13.18.42.4 GR1: "If the GET phrase is not specified, the
      *> PROPERTY clause causes a method to be defined for the containing
      *> object"; 8.4.3.9.1: "a method implicitly generated for a data
      *> item described with the PROPERTY clause"; 11.7.3 SR3: "If the
      *> OVERRIDE phrase is specified, there shall be a method with the
      *> same method resolution signature" -- here the clause's GET.
      *> The subclass PB1274D is written BEFORE its superclass PB1274C:
      *> the rosters are resolved once every class's data has bound, so
      *> source order does not matter.  Expected values:
      *>   D=9  N OF a PB1274D object: the GET runs (8.4.3.9.4 GR1) and
      *>        resolves to the subclass's override (9.3.6), MOVE 9.
      *>   C=1  N OF a PB1274C object: the clause's implicit GET, which
      *>        returns N (VALUE 1).
      *>   C=5  after MOVE 5 TO N OF B, the clause's implicit SET stored
      *>        5 (13.18.42.4 GR2) and the implicit GET returns it.
      *>   D=9  the override ignores the stored value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1274P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1274C
           CLASS PB1274D
           PROPERTY N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE PB1274D.
       01 B USAGE OBJECT REFERENCE PB1274C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1274D "NEW" RETURNING A.
           INVOKE PB1274C "NEW" RETURNING B.
           DISPLAY "D=" N OF A.
           DISPLAY "C=" N OF B.
           MOVE 5 TO N OF B.
           DISPLAY "C=" N OF B.
           MOVE 7 TO N OF A.
           DISPLAY "D=" N OF A.
           STOP RUN.
       END PROGRAM PB1274P.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1274D INHERITS FROM PB1274C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1274C.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY N OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC 9.
       PROCEDURE DIVISION RETURNING LK.
       MAIN.
           MOVE 9 TO LK.
       END METHOD.
       END OBJECT.
       END CLASS PB1274D.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1274C INHERITS FROM BASE.
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
       END CLASS PB1274C.
