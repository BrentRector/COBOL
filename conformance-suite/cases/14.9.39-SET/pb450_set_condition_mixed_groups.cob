*>--- conformance-case
*> id: pb450_set_condition_mixed_groups
*> rules: [GR-14.9.39.4-8, FMT-14.9.39.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> !! THE GROUPS OF ONE FORMAT-4 STATEMENT NEED NOT AGREE. kb/Work PB450.
      *>
      *> ISO 14.9.39.2 Format 4 repeats the WHOLE `{ condition-name-1 } ... TO { TRUE | FALSE }` unit
      *> ("The outer braces enclose the single repeated unit; the trailing `...` repeats the whole braced
      *> portion"), so the TRUE/FALSE choice belongs to each GROUP and not to the statement. A binder that
      *> read the keyword once per statement would store the first group's arm into every group.
      *>
      *> Expected values, COMPUTED FROM THE STANDARD (not measured):
      *>   14.9.39.4 GR8 - "If multiple condition-names are specified, the results are the same as if a
      *>   separate SET statement had been written for each condition-name-1" - so each group acts in
      *>   WRITTEN ORDER and a later group may overwrite an earlier one's conditional variable.
      *>   GR6 places "the literal in the VALUE clause" (13.18.63.4 GR20's FIRST literal) for a TRUE group;
      *>   GR7 places "the literal in the FALSE phrase of the VALUE clause" for a FALSE group, both
      *>   "according to the rules for the VALUE clause".
      *>     SET P-YES TO TRUE Q-YES TO FALSE   -> WS-P = "Y", WS-Q = "N"
      *>     SET P-YES TO FALSE Q-YES TO TRUE P-YES TO TRUE
      *>                                        -> group 1 stores "N" into WS-P, group 2 stores "Y" into
      *>                                           WS-Q, group 3 stores "Y" back into WS-P: WS-P = "Y".
      *>   8.8.4.5.3 GR2 then decides each IF against the value the store left.
      *>
      *> The FALSE arm is a COBOL-2002 addition (constructs.json set-condition-false-2002 /
      *> value-false-phrase-2002), which is why the mixed form lives here and its TRUE-only twin in the 85
      *> corpus (85/pb450_set_outer_repetition). negative/pb450-set-false-in-second-group-below-2002 pins
      *> the edge below this one, and pins it on the SECOND group - the position a statement-level gate
      *> could not see.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB450MIXGRP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-P PIC X VALUE "N".
          88 P-YES VALUE "Y" WHEN SET TO FALSE "N".
       01 WS-Q PIC X VALUE "N".
          88 Q-YES VALUE "Y" WHEN SET TO FALSE "N".
       PROCEDURE DIVISION.
       MAIN-P.
           SET P-YES TO TRUE Q-YES TO FALSE
           DISPLAY "P=" WS-P " Q=" WS-Q
           IF P-YES DISPLAY "P-ON=yes" ELSE DISPLAY "P-ON=no" END-IF
           IF Q-YES DISPLAY "Q-ON=yes" ELSE DISPLAY "Q-ON=no" END-IF
           SET P-YES TO FALSE Q-YES TO TRUE P-YES TO TRUE
           DISPLAY "P2=" WS-P " Q2=" WS-Q
           DISPLAY "DONE"
           STOP RUN.
