      *> reject-at: 2002 2014 2023
      *> ISO 8.4.3.4.3 SR4 (cite.py --check 8.4.3.4.3 "The data item referenced in the RETURNING phrase of
      *> the invoked method's procedure division header shall not be described with the ANY LENGTH clause"
      *> -> OK 8.4.3.4.3 4)): an ANY LENGTH RETURNING item has no length until the activation, and an inline
      *> invocation has no receiving item to give it one (13.18.2.4 GR1 b) takes n from the activating
      *> element's returning item), so the same method that PB1167 makes legal through an INVOKE statement with
      *> a RETURNING identifier is refused inline.  The ANY LENGTH twin of pb428-inline-invocation-returning-
      *> active-class.  COBOLNET2140.  kb/Work PB1167.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1167N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1167N1C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB1167N1C.
       01 W   PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1167N1C "NEW" RETURNING OBJ.
           MOVE OBJ :: "GETV" TO W.
           STOP RUN.
       END PROGRAM PB1167N1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1167N1C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X ANY LENGTH.
       PROCEDURE DIVISION RETURNING R.
       MAIN.
           MOVE "AB" TO R.
       END METHOD GETV.
       END OBJECT.
       END CLASS PB1167N1C.
