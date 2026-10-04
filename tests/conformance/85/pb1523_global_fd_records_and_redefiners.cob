      *> ISO 13.18.27.4 GR1-GR3: a GLOBAL FD with several record descriptions and
      *>   GLOBAL REDEFINES entries are reachable from a contained program.
      *> GR1: "All data-names subordinate to a global name are global
      *>   names."  cite.py --check 13.18.27.4 -> OK 13.18.27.4 1)
      *> GR2: "A statement in a program contained directly or indirectly
      *>   within a program that describes a global name may reference
      *>   that name without describing it again."
      *>   cite.py --check 13.18.27.4 -> OK 13.18.27.4 2)
      *> GR3: "it is only the subject of that REDEFINES clause that
      *>   possesses the global attribute."  cite.py --check 13.18.27.4
      *>   -> OK 13.18.27.4 3)
      *> Shapes (each used to crash the backend, kb/Work PB1523):
      *>   FD F GLOBAL with TWO records R1 / R2 (13.18.33.4 GR3: the
      *>   records are implicit redefinitions of one area), an ELEMENTARY
      *>   subject WS-B REDEFINES WS-A GLOBAL (WS-A is NOT global), a
      *>   second elementary subject WS-D REDEFINES WS-C GLOBAL, and a
      *>   program contained TWO levels deep that uses all of them.
      *> DERIVATION (one storage area per redefinition, 13.18.44.4 GR1):
      *>   INNER stores "ABCDE" into R1; R2 is the same area, so
      *>   R2A = "AB" and R2B = "CDE"                   => AB/CDE
      *>   WS-A = "1234" and WS-B (PIC 9(4)) is its storage = 1234;
      *>   ADD 1 TO WS-B leaves 1235 in the shared area  => 1235
      *>   INNER2 MOVEs "PQRS" TO WS-D (WS-C's storage); the container
      *>   then DISPLAYs WS-A and WS-C                   => 1235/PQRS
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1523A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB1523.DAT" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F GLOBAL.
       01  R1      PIC X(5).
       01  R2.
           05  R2A PIC X(2).
           05  R2B PIC X(3).
       WORKING-STORAGE SECTION.
       01  WS-A    PIC X(4) VALUE "1234".
       01  WS-B    REDEFINES WS-A GLOBAL PIC 9(4).
       01  WS-C    PIC X(4) VALUE "WXYZ".
       01  WS-D    REDEFINES WS-C GLOBAL PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB1523B".
           DISPLAY WS-A "/" WS-C.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1523B.
       PROCEDURE DIVISION.
       SUB-P.
           MOVE "ABCDE" TO R1.
           DISPLAY R2A "/" R2B.
           ADD 1 TO WS-B.
           DISPLAY WS-B.
           CALL "PB1523C".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1523C.
       PROCEDURE DIVISION.
       SUB2-P.
           MOVE "PQRS" TO WS-D.
           EXIT PROGRAM.
       END PROGRAM PB1523C.
       END PROGRAM PB1523B.
       END PROGRAM PB1523A.
