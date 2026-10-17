*>--- conformance-case
*> id: pb151_allocate
*> rules: [GR-14.9.3.4-1, GR-14.9.3.4-2, GR-14.9.3.4-4, GR-14.9.3.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       >>TURN EC-STORAGE-NOT-AVAIL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB151AL.
      *> kb/Work PB151 - ISO 14.9.3.4 per leg: GR1 rounds a NATIVE-FLOAT
      *> request UP on the double (COMP-2 2.5 -> a 3-character cell; the
      *> old (long)(double) truncated to 2 and the MOVE below would trip
      *> Deref's EC-BOUND-PTR); GR5 - a 21-digit request is NOT AVAILABLE:
      *> NULL + nonfatal EC-STORAGE-NOT-AVAIL (the old path wrapped it
      *> into a small VALID cell or threw OverflowException); GR2 - zero
      *> is NULL with NO exception; and 14.9.39 F10 GR19 - SET P UP BY a
      *> fractional float amount is unsuccessful with P UNCHANGED (the
      *> old truncation displaced by 1).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE COMP-2 VALUE 2.5.
       01 FH USAGE COMP-2 VALUE 1.5.
       01 P USAGE POINTER.
       01 B3 PIC X(3) BASED.
       01 N20 PIC 9(21) VALUE 100000000000000000000.
       PROCEDURE DIVISION.
       MAIN.
           ALLOCATE F CHARACTERS RETURNING P
           SET ADDRESS OF B3 TO P
           MOVE "ABC" TO B3
           DISPLAY "CEIL=" B3
           SET P UP BY FH
           SET ADDRESS OF B3 TO P
           DISPLAY "GR19=" B3
           ALLOCATE N20 CHARACTERS RETURNING P
           IF P = NULL
               DISPLAY "HUGE=NULL " FUNCTION EXCEPTION-STATUS(1:20)
           END-IF
           ALLOCATE 0 CHARACTERS RETURNING P
           IF P = NULL DISPLAY "ZERO=NULL" END-IF
           STOP RUN.
