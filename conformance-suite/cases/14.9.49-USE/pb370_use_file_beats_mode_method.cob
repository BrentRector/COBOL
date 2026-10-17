      *>--- conformance-case
      *> id: pb370_use_file_beats_mode_method
      *> rules: [GR-14.9.49.4-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.49.4 GR5 inside a METHOD: "Within a given procedure
      *> division, a USE statement specifying file-name-1 takes
      *> precedence over any USE statements specifying an INPUT, OUTPUT,
      *> I-O, or EXTEND phrase." A method definition's procedure
      *> division is one such procedure division: §14.2.2 SR10 admits
      *> the DECLARATIVES format in "a method definition", and GR4 a)
      *> selects the declaratives of the element containing the failing
      *> statement. The twin of
      *> conformance:cases/14.9.49-USE/pb370_use_file_beats_mode, run where the
      *> declaratives sit in a method over the object's files (OO is a
      *> COBOL-2002 introduction). The mode declarative is written
      *> FIRST.
      *>   F2  absent, opened INPUT by the method, named by no file USE:
      *>       the INPUT procedure runs (the control).
      *>   F1  absent, opened INPUT, named by the file USE: ONLY the
      *>       file procedure runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB370UM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB370UH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H USAGE OBJECT REFERENCE PB370UH.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB370UH "NEW" RETURNING H
           INVOKE H "OPEN-BOTH"
           STOP RUN.
       END PROGRAM PB370UM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB370UH INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb370um-absent-1.dat"
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "pb370um-absent-2.dat"
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
       IDENTIFICATION DIVISION.
       METHOD-ID. OPEN-BOTH.
       PROCEDURE DIVISION.
       DECLARATIVES.
       M-MODE SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       M-MODE-P.
           DISPLAY "M-MODE-USE " ST1 " " ST2.
       M-FILE SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       M-FILE-P.
           DISPLAY "M-FILE-USE " ST1.
       END DECLARATIVES.
       M-MAIN SECTION.
       M-MAIN-P.
           MOVE "--" TO ST1
           OPEN INPUT F2
           DISPLAY "F2 " ST2
           OPEN INPUT F1
           DISPLAY "F1 " ST1.
       END METHOD OPEN-BOTH.
       END OBJECT.
       END CLASS PB370UH.
