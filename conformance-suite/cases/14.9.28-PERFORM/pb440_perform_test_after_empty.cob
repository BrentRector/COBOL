*>--- conformance-case
*> id: pb440_perform_test_after_empty
*> rules: [GR-14.9.28.4-4, GR-14.9.28.4-13]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB440 - PERFORM ... WITH TEST AFTER VARYING ... AFTER ... over an EMPTY procedure range
      *> (ISO 14.9.28.4 GR13 c) 1-4), beside its INLINE twin and beside the same phrase over a NON-EMPTY range.
      *> Before the fix the out-of-line form over an empty section was a no-op: GR13 a) never ran and no
      *> condition of any level was ever evaluated, so A and B kept their VALUE 7.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>
      *> GR13 a) - "All induction variables are set to their associated initialization values in the left-to-right
      *>   order in which the induction variables are specified" => A=1, B=1 before anything else. This happens
      *>   whether or not the specified set is empty, because GR5's transfer of control is what an empty set
      *>   lacks, not GR13 a)'s initialization.
      *> GR13 c) - with TEST AFTER and one AFTER phrase:
      *>     1. the specified set is executed;
      *>     2. the rightmost condition-2 (B > 3) is evaluated;
      *>     3. if it is false, B is incremented and execution resumes at the body;
      *>     4. if it is true, the condition to its left (A > 3) is evaluated. While THAT is false, A is
      *>        incremented, every induction variable to the right of it is set to its initialization value
      *>        (B := 1), and execution resumes at the body. When no condition is found to be false, control is
      *>        transferred to the end of the PERFORM statement.
      *>   (The "execution proceeds with step 13 a" cross-references in c) 3 and c) 4 are the standard's own
      *>   unedited internal references - taken literally they would re-initialize every induction variable and
      *>   never terminate - so the operational target is step c) 1. That reading is adjudicated in kb/Work
      *>   PB436 4, against the canonical PDF.)
      *>   Body-first therefore gives each level ONE more pass than TEST BEFORE would: A takes 1,2,3,4 and for
      *>   each of them B takes 1,2,3,4, so the body runs 4 x 4 = 16 times and the statement ends with the values
      *>   the last body execution saw (NOTE 6): A=4, B=4.
      *> GR4  - "An inline PERFORM statement and an out-of-line PERFORM statement function identically", so the
      *>   INLINE twin gives C=4, D=4 and a count of 16.
      *> GR5  - an EMPTY specified set has no first statement to transfer to, so the out-of-line empty form runs
      *>   the same 16-iteration scaffold over a body that does nothing: N stays 0 and TAIL-SEC never runs.
      *>
      *>   EMPTY    A=4 B=4 N=0000
      *>   INLINE   C=4 D=4 M=0016
      *>   NONEMPTY A=4 B=4 K=0016
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB440AFTEREMPTY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 7.
       01 B PIC 9 VALUE 7.
       01 C PIC 9 VALUE 7.
       01 D PIC 9 VALUE 7.
       01 N PIC 9(4) VALUE 0.
       01 M PIC 9(4) VALUE 0.
       01 K PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN SECTION.
       MAIN-P.
           PERFORM EMPTY-SEC WITH TEST AFTER
                   VARYING A FROM 1 BY 1 UNTIL A > 3
                     AFTER B FROM 1 BY 1 UNTIL B > 3
           DISPLAY "EMPTY    A=" A " B=" B " N=" N
           PERFORM WITH TEST AFTER
                   VARYING C FROM 1 BY 1 UNTIL C > 3
                     AFTER D FROM 1 BY 1 UNTIL D > 3
               ADD 1 TO M
           END-PERFORM
           DISPLAY "INLINE   C=" C " D=" D " M=" M
           MOVE 7 TO A
           MOVE 7 TO B
           PERFORM COUNT-P WITH TEST AFTER
                   VARYING A FROM 1 BY 1 UNTIL A > 3
                     AFTER B FROM 1 BY 1 UNTIL B > 3
           DISPLAY "NONEMPTY A=" A " B=" B " K=" K
           STOP RUN.
       COUNT-P.
           ADD 1 TO K.
       EMPTY-SEC SECTION.
       TAIL-SEC SECTION.
       TAIL-P.
           ADD 1 TO N.
