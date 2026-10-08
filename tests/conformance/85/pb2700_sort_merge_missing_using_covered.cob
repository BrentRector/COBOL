      * kb/Work PB2700 - a SORT or MERGE whose USING file is missing,
      * when the program IS equipped to see the failure (a FILE STATUS
      * clause, or a USE procedure that applies).  The uncovered arm -
      * no FILE STATUS, no USE - ends the run unit, which no golden can
      * witness (the corpus requires exit 0): it is witnessed by
      * DocA1Item103WitnessTests (the SORT/MERGE implicit-OPEN cases).
      *
      * 9.1.13.6 5): "I-O status = 35. A permanent error exists because
      *   an OPEN statement with the INPUT ... phrase is attempted on a
      *   file that is not described as optional and the physical file
      *   is not present."  (cite.py: OK 9.1.13.6 5))  A status that
      *   begins with 3 is FATAL (9.1.13.1).
      * 14.9.40.4 12) a) / 14.9.24.4 7) a): the USING file is initiated
      *   "as if an OPEN statement with the INPUT phrase ... had been
      *   executed"; both rules name only a NONFATAL status of the OPEN.
      *   (cite.py: OK 14.9.40.4 12); OK 14.9.24.4 7) a))
      * 9.1.13.1: "The implementor may either continue or terminate the
      *   execution of the run unit. If the implementor chooses to
      *   continue execution of the run unit, control is transferred to
      *   the end of the statement that produced the fatal exception
      *   condition".  (cite.py: OK 9.1.13.1)  docs/CONFORMANCE.md
      *   DOC-A.1-103: continue when a FILE STATUS clause or a USE
      *   procedure covers the status.  So every leg below continues
      *   past its SORT/MERGE, the statement having ended at the failed
      *   OPEN: no record is released, the output procedure never runs
      *   (no IN-OP line), and no implicit CLOSE overwrites the status.
      *
      *   L1  SORT USING MISS-S (FILE STATUS) - MS-ST shows the 35.
      *   L2  SORT USING MISS-U (USE procedure) - the USE runs, then the
      *       statement ends.
      *   L3  MERGE USING MISS-S MISS-U - the FIRST file's OPEN ends the
      *       MERGE: MISS-U is never opened, so its USE does not run.
      *   L4  MERGE USING MISS-U MISS-S - the USE runs for MISS-U; the
      *       MERGE ends there, so MS-ST still holds L3's 35.
      * A leg fails if its SORT/MERGE terminates the run unit (no more
      * lines), runs the output procedure (an IN-OP line), or lets an
      * implicit CLOSE overwrite the status (00 / 42).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2700CV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MISS-S ASSIGN TO "pb2700-never-written-s.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS MS-ST.
           SELECT MISS-U ASSIGN TO "pb2700-never-written-u.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SW ASSIGN TO "pb2700w.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD MISS-S.
       01 MS-REC PIC X(4).
       FD MISS-U.
       01 MU-REC PIC X(4).
       SD SW.
       01 SW-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 MS-ST PIC XX VALUE SPACES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-U SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON MISS-U.
       D-U-P.
           DISPLAY "USE MISS-U".
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           SORT SW ON ASCENDING KEY SW-REC
               USING MISS-S OUTPUT PROCEDURE IS OP-S.
           DISPLAY "L1-AFTER=" MS-ST.
           SORT SW ON ASCENDING KEY SW-REC
               USING MISS-U OUTPUT PROCEDURE IS OP-S.
           DISPLAY "L2-AFTER".
           MOVE SPACES TO MS-ST.
           MERGE SW ON ASCENDING KEY SW-REC
               USING MISS-S MISS-U OUTPUT PROCEDURE IS OP-S.
           DISPLAY "L3-AFTER=" MS-ST.
           MERGE SW ON ASCENDING KEY SW-REC
               USING MISS-U MISS-S OUTPUT PROCEDURE IS OP-S.
           DISPLAY "L4-AFTER=" MS-ST.
           STOP RUN.
       OP-S SECTION.
       OP-P.
           DISPLAY "IN-OP".
