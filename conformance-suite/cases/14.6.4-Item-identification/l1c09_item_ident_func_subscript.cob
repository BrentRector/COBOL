      *>--- conformance-case
      *> id: l1c09_item_ident_func_subscript
      *> rules: [GR-14.6.4-2, GR-14.6.4-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.6.4 2) 4) — function evaluation and subscript
      *>            evaluation
      *> both precede reference modification in item identification.
      *> Rule (§14.6.4): "The item identification steps that are
      *> applicable to that identifier are evaluated in the following
      *> order: ... 2) function evaluation ... 4) subscript evaluation
      *> ... 7) reference modification", and "If a step in the
      *> evaluation of an identifier requires evaluation of another
      *> identifier or an arithmetic expression, that evaluation is
      *> done in full before proceeding to the next step."
      *> cite.py: OK  §14.6.4 2)  (Item identification) function
      *>            evaluation
      *> cite.py: OK  §14.6.4 4)  (Item identification) subscript
      *>            evaluation
      *> cite.py: OK  §14.6.4 7)  (Item identification) reference
      *>            modification
      *> cite.py: OK  §14.6.4   (Item identification) "that evaluation
      *>            is
      *>          done in full before proceeding to the next step"
      *> L1C09F (CTR) adds 1 to the EXTERNAL counter K and returns K;
      *> L1C09G (STRF) adds 1 to K and returns "wxyz". Each activation
      *> is therefore time-stamped by K.
      *> Line 1: MOVE FUNCTION L1C09G (FUNCTION L1C09F : 1) with K=0.
      *>   Step 2 (function evaluation of L1C09G) runs first: K=1,
      *>   value "wxyz". Step 7 (reference modification) then evaluates
      *>   its leftmost position: L1C09F -> K=2, returns 2. Position 2
      *>   length 1 of "wxyz" = "x". -> "F X=x K=2". (A modifier
      *>   evaluated first would give start 1 -> "w".)
      *> Line 2: MOVE E (FUNCTION L1C09F) (FUNCTION L1C09F : 1), K=0,
      *>   E = "abcd" "efgh" "ijkl". Step 4 (subscript) first: K=1,
      *>   E(1)="abcd"; step 7 then: K=2, start 2 -> "b".
      *>   -> "S X=b K=2". (Modifier first: start 1, E(2) -> "e".)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. L1C09F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 EXTERNAL.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
           ADD 1 TO K.
           MOVE K TO R.
           GOBACK.
       END FUNCTION L1C09F.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. L1C09G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 EXTERNAL.
       LINKAGE SECTION.
       01 R PIC X(4).
       PROCEDURE DIVISION RETURNING R.
           ADD 1 TO K.
           MOVE "wxyz" TO R.
           GOBACK.
       END FUNCTION L1C09G.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09H.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION L1C09F
           FUNCTION L1C09G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 EXTERNAL.
       01 X PIC X.
       01 TBL.
          05 E PIC X(4) OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 0 TO K.
           MOVE FUNCTION L1C09G (FUNCTION L1C09F : 1) TO X.
           DISPLAY "F X=" X " K=" K.
           MOVE "abcd" TO E (1).
           MOVE "efgh" TO E (2).
           MOVE "ijkl" TO E (3).
           MOVE 0 TO K.
           MOVE E (FUNCTION L1C09F) (FUNCTION L1C09F : 1) TO X.
           DISPLAY "S X=" X " K=" K.
           STOP RUN.
       END PROGRAM L1C09H.
