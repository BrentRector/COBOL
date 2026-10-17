      *>--- conformance-case
      *> id: pb518_clause_placement_legal
      *> rules: [SR-13.16.3-5, SR-13.18.8.3-1, SR-13.18.22.3-1, SR-13.18.32.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 - the LEGAL placements of the data-description clauses the clause-placement screen
      *> governs (kb/Work PB507 / PB512 / PB518 / PB519), each at the edition that introduced it (all 85-era):
      *>   F-REC  GLOBAL on a level-1 FILE SECTION record - 13.18.27.3 SR1 b) admits "a data description entry
      *>          whose level-number is 1 that is specified in the file, working-storage, local-storage, or
      *>          linkage section". Before the screen the registration scan never read the FILE SECTION, so the
      *>          contained program's reference to F-REC was UNDEFINED (COBOLNET1639) - legal source rejected.
      *>   W-GLB  GLOBAL on a level-1 working-storage entry (13.16.3 SR6 / SR7).
      *>   X-ANC  EXTERNAL on the redefines ANCHOR (13.16.3 SR5 forbids it only on the entry that ALSO says
      *>          REDEFINES). 13.18.22.4 GR1: the record is external, and X-RED shares its storage, so the value
      *>          stored through X-RED is the one PB518CLX sees through its own X-ANC.
      *>   N-BWZ  BLANK WHEN ZERO on an elementary numeric display item without 'S' (13.18.8.3 SR1/SR2); a zero
      *>          store sets it to all spaces (13.18.8.4 GR1).
      *>   A-JUS  JUSTIFIED on an elementary alphanumeric item (13.18.32.3 SR1/SR3); "AB" is right-justified
      *>          into five positions, space-filled on the left.
      *> Expected, derived from the rules above: W=GLB R=FILREC / X=1234 / N=[   ] J=[   AB]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB518CLP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb518clp.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC IS GLOBAL      PIC X(6).
       WORKING-STORAGE SECTION.
       01  W-GLB IS GLOBAL      PIC X(3) VALUE "GLB".
       01  X-ANC IS EXTERNAL    PIC X(4).
       01  X-RED REDEFINES X-ANC PIC 9(4).
       01  N-BWZ                PIC 9(3) BLANK WHEN ZERO.
       01  A-JUS                PIC X(5) JUSTIFIED RIGHT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F
           MOVE "FILREC" TO F-REC
           MOVE 1234 TO X-RED
           MOVE 0 TO N-BWZ
           MOVE "AB" TO A-JUS
           CALL "PB518CLI"
           CALL "PB518CLX"
           DISPLAY "N=[" N-BWZ "] J=[" A-JUS "]"
           CLOSE F
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB518CLI.
       PROCEDURE DIVISION.
       CLI-PARA.
           DISPLAY "W=" W-GLB " R=" F-REC
           EXIT PROGRAM.
       END PROGRAM PB518CLI.
       END PROGRAM PB518CLP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB518CLX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X-ANC IS EXTERNAL    PIC X(4).
       PROCEDURE DIVISION.
       CLX-PARA.
           DISPLAY "X=" X-ANC
           EXIT PROGRAM.
       END PROGRAM PB518CLX.
