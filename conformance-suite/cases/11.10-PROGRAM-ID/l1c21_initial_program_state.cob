      *>--- conformance-case
      *> id: l1c21_initial_program_state
      *> rules: [GR-11.10.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.10.4 GR3 — INITIAL program and its containees restart
      *> "The INITIAL clause specifies that the program is initial
      *> program. When an initial program is activated, the data items
      *> and file connectors contained in it and any program contained
      *> within it are set to their initial states."
      *>   cite.py --check 11.10.4 -> OK  §11.10.4 3)  (General rules)
      *> What "initial state" means, §14.6.2.3.2:
      *>   2) static data is placed in the initial state "The first time
      *>   the program in which it is described is activated after the
      *>   execution of an activating statement referencing a program
      *>   that possesses the initial attribute and directly or
      *>   indirectly contains the program."
      *>   cite.py --check 14.6.2.3.2 -> OK  §14.6.2.3.2 2)  (Initial
      *>   state)
      *>   3) "The function, method, or program's internal file
      *>   connectors are initialized by setting them to not be in any
      *>   open mode."
      *>   cite.py --check 14.6.2.3.2 -> OK  §14.6.2.3.2 3)  (Initial
      *>   state)
      *> Control: §8.6.6 "If neither the INITIAL nor RECURSIVE clause is
      *> specified ... the program's data is in the last-used state on
      *> other than the first activation".
      *>   cite.py --check 8.6.6 -> OK  §8.6.6   (Common, initial, and
      *>   recursive attributes)
      *> L1C21D calls, twice: L1C21E (INITIAL; it OPENs OUTPUT its file
      *> and never CLOSEs it, adds 1 to a VALUE 0 counter, then calls
      *> its contained non-initial L1C21F, which adds 1 to its own VALUE
      *> 0 counter), then L1C21G (ordinary, the control).
      *> Expected output, derived:
      *>   "E 1 00" — first activation: counter 0+1; OPEN OUTPUT of a
      *>              not-open connector succeeds, I-O status '00'.
      *>   "F 1"    — F's first activation: 0+1.
      *>   "G 1"    — G's first activation: 0+1.
      *>   "E 1 00" — E is INITIAL: its counter is back to VALUE 0, so
      *>              0+1 = 1 (a last-used state would print 2); its
      *>              file connector is back to not-open, so OPEN
      *>              OUTPUT again gives '00' (a still-open connector
      *>              would give '41', §14.9.27.4 GR2: "If it is open,
      *>              the execution of the OPEN statement is
      *>              unsuccessful and the I-O status ... '41'";
      *>              cite.py --check 14.9.27.4 -> OK  §14.9.27.4 2)).
      *>   "F 1"    — F is contained in the INITIAL program E, so
      *>              §14.6.2.3.2 2) resets it too: 1, not 2.
      *>   "G 2"    — G is not initial and not contained in E: its
      *>              last-used counter 1 becomes 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21D.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "L1C21E"
           CALL "L1C21G"
           CALL "L1C21E"
           CALL "L1C21G"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21E IS INITIAL PROGRAM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "L1C21E.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS EST.
       DATA DIVISION.
       FILE SECTION.
       FD EF.
       01 EF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ECNT PIC 9 VALUE 0.
       01 EST PIC XX VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD 1 TO ECNT
           OPEN OUTPUT EF
           DISPLAY "E " ECNT " " EST
           CALL "L1C21F"
           EXIT PROGRAM.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FCNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD 1 TO FCNT
           DISPLAY "F " FCNT
           EXIT PROGRAM.
       END PROGRAM L1C21F.
       END PROGRAM L1C21E.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GCNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD 1 TO GCNT
           DISPLAY "G " GCNT
           EXIT PROGRAM.
       END PROGRAM L1C21G.
       END PROGRAM L1C21D.
