      *>--- conformance-case
      *> id: pb1177_sort_procedure_use_not_normal
      *> rules: [GR-14.9.40.4-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       >>TURN EC-BOUND-SUBSCRIPT EC-SORT-MERGE-ACTIVE CHECKING ON
      *> kb/Work PB1177 - 14.9.40.4 GR17: "If a USE procedure invoked
      *> while a format 1 SORT statement is active does not complete
      *> normally, the SORT statement is terminated." - for a USE
      *> procedure invoked by a statement OF THE INPUT OR OUTPUT
      *> PROCEDURE (pb367b_sort_use_not_normal pins the implicit
      *> USING/GIVING transfer).
      *>
      *> THE RULES, LINE BY LINE:
      *>  . 14.9.40.4 GR10 / GR13: the range of the input (output)
      *>    procedure "includes all statements in declarative
      *>    procedures that are executed as a result of the execution
      *>    of statements in the range" - a USE procedure an OPEN in
      *>    the procedure invokes runs while the SORT is active.
      *>  . 14.6.13.1.2 1): a declarative does not complete normally
      *>    when "An EXIT PROGRAM, GOBACK, RESUME, or STOP statement
      *>    ... is executed, or a fatal exception occurs within the
      *>    scope of the declarative".
      *>  . GR17 then terminates the SORT: nothing more of its input
      *>    procedure, sequence phase or output procedure runs.
      *>    14.9.33.4 GR2 a) puts RESUME AT NEXT STATEMENT's landing
      *>    after the raising statement "unless general rules
      *>    associated with the applicable statement specify
      *>    otherwise"; GR17 is that rule - a statement inside a
      *>    terminated SORT's procedure has no next statement to run,
      *>    so control goes to the end of the SORT (=> Ln-AFTER).
      *>  . 14.9.33.4 GR3: RESUME AT procedure-name is a GO TO; the
      *>    SORT is terminated first, so the next SORT is not one
      *>    "executed within the range" of this one (GR10 -
      *>    EC-SORT-MERGE-ACTIVE is checked ON above and stays quiet).
      *>
      *> THE LEGS (the expected output is computed from the rules
      *> above, not from a run):
      *>  L0  control - the USE procedure completes normally: the SORT
      *>      goes on => L0-USE-OK=35 L0-IN-AFTER-OPEN L0-OUT=A L0-OUT=B
      *>      L0-AFTER.
      *>  L1  input procedure, RESUME AT NEXT STATEMENT => L1-USE-IN=35
      *>      L1-AFTER (no L1-IN-AFTER-OPEN, no output procedure).
      *>  L2  input procedure, RESUME AT L2-LANDED => L2-USE-RP=35
      *>      L2-LANDED, never L2-NOT-REACHED.
      *>  L3  output procedure, after one RETURN => L3-OUT=A
      *>      L3-USE-IN=35 L3-AFTER (no L3-OUT-AFTER-OPEN, no B).
      *>  L4  a fall-off that is not normal: the GLOBAL declarative
      *>      PERFORMs a paragraph whose RESUME 14.9.33.4 GR1 makes a
      *>      CONTINUE => L4-USE-G-START L4-USE-RES L4-USE-RES-AFTER
      *>      L4-USE-G-END L4-AFTER.
      *>  L5  the fatal EC-BOUND-SUBSCRIPT inside D-F selects D-SUB,
      *>      itself a USE procedure invoked while the SORT is active;
      *>      its RESUME terminates the SORT there => L5-USE-F-START
      *>      L5-USE-SUB L5-AFTER (no L5-USE-F-END).
      *>  L6  MERGE has no GR17 (14.9.24.4): the output procedure's
      *>      OPEN is the applicable statement (14.9.33.4 GR2 a) 1.)
      *>      and the MERGE goes on => L6-OUT=A L6-USE-IN=35
      *>      L6-OUT-AFTER-OPEN L6-OUT=B L6-AFTER.
      *>  L7  the sort file is usable again => L7-OUT=Z, then DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1177SRT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NF-IN ASSIGN TO "pb1177-absent-in.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS-IN.
           SELECT NF-RP ASSIGN TO "pb1177-absent-rp.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS-RP.
           SELECT NF-OK ASSIGN TO "pb1177-absent-ok.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS-OK.
           SELECT NF-G ASSIGN TO "pb1177-absent-g.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS-G.
           SELECT NF-F ASSIGN TO "pb1177-absent-f.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS-F.
           SELECT NF-X ASSIGN TO "pb1177-absent-x.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS-X.
           SELECT SRC-A ASSIGN TO "pb1177a.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS SA-ST.
           SELECT SRC-B ASSIGN TO "pb1177b.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS SB-ST.
           SELECT SW-FILE ASSIGN TO "pb1177w.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  NF-IN.
       01  NF-IN-REC PIC X.
       FD  NF-RP.
       01  NF-RP-REC PIC X.
       FD  NF-OK.
       01  NF-OK-REC PIC X.
       FD  NF-G.
       01  NF-G-REC PIC X.
       FD  NF-F.
       01  NF-F-REC PIC X.
       FD  NF-X.
       01  NF-X-REC PIC X.
       FD  SRC-A.
       01  SA-REC PIC X.
       FD  SRC-B.
       01  SB-REC PIC X.
       SD  SW-FILE.
       01  SW-REC.
           05  SW-KEY PIC X.
       WORKING-STORAGE SECTION.
       01  FS-IN PIC XX.
       01  FS-RP PIC XX.
       01  FS-OK PIC XX.
       01  FS-G PIC XX.
       01  FS-F PIC XX.
       01  FS-X PIC XX.
       01  SA-ST PIC XX.
       01  SB-ST PIC XX.
       01  LEG PIC XX.
       01  W-EOF PIC X.
       01  WS-N PIC 9 VALUE 9.
       01  WS-TBL.
           05  WS-T PIC X OCCURS 3 TIMES VALUE "Q".
       01  WS-X PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-IN SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NF-IN.
       D-IN-P.
           DISPLAY LEG "-USE-IN=" FS-IN.
           RESUME AT NEXT STATEMENT.
       D-RP SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NF-RP.
       D-RP-P.
           DISPLAY LEG "-USE-RP=" FS-RP.
           RESUME AT L2-LANDED.
       D-OK SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NF-OK.
       D-OK-P.
           DISPLAY LEG "-USE-OK=" FS-OK.
       D-G SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON NF-G.
       D-G-P.
           DISPLAY LEG "-USE-G-START".
           PERFORM D-RES-P.
           DISPLAY LEG "-USE-G-END".
       D-RES SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NF-X.
       D-RES-P.
           DISPLAY LEG "-USE-RES".
           RESUME AT NEXT STATEMENT.
           DISPLAY LEG "-USE-RES-AFTER".
       D-F SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NF-F.
       D-F-P.
           DISPLAY LEG "-USE-F-START".
           MOVE WS-T (WS-N) TO WS-X.
           DISPLAY LEG "-USE-F-END".
       D-SUB SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       D-SUB-P.
           DISPLAY LEG "-USE-SUB".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       L0.
           MOVE "L0" TO LEG.
           SORT SW-FILE ON ASCENDING KEY SW-KEY
               INPUT PROCEDURE IS IN-OK
               OUTPUT PROCEDURE IS OUT-ALL.
           DISPLAY "L0-AFTER".
       L1.
           MOVE "L1" TO LEG.
           SORT SW-FILE ON ASCENDING KEY SW-KEY
               INPUT PROCEDURE IS IN-RESUME-NEXT
               OUTPUT PROCEDURE IS OUT-ALL.
           DISPLAY "L1-AFTER".
       L2.
           MOVE "L2" TO LEG.
           SORT SW-FILE ON ASCENDING KEY SW-KEY
               INPUT PROCEDURE IS IN-RESUME-AT
               OUTPUT PROCEDURE IS OUT-ALL.
           DISPLAY "L2-NOT-REACHED".
       L2-LANDED.
           DISPLAY "L2-LANDED".
       L3.
           MOVE "L3" TO LEG.
           SORT SW-FILE ON ASCENDING KEY SW-KEY
               INPUT PROCEDURE IS IN-AB
               OUTPUT PROCEDURE IS OUT-RESUME-NEXT.
           DISPLAY "L3-AFTER".
       L4.
           MOVE "L4" TO LEG.
           SORT SW-FILE ON ASCENDING KEY SW-KEY
               INPUT PROCEDURE IS IN-GLOBAL
               OUTPUT PROCEDURE IS OUT-ALL.
           DISPLAY "L4-AFTER".
       L5.
           MOVE "L5" TO LEG.
           SORT SW-FILE ON ASCENDING KEY SW-KEY
               INPUT PROCEDURE IS IN-FATAL
               OUTPUT PROCEDURE IS OUT-ALL.
           DISPLAY "L5-AFTER".
       L6.
           MOVE "L6" TO LEG.
           OPEN OUTPUT SRC-A.
           MOVE "A" TO SA-REC.
           WRITE SA-REC.
           CLOSE SRC-A.
           OPEN OUTPUT SRC-B.
           MOVE "B" TO SB-REC.
           WRITE SB-REC.
           CLOSE SRC-B.
           MERGE SW-FILE ON ASCENDING KEY SW-KEY
               USING SRC-A SRC-B
               OUTPUT PROCEDURE IS OUT-RESUME-NEXT.
           DISPLAY "L6-AFTER".
       L7.
           MOVE "L7" TO LEG.
           SORT SW-FILE ON ASCENDING KEY SW-KEY
               INPUT PROCEDURE IS IN-Z
               OUTPUT PROCEDURE IS OUT-ALL.
           DISPLAY "DONE".
           STOP RUN.
       IN-OK-S SECTION.
       IN-OK.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           OPEN INPUT NF-OK.
           DISPLAY LEG "-IN-AFTER-OPEN".
           MOVE "A" TO SW-REC.
           RELEASE SW-REC.
       IN-RESUME-NEXT-S SECTION.
       IN-RESUME-NEXT.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           OPEN INPUT NF-IN.
           DISPLAY LEG "-IN-AFTER-OPEN".
           MOVE "A" TO SW-REC.
           RELEASE SW-REC.
       IN-RESUME-AT-S SECTION.
       IN-RESUME-AT.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           OPEN INPUT NF-RP.
           DISPLAY LEG "-IN-AFTER-OPEN".
       IN-AB-S SECTION.
       IN-AB.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           MOVE "A" TO SW-REC.
           RELEASE SW-REC.
       IN-GLOBAL-S SECTION.
       IN-GLOBAL.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           OPEN INPUT NF-G.
           DISPLAY LEG "-IN-AFTER-OPEN".
       IN-FATAL-S SECTION.
       IN-FATAL.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           OPEN INPUT NF-F.
           DISPLAY LEG "-IN-AFTER-OPEN".
       IN-Z-S SECTION.
       IN-Z.
           MOVE "Z" TO SW-REC.
           RELEASE SW-REC.
       OUT-ALL-S SECTION.
       OUT-ALL.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               RETURN SW-FILE
                   AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY LEG "-OUT=" SW-KEY
               END-RETURN
           END-PERFORM.
       OUT-RESUME-NEXT-S SECTION.
       OUT-RESUME-NEXT.
           RETURN SW-FILE
               AT END DISPLAY LEG "-OUT-AT-END"
               NOT AT END DISPLAY LEG "-OUT=" SW-KEY
           END-RETURN.
           OPEN INPUT NF-IN.
           DISPLAY LEG "-OUT-AFTER-OPEN".
           RETURN SW-FILE
               AT END DISPLAY LEG "-OUT-AT-END"
               NOT AT END DISPLAY LEG "-OUT=" SW-KEY
           END-RETURN.
