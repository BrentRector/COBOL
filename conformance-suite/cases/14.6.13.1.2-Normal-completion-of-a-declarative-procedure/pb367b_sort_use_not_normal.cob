*>--- conformance-case
*> id: pb367b_sort_use_not_normal
*> rules: [GR-14.6.13.1.2-1, GR-14.9.40.4-12, GR-14.9.40.4-17, GR-14.9.49.4-7, GR-14.9.49.4-12]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB367b - 14.9.40.4 GR17: "If a USE procedure invoked while a format 1 SORT statement is
      *> active does not complete normally, the SORT statement is terminated."
      *>
      *> THE SETUP. S-IN is never created, so the SORT's implicit initiation of the USING file -
      *> 14.9.40.4 GR12 a), "The initiation is performed as if an OPEN statement with the INPUT phrase ...
      *> had been executed" - is unsuccessful (9.1.13.4: I-O status 35, an OPEN of a non-optional file that
      *> is not available). 14.9.40.4 GR12's own closing paragraph - "These implicit functions are performed
      *> such that any applicable USE procedures are executed" - selects D-IN by GR3 a) (a format 1 USE
      *> naming file-name-1) and GR6 a).
      *>
      *> WHAT THE RULES THEN REQUIRE, LINE BY LINE:
      *>  . D-IN displays DECL-RAN and executes RESUME AT NEXT STATEMENT. 14.6.13.1.2 #1 - a declarative
      *>    procedure completes normally only if no "EXIT PROGRAM, GOBACK, RESUME, or STOP statement ... is
      *>    executed" - so this procedure does NOT complete normally.
      *>  . GR17 therefore terminates the SORT statement. The OUTPUT PROCEDURE is part of the SORT's own
      *>    execution (GR14: "control passes to it after the file referenced by file-name-1 has been
      *>    sequenced by the SORT statement"), so a terminated SORT never passes control to it and
      *>    OUT-P-RAN is NOT displayed.
      *>  . 14.9.33.4 GR2 a) 1. puts the resume landing at the same place from the other direction: the
      *>    implicit OPEN is not a statement of the program, so "the applicable statement is the one in
      *>    which the exception condition was raised" is the SORT, and control goes to the implicit CONTINUE
      *>    after it => AFTER-SORT.
      *>  . The run then proves the sort file is usable again - a second SORT with an INPUT PROCEDURE
      *>    releasing one record runs to completion => SORTED=A. A terminated SORT statement is not a
      *>    terminated run unit (14.6.12 is nowhere in GR17), and the sort store is released either way.
      *>
      *> EXPECTED (computed from the rules above, not from a run):
      *>    DECL-RAN / AFTER-SORT / SORTED=A / DONE
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB367BSRTUSE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S-IN ASSIGN TO "pb367b-sort-absent.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS-IN.
           SELECT SW-FILE ASSIGN TO "pb367b-sortwork.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  S-IN.
       01  S-IN-REC PIC X.
       SD  SW-FILE.
       01  SW-REC.
           05  SW-KEY PIC X.
       WORKING-STORAGE SECTION.
       01  FS-IN PIC XX VALUE "00".
       01  W-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-IN SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON S-IN.
       D-IN-P.
           DISPLAY "DECL-RAN".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SORT SW-FILE ON ASCENDING KEY SW-KEY
               USING S-IN
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "AFTER-SORT".
           SORT SW-FILE ON ASCENDING KEY SW-KEY
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT2-P.
           DISPLAY "DONE".
           STOP RUN.
       IN-SECT SECTION.
       IN-P.
           MOVE "A" TO SW-REC.
           RELEASE SW-REC.
       OUT-SECT SECTION.
       OUT-P.
           DISPLAY "OUT-P-RAN".
       OUT2-SECT SECTION.
       OUT2-P.
           RETURN SW-FILE AT END MOVE "Y" TO W-EOF END-RETURN.
           IF W-EOF = "N" DISPLAY "SORTED=" SW-KEY END-IF.
