      *>--- conformance-case
      *> id: pb370_use_file_beats_mode
      *> rules: [GR-14.9.49.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.49.4 GR5: "Within a given procedure division, a USE
      *> statement specifying file-name-1 takes precedence over any USE
      *> statements specifying an INPUT, OUTPUT, I-O, or EXTEND phrase."
      *> GR3: "The first declarative that satisfies the selection
      *> criteria is executed and no other declaratives are executed",
      *> with a) (file-name-1 specified) examined before b) (not
      *> specified); and GR6 b) runs an INPUT procedure for a file
      *> opened in the input mode "except those files referenced by
      *> file-name-1 in another USE statement specifying the same
      *> condition". The two declaratives are written MODE FIRST, FILE
      *> SECOND, so a selection by source order would run the mode one;
      *> the rule's precedence is by tier, not by order.
      *>   F2  absent, opened INPUT, no file-scoped USE names it: the
      *>       INPUT procedure runs (the control - the mode arm is
      *>       live).
      *>   F1  absent, opened INPUT, named by the file-scoped USE: ONLY
      *>       the file procedure runs, although the INPUT one, written
      *>       first, also matches the mode.
      *> '35' is the §9.1.13.6 5) value for OPEN INPUT of an absent
      *> non-optional file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB370UF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb370uf-absent-1.dat"
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "pb370uf-absent-2.dat"
               FILE STATUS IS ST2.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(5).
       FD F2.
       01 R2 PIC X(5).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       01 ST2 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-MODE SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       D-MODE-P.
           DISPLAY "MODE-USE " ST1 " " ST2.
       D-FILE SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       D-FILE-P.
           DISPLAY "FILE-USE " ST1.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE "--" TO ST1
           OPEN INPUT F2
           DISPLAY "F2 " ST2
           OPEN INPUT F1
           DISPLAY "F1 " ST1
           STOP RUN.
