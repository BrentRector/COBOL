      *> reject-at: 2002 2014 2023
      *> ISO 14.8.3.3 rule 4 (cite.py --check 14.8.3.3 "If the receiving operand is described with the ANY
      *> LENGTH clause, the sending operand shall also be described with the ANY LENGTH clause" -> OK 14.8.3.3
      *> 4)): rule 5 relaxes the length of an ANY LENGTH SENDER only; a RETURNING item delivered to an ANY
      *> LENGTH RECEIVER (here the caller's formal AL) from a FIXED-length sending item is a non-conforming
      *> pair.  PB1167 makes the conforming pairings reachable and keeps this prohibition: GETFIX returns
      *> PIC X(5) and AL is ANY LENGTH.  COBOLNET0828.  kb/Work PB1167.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1167N3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1167N3C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB1167N3C.
       01 A5  PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1167N3C "NEW" RETURNING OBJ.
           CALL "CALLER" AS NESTED USING A5.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CALLER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O2 USAGE OBJECT REFERENCE PB1167N3C.
       LINKAGE SECTION.
       01 AL PIC X ANY LENGTH.
       PROCEDURE DIVISION USING AL.
           INVOKE PB1167N3C "NEW" RETURNING O2.
           INVOKE O2 "GETFIX" RETURNING AL.
           GOBACK.
       END PROGRAM CALLER.
       END PROGRAM PB1167N3.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1167N3C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETFIX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X(5).
       PROCEDURE DIVISION RETURNING R.
       MAIN.
           MOVE "AB" TO R.
       END METHOD GETFIX.
       END OBJECT.
       END CLASS PB1167N3C.
