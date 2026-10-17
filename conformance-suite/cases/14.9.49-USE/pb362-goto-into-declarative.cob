*>--- conformance-case
*> id: pb362-goto-into-declarative
*> rules: [SR-14.9.49.3-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.49.3 SR4: "Procedure-names within a declarative section may be referenced in a different
*> declarative section or in a nondeclarative procedure only with a PERFORM statement." The GO TO DP1
*> in the nondeclarative portion names a paragraph of declarative section D1 (kb/Work PB362). The legal
*> PERFORM of the same paragraph is conformance:85/pb362_declaratives_reference_boundary.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB362GID.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb362gid.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       DP1.
           ADD 1 TO N.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           PERFORM DP1.
           GO TO DP1.
