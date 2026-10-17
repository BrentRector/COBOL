      *>--- conformance-case
      *> id: l1c17_linkage_index_separate
      *> rules: [GR-13.7.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.7.4 GR2 — linkage correspondence; index-names separate
      *> "The mechanism by which a correspondence is established between
      *>  the formal parameters and returning items described in the
      *>  linkage section and data items described in the activating
      *>  element is described in 14.2.3, General rules of the procedure
      *>  division. In the case of index-names, no such correspondence
      *>  is established and index-names in the activated and
      *>  activating source elements always refer to separate indices."
      *> cite.py --check 13.7.4 "index-names in the activated and
      *>   activating source elements always refer to separate indices"
      *>   -> OK §13.7.4 2)
      *> The caller passes table W-T (BY REFERENCE, the default) whose
      *> index is W-IX; the callee describes the same table as L-T with
      *> its OWN index L-IX.  The data item corresponds (the callee's
      *> store reaches the caller); the index-names do not.
      *>
      *> DERIVED OUTPUT:
      *>   CALLEE=E       callee: SET L-IX TO 5, L-E (L-IX) = "E" - the
      *>                  callee's index addresses the passed data.
      *>   CALLER-IX=C    back in the caller W-IX is still 3 (set before
      *>                  the CALL), so W-E (W-IX) = "C".  Had the
      *>                  indices been shared it would read "E".
      *>   CALLER-5=Z     the callee's MOVE "Z" TO L-E (L-IX) reached
      *>                  the caller's element 5: the data correspond.
      *>   CALLEE2=B      second CALL: the callee SETs L-IX TO 2 on its
      *>                  own; the caller's SET W-IX TO 4 in between
      *>                  does not affect it.
      *>   CALLER-IX2=D   W-IX = 4 -> "D", unaffected by the callee.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C17M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-T.
          05 W-E PIC X OCCURS 5 TIMES INDEXED BY W-IX.
       01 W-STEP PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCDE" TO W-T.
           SET W-IX TO 3.
           CALL "L1C17N" USING W-T W-STEP.
           DISPLAY "CALLER-IX=" W-E (W-IX).
           DISPLAY "CALLER-5=" W-E (5).
           SET W-IX TO 4.
           MOVE 2 TO W-STEP.
           CALL "L1C17N" USING W-T W-STEP.
           DISPLAY "CALLER-IX2=" W-E (W-IX).
           STOP RUN.
       END PROGRAM L1C17M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C17N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-T.
          05 L-E PIC X OCCURS 5 TIMES INDEXED BY L-IX.
       01 L-STEP PIC 9.
       PROCEDURE DIVISION USING L-T L-STEP.
       P.
           IF L-STEP = 1
               SET L-IX TO 5
               DISPLAY "CALLEE=" L-E (L-IX)
               MOVE "Z" TO L-E (L-IX)
           ELSE
               SET L-IX TO 2
               DISPLAY "CALLEE2=" L-E (L-IX)
           END-IF.
           EXIT PROGRAM.
       END PROGRAM L1C17N.
