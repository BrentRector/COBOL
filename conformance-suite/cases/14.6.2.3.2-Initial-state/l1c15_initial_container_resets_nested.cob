      *>--- conformance-case
      *> id: l1c15_initial_container_resets_nested
      *> rules: [GR-14.6.2.3.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.6.2.3.2 initial state case 2 — CALLing an INITIAL
      *> program resets the static data of the programs it directly AND
      *> indirectly contains.
      *> Rule: "Static data is placed in the initial state: ... 2) The
      *> first time the program in which it is described is activated
      *> after the execution of an activating statement referencing a
      *> program that possesses the initial attribute and directly or
      *> indirectly contains the program."
      *> cite.py: OK  §14.6.2.3.2 2)  (Initial state)
      *> Initial state includes "3) The function, method, or program's
      *> internal file connectors are initialized by setting them to
      *> not be in any open mode." (same clause, second list).
      *> §14.6.2.3.3: "Static data is in the last-used state except
      *> when it is in the initial state as defined above."
      *> cite.py: OK  §14.6.2.3.3   (Last-used state)
      *> Structure: L1C15D (INITIAL) contains L1C15E (M PIC 99 VALUE 0,
      *> internal file FE) which contains L1C15F (I PIC 99 VALUE 0).
      *> Neither E nor F is INITIAL or RECURSIVE, so their WS is static.
      *> L1C15G is a separate non-initial, non-contained control.
      *> DERIVATION:
      *>  Pass 1 (first activation in the run unit, case 1): E adds 1
      *>   to M -> "E M=01"; E opens FE OUTPUT, never closes it ->
      *>   "E FS=00"; F adds 1 to I -> "F I=01".
      *>  Pass 2: CALL "L1C15D" references an INITIAL program that
      *>   directly contains E and indirectly contains F, so case 2
      *>   puts both in initial state again: M=0 -> "E M=01", FE in no
      *>   open mode so OPEN OUTPUT succeeds -> "E FS=00" (last-used
      *>   would leave FE open: status 41), I=0 -> "F I=01" (last-used
      *>   would print 02).
      *>  Control G: not contained in any initial program, so its K is
      *>   in last-used state on the second CALL -> "G K=01", "G K=02".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C15C.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "PASS 1".
           CALL "L1C15D".
           DISPLAY "PASS 2".
           CALL "L1C15D".
           CALL "L1C15G".
           CALL "L1C15G".
           STOP RUN.
       END PROGRAM L1C15C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C15D IS INITIAL PROGRAM.
       PROCEDURE DIVISION.
       D-P.
           CALL "L1C15E".
           EXIT PROGRAM.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C15E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FE ASSIGN TO "L1C15E.DAT"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD FE.
       01 RE PIC X(4).
       WORKING-STORAGE SECTION.
       01 M  PIC 99 VALUE 0.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       E-P.
           ADD 1 TO M.
           DISPLAY "E M=" M.
           OPEN OUTPUT FE.
           DISPLAY "E FS=" FS.
           CALL "L1C15F".
           EXIT PROGRAM.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C15F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       F-P.
           ADD 1 TO I.
           DISPLAY "F I=" I.
           EXIT PROGRAM.
       END PROGRAM L1C15F.
       END PROGRAM L1C15E.
       END PROGRAM L1C15D.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C15G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       G-P.
           ADD 1 TO K.
           DISPLAY "G K=" K.
           EXIT PROGRAM.
       END PROGRAM L1C15G.
