      *>--- conformance-case
      *> id: pb370_open_unsuccessful_order
      *> rules: [GR-14.9.27.4-25]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.27.4 GR25: "If the execution of the OPEN statement is
      *> unsuccessful, the file is not affected and the following
      *> actions take place in the following order:
      *>   a) A value is placed in the I-O status associated with
      *>      file-name to indicate the condition that caused the OPEN
      *>      statement to be unsuccessful.
      *>   b) If it is enabled, the level-3 EC-I-O exception condition
      *>      associated with the I-O status value is set to exist.
      *>   c) If the OPEN statement is specified in
      *>      imperative-statement-1 in an exception-checking PERFORM
      *>      statement and there is an applicable WHEN phrase in that
      *>      PERFORM statement, then
      *>      execution continues according to the rules for that WHEN
      *>      phrase. No applicable USE declarative is executed.
      *>   d) Any applicable USE FOR EXCEPTION or USE AFTER EXCEPTION
      *>      procedure is executed as specified for the rules for the
      *>      USE statement."
      *> FN and FM are absent and not OPTIONAL, so OPEN INPUT is '35'
      *> (§9.1.13.6 5)), a fatal EC-I-O-PERMANENT-ERROR (§9.1.13.1).
      *>   L1  checking OFF: a) '35', then d) the format 1 USE for FN
      *>       (F1-USE 35). b) does not apply ("If it is enabled").
      *>   L2  checking ON, FM: a), b) EC-I-O-PERMANENT-ERROR, d) the
      *>       format 3 USE (F3-USE 35 EC-I-O-PERMANENT-ERROR); its
      *>       RESUME AT NEXT STATEMENT continues past the fatal
      *>       condition (§14.6.13.1.3 5) NOTE 2).
      *>   L3  checking ON, FN inside an exception-checking PERFORM: a),
      *>       b), then c) - the WHEN body runs (WHEN 35 + the status)
      *>       and the format 1 USE for FN, which d) would otherwise
      *>       run, does NOT. The WHEN body ends with STOP RUN:
      *>       §14.6.13.1.3 4) would terminate the run unit abnormally
      *>       after it for this fatal condition.
      *> "The file is not affected": the failed OPEN INPUT of L1 does
      *> not create FN, so L3's OPEN of the same file is '35' again.
      *> (The destructive direction - a '61' sharing refusal leaves the
      *> file's content intact - is
      *> conformance:cases/12.4.5.15-SHARING/pb321_table19_open_arbitration.) The
      *> exception-checking PERFORM is a COBOL-2023 introduction
      *> (perform-exception-checking-2023), so this golden runs at 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB370OU.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FN ASSIGN TO "pb370ou-absent-n.dat"
               FILE STATUS IS STN.
           SELECT FM ASSIGN TO "pb370ou-absent-m.dat"
               FILE STATUS IS STM.
       DATA DIVISION.
       FILE SECTION.
       FD FN.
       01 RN PIC X(5).
       FD FM.
       01 RM PIC X(5).
       WORKING-STORAGE SECTION.
       01 STN PIC XX.
       01 STM PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-FN SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON FN.
       D-FN-P.
           DISPLAY "F1-USE " STN.
       D-EC SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-PERMANENT-ERROR.
       D-EC-P.
           DISPLAY "F3-USE " STM " " FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN INPUT FN
           DISPLAY "L1 " STN
           >>TURN EC-I-O CHECKING ON
           OPEN INPUT FM
           DISPLAY "L2 " STM
           MOVE SPACES TO STN
           PERFORM
               OPEN INPUT FN
           WHEN EC-I-O-PERMANENT-ERROR
               DISPLAY "WHEN " STN " " FUNCTION EXCEPTION-STATUS
               STOP RUN
           END-PERFORM
           DISPLAY "L3 NOT REACHED"
           STOP RUN.
