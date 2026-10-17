      *>--- conformance-case
      *> id: l1c22_call_positional_formals
      *> rules: [GR-14.2.3-2, GR-14.2.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.2.3 GR2/GR5 — CALL USING arguments bind to the USING
      *> formal parameters by position, not by name.
      *> RULE (14.2.3 GR2): "The correspondence between the arguments
      *> and the formal parameters is established on a positional
      *> basis."
      *> RULE (14.2.3 GR5): "Data-name-1 is a formal parameter for the
      *> function, method, or program."
      *> cite.py --check 14.2.3 "The correspondence between the
      *>   arguments and the formal parameters is established on a
      *>   positional basis" -> OK  §14.2.3 2)  (General rules)
      *> cite.py --check 14.2.3 "Data-name-1 is a formal parameter for
      *>   the function, method, or program" -> OK  §14.2.3 5)
      *> cite.py --check 14.2.3 "If the argument is passed by
      *>   reference, the activated runtime element operates as if the
      *>   formal parameter occupies the same storage area as the
      *>   argument" -> OK  §14.2.3 8)  (General rules)
      *> The caller and the callee both name their items X and Y, and
      *> the CALL passes them in the OPPOSITE order: USING Y X. A
      *> name-based binding would print the opposite of every line.
      *> DERIVATION of every output line:
      *>  In L1C22H, formal 1 is X and formal 2 is Y. Argument 1 is the
      *>  caller's Y ("BBB"), argument 2 the caller's X ("AAA"), so by
      *>  position the callee's X is "BBB" and its Y is "AAA":
      *>  "IN X=BBB Y=AAA".
      *>  The callee's X is a formal parameter (GR5) passed by reference
      *>  (the default: cite.py --check 14.2.3 "If neither the BY
      *>  REFERENCE nor the BY VALUE phrase is specified prior to the
      *>  first parameter, the BY REFERENCE phrase is assumed" -> OK
      *>  §14.2.3 4)), so it occupies the caller's Y's
      *>  storage (GR8): MOVE "ZZZ" TO X changes the caller's Y and
      *>  leaves the caller's X alone: "OUT X=AAA Y=ZZZ".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C22G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "AAA".
       01 Y PIC X(3) VALUE "BBB".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "L1C22H" USING Y X
           DISPLAY "OUT X=" X " Y=" Y
           STOP RUN.
       END PROGRAM L1C22G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C22H.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC X(3).
       01 Y PIC X(3).
       PROCEDURE DIVISION USING X Y.
       P-MAIN.
           DISPLAY "IN X=" X " Y=" Y
           MOVE "ZZZ" TO X
           EXIT PROGRAM.
       END PROGRAM L1C22H.
