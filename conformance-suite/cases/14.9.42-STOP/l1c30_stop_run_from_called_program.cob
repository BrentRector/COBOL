      *>--- conformance-case
      *> id: l1c30_stop_run_from_called_program
      *> rules: [GR-14.9.42.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.42.4 GR6 — STOP RUN ends the whole run unit
      *>   "6) Execution of the run unit terminates and control is
      *>   transferred to the operating system."
      *>   cite.py --check 14.9.42.4 "Execution of the run unit
      *>     terminates and control is transferred to the operating
      *>     system." -> OK §14.9.42.4 6)
      *> The STOP RUN executes in a CALLed program, two levels below
      *> the main program's out-of-line PERFORM. GR6 terminates the RUN
      *> UNIT, not merely the called program (that would be EXIT
      *> PROGRAM / GOBACK, which return to the caller) and not merely
      *> the PERFORM range: no statement after it anywhere runs.
      *> DERIVATION of every .out line:
      *>   MAIN-BEFORE    - L1C30M runs first
      *>   SUB-BEFORE     - PERFORM P1 -> CALL "L1C30O" -> first DISPLAY
      *>   (nothing else) - STOP RUN in L1C30O: SUB-AFTER, the caller's
      *>                    MAIN-AFTER-CALL and MAIN-AFTER-PERFORM are
      *>                    never reached.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C30M.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "MAIN-BEFORE".
           PERFORM P1.
           DISPLAY "MAIN-AFTER-PERFORM".
           STOP RUN.
       P1.
           CALL "L1C30O".
           DISPLAY "MAIN-AFTER-CALL".
       END PROGRAM L1C30M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C30O.
       PROCEDURE DIVISION.
       SUB-MAIN.
           DISPLAY "SUB-BEFORE".
           STOP RUN.
       SUB-TAIL.
           DISPLAY "SUB-AFTER".
           EXIT PROGRAM.
       END PROGRAM L1C30O.
