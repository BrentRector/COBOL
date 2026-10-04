      *> reject-at: 2002 2014 2023
      *> ISO 13.18.2.3 SR2 (cite.py --check 13.18.2.3 "The ANY LENGTH clause may be specified only in an
      *> elementary level 1 entry in the linkage section of a function, of a contained program, or of a method
      *> that is not a property method" -> OK 13.18.2.3 2)): SR3 b) admits an ANY LENGTH RETURNING item of a
      *> METHOD, but a GET PROPERTY method is a property method, so its RETURNING item may not be described
      *> with ANY LENGTH.  PB1167 lifts the SR3 b) staging for ordinary methods and leaves this screen in
      *> force.  COBOLNET1542.  kb/Work PB1167.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1167N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1167N2C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB1167N2C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1167N2C "NEW" RETURNING OBJ.
           STOP RUN.
       END PROGRAM PB1167N2.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1167N2C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY P.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X ANY LENGTH.
       PROCEDURE DIVISION RETURNING R.
       MAIN.
           MOVE "AB" TO R.
       END METHOD.
       END OBJECT.
       END CLASS PB1167N2C.
