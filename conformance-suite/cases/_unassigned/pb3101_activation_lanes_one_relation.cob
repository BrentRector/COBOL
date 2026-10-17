*>--- conformance-case
*> id: pb3101_activation_lanes_one_relation
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3101 / PB3127 - every bind-time activation lane asks the ONE relation the run-time CALL lane asks
      *> (ActivationRelations.CallArgumentViolation / CallReturningViolation), so the pairs ISO 1989:2023 admits
      *> run the same on every lane. Each verdict below is derived from the rule, not measured:
      *>  P1 a reference-modified argument BY REFERENCE to a PROTOTYPED program (14.9.4.3 SR25) whose formal is a
      *>     4-byte alphanumeric group: 8.4.3.3.4 GR5/GR6 give the slice no PICTURE clause, a group has none
      *>     either, and 14.8.2.2 rule 1 admits a formal of "the same number or a smaller number of bytes"
      *>     (docs/CONFORMANCE.md D-RM1). The callee sees Y(2:4) = "BCDE" and writes "wxyz" through it, so Y
      *>     becomes "AwxyzFGH" (14.2.3 GR8: the same storage area).
      *>  F1 the same through a user-defined FUNCTION (8.4.3.2.3 SR13; 14.8.2.3.2 rule 2 lists "a function"):
      *>     Y(1:3) into a 3-byte group formal; the function returns it, "Awx".
      *>  N1 an ANY LENGTH formal of a contained program activated with the NESTED phrase: K is described with
      *>     the same clauses but its length, which 14.8.2.3.2 "Additionally" d) relaxes ("its length is
      *>     considered to match the length of the corresponding argument"), so it conforms; 13.18.2.4 GR1 b)
      *>     makes the formal K's 4 positions: "[KLMN]".
      *>  N2 a level-66 THROUGH alias is "an alphanumeric group item" (13.18.45.4 GR2): 6 bytes (Q2 Q3) into the
      *>     4-byte group formal of N1G - 14.8.2.2 rule 1 admits it, and the callee sees "cdef".
      *> cite.py --check 14.8.2.2 "the formal parameter shall be described with the same number or a smaller
      *>   number of bytes as the corresponding argument" -> OK 14.8.2.2 1)
      *> cite.py --check 14.8.2.3.2 "its length is considered to match the length of the corresponding
      *>   argument" -> OK 14.8.2.3.2 2)
      *> cite.py --check 13.18.45.4 "data-name-1 defines an alphanumeric group item" -> OK 13.18.45.4 2)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3101P PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A.
          05 A1 PIC X(4).
       PROCEDURE DIVISION USING A.
       END PROGRAM P3101P.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. F3101G.
       DATA DIVISION.
       LINKAGE SECTION.
       01 G.
          05 G1 PIC X(3).
       01 R PIC X(3).
       PROCEDURE DIVISION USING BY REFERENCE G RETURNING R.
           MOVE G TO R
           GOBACK.
       END FUNCTION F3101G.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3101M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P3101P
           FUNCTION F3101G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y PIC X(8) VALUE "ABCDEFGH".
       01 K PIC X(4) VALUE "KLMN".
       01 R3 PIC X(3).
       01 QREC.
          05 Q1 PIC X(2) VALUE "ab".
          05 Q2 PIC X(2) VALUE "cd".
          05 Q3 PIC X(4) VALUE "efgh".
       66 QALIAS RENAMES Q2 THROUGH Q3.
       PROCEDURE DIVISION.
           CALL P3101P USING Y(2:4)
           DISPLAY "P1 Y=" Y
           MOVE FUNCTION F3101G(Y(1:3)) TO R3
           DISPLAY "F1 R=" R3
           CALL "N3101A" AS NESTED USING K
           CALL "N3101G" AS NESTED USING QALIAS
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101A.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       PROCEDURE DIVISION USING L.
           DISPLAY "N1 L=[" L "]"
           GOBACK.
       END PROGRAM N3101A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101G.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LG1 PIC X(4).
       PROCEDURE DIVISION USING LG.
           DISPLAY "N2 LG=" LG
           GOBACK.
       END PROGRAM N3101G.
       END PROGRAM P3101M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3101P.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A.
          05 A1 PIC X(4).
       PROCEDURE DIVISION USING A.
           DISPLAY "P1 A=" A
           MOVE "wxyz" TO A
           GOBACK.
       END PROGRAM P3101P.
