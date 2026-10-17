      *>--- conformance-case
      *> id: l1c21_common_program_visibility
      *> rules: [GR-11.10.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.10.4 GR2 — a COMMON program is callable beyond parent
      *> "The COMMON clause specifies that the program is common. A
      *> common program is contained within another program but may be
      *> called from programs other than that containing it as stated
      *> in 8.4.6, Scope of names."
      *>   cite.py --check 11.10.4 -> OK  §11.10.4 2)  (General rules)
      *> The scope rules it defers to:
      *>   §8.4.6.3 2) a COMMON program directly contained in P "may be
      *>   referenced only by statements included in that containing
      *>   program and any programs directly or indirectly contained
      *>   within that containing program" (except itself and its own
      *>   containees unless recursive — not exercised here).
      *>   cite.py --check 8.4.6.3 -> OK  §8.4.6.3 2)  (Scope of
      *>   program-names)
      *>   §8.4.6.3 1) a NON-common program directly contained in P
      *>   "may be referenced only by statements included in that
      *>   containing program or, if the program possesses the
      *>   recursive attribute, in the program itself."
      *>   cite.py --check 8.4.6.3 -> OK  §8.4.6.3 1)  (Scope of
      *>   program-names)
      *>   §14.9.4.4 GR3b "If the program cannot be located ... the
      *>   EC-PROGRAM-NOT-FOUND exception condition is set to exist" and
      *>   the call is not successful -> the ON EXCEPTION phrase runs.
      *>   cite.py --check 14.9.4.4 -> OK  §14.9.4.4 3)  (General rules)
      *> Structure: L1C21B contains L1C21C (COMMON), L1C21N (not
      *> common) and L1C21S (not common); L1C21S contains L1C21T.
      *> L1C21C is not INITIAL, so its counter keeps its last-used
      *> value between calls (§8.6.6).
      *> Expected output, derived:
      *>   "B START"            — first statement of L1C21B.
      *>   "C RUNS 1"           — B calls C: B is C's container.
      *>   "N RUNS"             — B calls N: B is N's containing program
      *>                          (§8.4.6.3 1), so N is visible to B.
      *>   "C RUNS 2"           — S calls C: S is a sibling, not C's
      *>                          container; GR2 / §8.4.6.3 2) make the
      *>                          COMMON program visible to it.
      *>   "S: N NOT VISIBLE"   — S calls N: N is not common, S is not
      *>                          its container, so N cannot be located
      *>                          -> EC-PROGRAM-NOT-FOUND
      *>                          -> ON EXCEPTION.
      *>   "C RUNS 3"           — T (INDIRECTLY contained in B) calls C:
      *>                          still within §8.4.6.3 2)'s scope.
      *>   "B END"              — back in B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21B.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "B START"
           CALL "L1C21C"
           CALL "L1C21N"
           CALL "L1C21S"
           DISPLAY "B END"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21C IS COMMON PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD 1 TO CNT
           DISPLAY "C RUNS " CNT
           EXIT PROGRAM.
       END PROGRAM L1C21C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N RUNS"
           EXIT PROGRAM.
       END PROGRAM L1C21N.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21S.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "L1C21C"
           CALL "L1C21N"
               ON EXCEPTION DISPLAY "S: N NOT VISIBLE"
               NOT ON EXCEPTION DISPLAY "S: N CALLED"
           END-CALL
           CALL "L1C21T"
           EXIT PROGRAM.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "L1C21C"
               ON EXCEPTION DISPLAY "T: C NOT VISIBLE"
           END-CALL
           EXIT PROGRAM.
       END PROGRAM L1C21T.
       END PROGRAM L1C21S.
       END PROGRAM L1C21B.
