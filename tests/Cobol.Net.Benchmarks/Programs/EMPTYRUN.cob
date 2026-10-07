      *> kb/Work PB2117 (A6's instrument) -- the start-up floor.
      *> A program that does nothing but DISPLAY its witness, timed as a
      *> whole process beside the hot paths, so perf_baseline.py
      *> can state each implementation's process start-up cost and the
      *> hot path's time net of it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EMPTYRUN.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "EMPTYRUN"
           STOP RUN.
