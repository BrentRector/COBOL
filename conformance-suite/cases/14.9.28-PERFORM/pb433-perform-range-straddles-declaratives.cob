*>--- conformance-case
*> id: pb433-perform-range-straddles-declaratives
*> rules: [SR-14.9.28.3-11]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.28.3 SR11: "When procedure-name-1 and procedure-name-2 are both specified and either is the
      *> name of a procedure in the declaratives portion of the procedure division, both shall be
      *> procedure-names in the same declarative section." The rule is X3.23-1985's and is carried unchanged
      *> through 2002/2014/2023, so there is no edition below it and every --std rejects.
      *>
      *> HALF 1 of the rule (kb/Work PB433) — procedure-name-1 is a procedure of declarative section D-SEC and
      *> procedure-name-2 is in the NONdeclarative portion. Accepted, this compiled clean at every --std and
      *> then died: the specified set (§14.9.28.4 GR4) runs forward from the declarative paragraph through
      *> whatever physically follows END DECLARATIVES, which includes MAIN-P and therefore the PERFORM itself,
      *> so the dispatcher recursed until the CLR killed the process with a stack overflow.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB433STRADDLE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb433-straddle.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       D-P1.
           ADD 1 TO N.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-P.
           PERFORM D-P1 THRU MAIN-P2
           DISPLAY "N=" N
           STOP RUN.
       MAIN-P2.
           ADD 10 TO N.
