      *>--- conformance-case
      *> id: pb1177_sort_called_goback_ec_free
      *> rules: [GR-14.9.40.4-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1177 - 14.9.40.4 GR17 for a CALLed program's USE
      *> procedure that ends in GOBACK, in a compilation group that uses
      *> no exception-condition feature at all (no RESUME, no >>TURN):
      *> 14.6.13.1.2 1) makes the GOBACK a completion that is not
      *> normal, GR10 puts the CALLed program in the input procedure's
      *> range, so the SORT is terminated once the CALL has returned.
      *> 14.2.3 GR8: what the program stored through its BY REFERENCE
      *> formal is the activator's.
      *> EXPECTED (computed from the rules, not from a run):
      *>    E-SUB-USE=35 / E-ARG=Y / E-AFTER (no E-IN-AFTER-CALL, no
      *>    E-OUT lines), then the control SORT: E-OUT=A / E-OUT=B /
      *>    E-AFTER2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1177E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1177e-sort-wk.dat".
       DATA DIVISION.
       FILE SECTION.
       SD  SW.
       01  SW-REC PIC X.
       WORKING-STORAGE SECTION.
       01  W-EOF PIC X.
       01  W-ARG PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "N" TO W-ARG.
           SORT SW ON ASCENDING KEY SW-REC
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "E-ARG=" W-ARG.
           DISPLAY "E-AFTER".
           SORT SW ON ASCENDING KEY SW-REC
               INPUT PROCEDURE IS IN-PLAIN
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "E-AFTER2".
           STOP RUN.
       IN-P.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           CALL "PB1177ES" USING W-ARG.
           DISPLAY "E-IN-AFTER-CALL".
           MOVE "A" TO SW-REC.
           RELEASE SW-REC.
       IN-PLAIN.
           MOVE "B" TO SW-REC.
           RELEASE SW-REC.
           MOVE "A" TO SW-REC.
           RELEASE SW-REC.
       OUT-P.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               RETURN SW AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY "E-OUT=" SW-REC
               END-RETURN
           END-PERFORM.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1177ES.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NF ASSIGN TO "pb1177e-absent.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  NF.
       01  NF-REC PIC X.
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       LINKAGE SECTION.
       01  L-ARG PIC X.
       PROCEDURE DIVISION USING L-ARG.
       DECLARATIVES.
       D-NF SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NF.
       D-NF-P.
           DISPLAY "E-SUB-USE=" FS.
           GOBACK.
       END DECLARATIVES.
       S-MAIN SECTION.
       S-1.
           MOVE "Y" TO L-ARG.
           OPEN INPUT NF.
           DISPLAY "E-SUB-AFTER-OPEN".
           GOBACK.
       END PROGRAM PB1177ES.
       END PROGRAM PB1177E.