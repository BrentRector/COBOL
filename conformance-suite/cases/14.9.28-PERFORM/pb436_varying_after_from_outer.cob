      *>--- conformance-case
      *> id: pb436_varying_after_from_outer
      *> rules: [GR-14.9.28.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB436 - PERFORM VARYING ... AFTER, the ORDER of ISO 14.9.28.4 GR13 e) 2's two operations.
      *>
      *> The order is INVISIBLE whenever an AFTER level's FROM operand is a literal or an independent item, and
      *> it decides the WHOLE iteration set the moment the FROM reads the level to its left - the ordinary
      *> triangular `AFTER B FROM A` idiom. Every pre-existing VARYING golden used literal FROM operands, so
      *> none of them could see it. This one is written so that it can.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULE AND NOT FROM A RUN.
      *>
      *> 14.9.28.4 GR12 - "Item identification for identifier-3, identifier-4, identifier-6, identifier-7,
      *>   index-name-2, and index-name-4 is done each time the content of the data item referenced by the
      *>   identifier or the index referenced by the index-name is used in a setting or augmenting operation",
      *>   and GR13's closing paragraph - "all changes to the induction variable, the variables associated with
      *>   the augment value, and the variables associated with the initialization value have immediate effect".
      *>   So an AFTER level's initialization value is RE-READ at every reset, from the CURRENT contents.
      *>
      *> 14.9.28.4 GR13 e) 2, true branch, VERBATIM AND IN ITS ORDER:
      *>   a. "the induction variable associated with the current condition is set to its initialization value"
      *>   b. "the condition to the left of the current condition becomes the current condition"
      *>   c. "the induction variable associated with the new current condition is incremented by its
      *>      associated augment value"
      *>   => the INNER level RESETS FIRST and the OUTER level augments SECOND, so B is reset from the
      *>      PRE-augment A. (The opposite order gives 3+2+1 = 6 bodies; this order gives 3+3+2 = 8.)
      *>
      *> OUTLINE / INLINE (2 levels, TEST BEFORE), A 1..3 and B from A:
      *>   e) 1 A=1 false -> e) 2 B=1,2,3 run the body and reach 4; true -> a. B := A = 1, c. A := 2;
      *>   again B=1,2,3 -> B := A = 2, A := 3;  again B=2,3 -> B := A = 3, A := 4;  e) 1 A>3 -> end.
      *>   8 bodies (1,1)(1,2)(1,3)(2,1)(2,2)(2,3)(3,2)(3,3); A=4, B=3.
      *>   GR4 - "An inline PERFORM statement and an out-of-line PERFORM statement function identically" -
      *>   so the two spellings below shall print the same three values and the same trace.
      *>
      *> THREE LEVELS (TEST BEFORE), A 1..2, B from A, C from B: e) 2's true branch walks LEFTWARDS one level
      *>   per step, and e) 2's false branch a. moves RIGHT without resetting, so C keeps its value across an
      *>   augment of A.  7 bodies (111)(112)(121)(122)(212)(221)(222); A=3, B=2, C=2.
      *>
      *> INDEX-NAME LEVELS (TEST BEFORE): 14.9.28.4 GR13 is written on "induction variables", which GR12 defines
      *>   to include "the indexes referenced by index-name-1 and index-name-3", so an index-name nest takes the
      *>   same order; the occurrence numbers repeat the 2-level answer above: 8 bodies, IX=4, JX=3.
      *>
      *> TEST AFTER (2 levels) IS THE CONTRAST ARM AND IS PINNED HERE SO NEITHER CAN DRIFT.  GR13 c) 4 is the
      *>   ONE sub-step of GR13 that states the increment BEFORE the reset - "the induction variable associated
      *>   with that condition is incremented by the associated augment value, all induction variables to the
      *>   right of the false condition are set to their initialization values" - so J is reset from the
      *>   POST-augment I: 4+3+2+1 = 10 bodies (1,1)(1,2)(1,3)(1,4)(2,2)(2,3)(2,4)(3,3)(3,4)(4,4); I=4, J=4.
      *>
      *> NIST NC201A PFM-TEST-F4-23 ("ORDER OF INITIALISATION OF VARYING IDENTIFIERS") asserts 6 for the first
      *> program below.  Per CLAUDE.md rule 1 the ISO text is the oracle and the CCVS corpus is a regression
      *> net; the divergence is recorded in tests/nist/corpus.tsv, pinned in SpecPinnedNistTests and determined
      *> in docs/CONFORMANCE.md section 3.  GR13 e) is worded identically in COBOL-85/2002/2014/2023 (no row in
      *> docs/VERSION_CHANGE_REFERENCE.md), so this 85 witness covers every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB436VARYAFTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TAB.
          05 WS-CELL PIC X OCCURS 5 TIMES INDEXED BY IX JX.
       01 A   PIC 9 VALUE 0.
       01 B   PIC 9 VALUE 0.
       01 C   PIC 9 VALUE 0.
       01 WI  PIC 9 VALUE 0.
       01 WJ  PIC 9 VALUE 0.
       01 N   PIC 99 VALUE 0.
      *> The trace fields are sized to the EXACT length the rule predicts (2 or 3 digits per body execution),
      *> so a short run leaves visible spaces and a long one is truncated by STRING: the width is part of the
      *> assertion, not padding.
       01 T2  PIC X(16) VALUE SPACES.
       01 T3  PIC X(21) VALUE SPACES.
       01 T5  PIC X(20) VALUE SPACES.
       01 P   PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
      *> --- GR13 e), two levels, OUT-OF-LINE ---
           MOVE SPACES TO T2
           MOVE 1 TO P
           MOVE 0 TO N
           PERFORM REC-AB VARYING A FROM 1 BY 1 UNTIL A > 3
                            AFTER B FROM A BY 1 UNTIL B > 3
           DISPLAY "OUTLINE-N=" N " A=" A " B=" B
           DISPLAY "OUTLINE-T=" T2
      *> --- GR13 e), two levels, INLINE (GR4: identical) ---
           MOVE SPACES TO T2
           MOVE 1 TO P
           MOVE 0 TO N
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
                     AFTER B FROM A BY 1 UNTIL B > 3
               ADD 1 TO N
               STRING A B DELIMITED BY SIZE INTO T2 WITH POINTER P
           END-PERFORM
           DISPLAY "INLINE -N=" N " A=" A " B=" B
           DISPLAY "INLINE -T=" T2
      *> --- GR13 e), three levels, each FROM the level to its left ---
           MOVE SPACES TO T3
           MOVE 1 TO P
           MOVE 0 TO N
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 2
                     AFTER B FROM A BY 1 UNTIL B > 2
                     AFTER C FROM B BY 1 UNTIL C > 2
               ADD 1 TO N
               STRING A B C DELIMITED BY SIZE INTO T3 WITH POINTER P
           END-PERFORM
           DISPLAY "LEVEL3 -N=" N " A=" A " B=" B " C=" C
           DISPLAY "LEVEL3 -T=" T3
      *> --- GR13 e), two INDEX-NAME levels (GR12: indexes are induction variables too) ---
           MOVE SPACES TO T2
           MOVE 1 TO P
           MOVE 0 TO N
           PERFORM VARYING IX FROM 1 BY 1 UNTIL IX > 3
                     AFTER JX FROM IX BY 1 UNTIL JX > 3
               ADD 1 TO N
               SET WI TO IX
               SET WJ TO JX
               STRING WI WJ DELIMITED BY SIZE INTO T2 WITH POINTER P
           END-PERFORM
           SET WI TO IX
           SET WJ TO JX
           DISPLAY "INDEXN -N=" N " I=" WI " J=" WJ
           DISPLAY "INDEXN -T=" T2
      *> --- GR13 c) 4, the CONTRAST arm: TEST AFTER increments BEFORE it resets ---
           MOVE SPACES TO T5
           MOVE 1 TO P
           MOVE 0 TO N
           PERFORM WITH TEST AFTER
                   VARYING WI FROM 1 BY 1 UNTIL WI > 3
                     AFTER WJ FROM WI BY 1 UNTIL WJ > 3
               ADD 1 TO N
               STRING WI WJ DELIMITED BY SIZE INTO T5 WITH POINTER P
           END-PERFORM
           DISPLAY "TSTAFT -N=" N " I=" WI " J=" WJ
           DISPLAY "TSTAFT -T=" T5
           STOP RUN.
       REC-AB.
           ADD 1 TO N
           STRING A B DELIMITED BY SIZE INTO T2 WITH POINTER P.
