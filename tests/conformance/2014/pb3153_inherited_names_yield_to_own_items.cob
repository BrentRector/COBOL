      *> kb/Work PB3153 (review round 1) - a SPECIAL-NAMES word a
      *>   containing program declares, at the four positions that admit
      *>   another reading of the same word as well.
      *> PB3153Y1 has no item of those spellings, so each word is the
      *>   inherited name (12.3.4 GR1; 8.4.6.1 "may be referenced only
      *>   by statements and entries either in that source element that
      *>   contains a configuration section or in any source element
      *>   contained within that source element"): HI IN PB53GRP is the
      *>   EVALUATE range's alphabet phrase (C is within A THRU M), the
      *>   trailing PB53KZ is the VALUE clause's IN-omitted alphabet
      *>   phrase (C is within A THRU D), PB53LVL is STANDARD-COMPARE's
      *>   ordering-name ("a" before "b"), and PB53KQ is the coded
      *>   character at ordinal position 66 of the native set, "A".
      *> PB3153Y2 describes items of the same spellings, and "a source
      *>   element's reference to such a name, even when it is a
      *>   different type of user-defined word, is to the item which
      *>   that source element describes" (8.4.6.1): HI IN PB53GRP is a
      *>   qualified reference to its own HI (Q is not within A THRU M),
      *>   PB53KZ is its own constant-name "Q", one more VALUE, PB53LVL
      *>   is argument-4 (ordering level 1), and PB53KQ is its own
      *>   constant-name "Z". None is a use of the inherited name, so no
      *>   word is two types of user-defined word (8.3.2.2).
      *> cite.py --check 8.4.6.1 "a source element's reference to such
      *>   a name, even when it is a different type of user-defined
      *>   word, is to the item which that source element describes"
      *>   -> OK 8.4.6.1; 12.3.4 "apply to each directly or indirectly
      *>   contained source unit" -> OK 12.3.4 1); 8.3.2.2 "a given
      *>   user-defined word may be used as only one type of
      *>   user-defined word" -> OK 8.3.2.2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3153Y.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET PB53GRP IS NATIVE
           ALPHABET PB53KZ IS NATIVE
           SYMBOLIC CHARACTERS PB53KQ IS 66
           ORDER TABLE PB53LVL IS "ISO 14651_2020_TABLE1".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB3153Y1".
           CALL "PB3153Y2".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3153Y1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 HI PIC X VALUE "M".
       01 X PIC X VALUE "C".
          88 XIN VALUE "A" THRU "D" PB53KZ.
       01 Y PIC X VALUE PB53KQ.
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE X
               WHEN "A" THRU HI IN PB53GRP
                   DISPLAY "IN"
               WHEN OTHER
                   DISPLAY "OUT"
           END-EVALUATE.
           IF XIN DISPLAY "Y" ELSE DISPLAY "N" END-IF.
           DISPLAY FUNCTION STANDARD-COMPARE("a" "b" PB53LVL).
           DISPLAY Y.
           EXIT PROGRAM.
       END PROGRAM PB3153Y1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3153Y2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB53GRP.
          05 HI PIC X VALUE "M".
       01 PB53KZ CONSTANT AS "Q".
       01 PB53KQ CONSTANT AS "Z".
       01 PB53LVL PIC 9 VALUE 1.
       01 X PIC X VALUE "Q".
          88 XQ VALUE "A" THRU "C" PB53KZ.
       01 Y PIC X VALUE PB53KQ.
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE X
               WHEN "A" THRU HI IN PB53GRP
                   DISPLAY "IN"
               WHEN OTHER
                   DISPLAY "OUT"
           END-EVALUATE.
           IF XQ DISPLAY "Y" ELSE DISPLAY "N" END-IF.
           DISPLAY FUNCTION STANDARD-COMPARE("a" "b" PB53LVL).
           DISPLAY Y.
           EXIT PROGRAM.
       END PROGRAM PB3153Y2.
       END PROGRAM PB3153Y.
