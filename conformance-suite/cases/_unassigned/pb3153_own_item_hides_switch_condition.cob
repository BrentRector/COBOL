      *>--- conformance-case
      *> id: pb3153_own_item_hides_switch_condition
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3153 (review w1085d R1-1) - a SPECIAL-NAMES
      *>   switch-status condition-name a containing program declares,
      *>   written as a bare condition, the position that also admits
      *>   a simple boolean condition on a data item of that spelling.
      *> PB53SW sets SWITCH-1 ON, so PB53ON is true and PB53OFF false.
      *> PB53SW1 describes no item of those spellings, so each word is
      *>   the inherited switch-status condition-name (12.3.4 GR1):
      *>   SW-ON, then NOT PB53OFF and EVALUATE TRUE WHEN PB53ON.
      *> PB53SW2 describes boolean items PB53ON (0) and PB53OFF (1).
      *>   "A source element's reference to such a name, even when it
      *>   is a different type of user-defined word, is to the item
      *>   which that source element describes" (8.4.6.1), so each bare
      *>   word is the simple boolean condition on its own item, true
      *>   when the item is 1 (8.8.4.3.4 GR1): OWN-FALSE and then
      *>   OWN-OFF-TRUE, the opposite of both switch readings, and after
      *>   the MOVE, OWN-TRUE; the parenthesized (PB53ON) and the
      *>   EVALUATE object read the item too. Neither is a use of the
      *>   name, so no word is two types of user-defined word (8.3.2.2).
      *> cite.py --check 8.4.6.1 "a source element's reference to such
      *>   a name, even when it is a different type of user-defined
      *>   word, is to the item which that source element describes"
      *>   -> OK 8.4.6.1; 8.8.4.3.4 "Boolean-expression-1 evaluates
      *>   true if the result of the expression is 1" -> OK 8.8.4.3.4
      *>   1); 8.3.2.2 "a given user-defined word may be used as only
      *>   one type of user-defined word" -> OK 8.3.2.2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB53SW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS PB53S1 ON STATUS IS PB53ON
                              OFF STATUS IS PB53OFF.
       PROCEDURE DIVISION.
       MAIN-P.
           SET PB53S1 TO ON.
           CALL "PB53SW1".
           CALL "PB53SW2".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB53SW1.
       PROCEDURE DIVISION.
       MAIN-P.
           IF PB53ON DISPLAY "SW-ON" ELSE DISPLAY "SW-OFF" END-IF.
           IF NOT PB53OFF DISPLAY "SW-NOT-OFF" END-IF.
           EVALUATE TRUE
               WHEN PB53ON DISPLAY "EV-SW-ON"
               WHEN OTHER DISPLAY "EV-SW-OTHER"
           END-EVALUATE.
           EXIT PROGRAM.
       END PROGRAM PB53SW1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB53SW2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB53ON PIC 1 VALUE B"0".
       01 PB53OFF PIC 1 VALUE B"1".
       PROCEDURE DIVISION.
       MAIN-P.
           IF PB53ON DISPLAY "OWN-TRUE" ELSE DISPLAY "OWN-FALSE" END-IF.
           IF PB53OFF DISPLAY "OWN-OFF-TRUE" END-IF.
           MOVE B"1" TO PB53ON.
           IF PB53ON DISPLAY "OWN-TRUE" ELSE DISPLAY "OWN-FALSE" END-IF.
           IF (PB53ON) DISPLAY "PAREN-TRUE" END-IF.
           EVALUATE TRUE
               WHEN PB53ON DISPLAY "EV-OWN-TRUE"
               WHEN OTHER DISPLAY "EV-OWN-OTHER"
           END-EVALUATE.
           EXIT PROGRAM.
       END PROGRAM PB53SW2.
       END PROGRAM PB53SW.
