      *> kb/Work PB1668. ISO 1989:2023 14.9.13: EVALUATE's selection subject and selection object may each be the
      *> word TRUE or FALSE (Table 15: the TRUE-or-FALSE row against the TRUE-or-FALSE column is 'Y').
      *>   GR3 f) the subject word is assigned a truth value - true for TRUE, false for FALSE;
      *>   GR4 a) 4. "If the truth value of the selection subject and selection object match, the result of the
      *>   analysis is true. If they do not match, the result is false."
      *>   cite.py --check 14.9.13.4 "Any selection subject specified by the words TRUE or FALSE is assigned a
      *>     truth value." -> OK 3) f)
      *>   cite.py --check 14.9.13.4 "If the selection object is either TRUE or FALSE, the selection subject is
      *>     condition-1." -> OK 4) a) 4.
      *> Each EVALUATE below has exactly one matching WHEN, so each leg fails if its pair's truth values are
      *> compared wrongly. Before this change the word object was bound as a condition, which refused conforming
      *> source through the COBOLNET2319 internal-error net.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1668EVT.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE TRUE
              WHEN TRUE DISPLAY "T-T"
              WHEN OTHER DISPLAY "T-O"
           END-EVALUATE
           EVALUATE TRUE
              WHEN FALSE DISPLAY "TF-F"
              WHEN OTHER DISPLAY "TF-O"
           END-EVALUATE
           EVALUATE FALSE
              WHEN TRUE DISPLAY "FT-T"
              WHEN FALSE DISPLAY "FT-F"
           END-EVALUATE
           EVALUATE FALSE
              WHEN FALSE DISPLAY "FF-F"
              WHEN OTHER DISPLAY "FF-O"
           END-EVALUATE
           EVALUATE TRUE ALSO FALSE
              WHEN TRUE ALSO TRUE DISPLAY "A-TT"
              WHEN FALSE ALSO FALSE DISPLAY "A-FF"
              WHEN TRUE ALSO FALSE DISPLAY "A-TF"
              WHEN OTHER DISPLAY "A-O"
           END-EVALUATE
           STOP RUN.
