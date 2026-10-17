*>--- conformance-case
*> id: pb433-perform-range-two-declarative-sections
*> rules: [SR-14.9.28.3-11]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.28.3 SR11: "When procedure-name-1 and procedure-name-2 are both specified and either is the
      *> name of a procedure in the declaratives portion of the procedure division, both shall be
      *> procedure-names in the same declarative section." Unchanged since X3.23-1985, so every --std rejects.
      *>
      *> HALF 2 of the rule (kb/Work PB433) — the QUIET half, and the one a "reject when one end is declarative
      *> and the other is not" screen would leave open: BOTH names are in the declaratives portion, but in two
      *> DIFFERENT declarative sections. Accepted, this ran and printed N=0011, silently executing a range that
      *> spans two USE procedures with different dispatch conditions (D-SEC1 is the error procedure for F1,
      *> D-SEC2 for F2). The witness for that half is here, beside the loud one, because one predicate decides
      *> both: the two ends shall be procedures of the SAME declarative section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB433TWODECLSECS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb433-two-1.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "pb433-two-2.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(10).
       FD F2.
       01 F2-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       D-P1.
           ADD 1 TO N.
       D-SEC2 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F2.
       D-P2.
           ADD 10 TO N.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-P.
           PERFORM D-P1 THRU D-P2
           DISPLAY "N=" N
           STOP RUN.
