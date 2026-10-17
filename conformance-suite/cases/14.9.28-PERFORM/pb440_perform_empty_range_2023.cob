*>--- conformance-case
*> id: pb440_perform_empty_range_2023
*> rules: [GR-14.9.28.4-4, GR-14.9.28.4-5, GR-14.9.28.4-6,
*>    GR-14.9.28.4-8, GR-14.9.28.4-9, GR-14.9.28.4-13]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB440 - PERFORM of an EMPTY procedure range. A section with ZERO paragraphs is legal:
      *> ISO 14.4.2 - "A section consists of a section header followed by zero, one, or more successive
      *> paragraphs". It makes 14.9.28.4 GR4's SPECIFIED SET OF STATEMENTS empty, and an empty set is a set.
      *>
      *> Two arms of one root cause were wrong and each is measured below.
      *>   ARM A - the binder returned a no-op for `start > end`, DELETING the whole control phrase with the
      *>           body, so GR13 a) never ran: the induction variables kept whatever a preceding MOVE left
      *>           in them and no condition of any level was ever evaluated.
      *>   ARM B - with THRU written that test was skipped and the dispatcher was handed the inverted pair
      *>           (start, start-1). Its return test (`__atExit && __pc == __exitPc + 1`) can never fire on
      *>           such a pair, so it ran from the range start to the END OF THE PC SPACE - the FOLLOWING
      *>           sections' statements, once per iteration of the control phrase.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>
      *> 14.9.28.4 GR4 - "An inline PERFORM statement and an out-of-line PERFORM statement function
      *>   identically according to the following rules." The first two lines are the SAME control phrase
      *>   written both ways over the same (empty) body, so they shall agree digit for digit.
      *> 14.9.28.4 GR13 a) - "All induction variables are set to their associated initialization values in
      *>   the left-to-right order in which the induction variables are specified" => A=1, B=1 BEFORE any
      *>   transfer of control. VALUE 7 is therefore erased whatever the body does.
      *> 14.9.28.4 GR13 e) - condition-1 (A > 3) is evaluated first; while it is false the AFTER condition
      *>   (B > 3) is the current one, and each time IT goes true its induction variable is RESET to its
      *>   initialization value and the variable to its left is augmented. So B walks 1,2,3 then resets to 1
      *>   while A becomes 2 ... and the statement ends when A reaches 4 with B just reset:
      *>       A=4 B=1, and identically C=4 D=1.
      *> 14.9.28.4 GR5 - "control is transferred to the first statement of the specified set of statements":
      *>   an EMPTY set has no first statement, so NO transfer takes place and TAIL-SEC never runs. Every
      *>   N below is therefore 0 - PLAIN (GR8, the set executed once), THRUBASIC (GR8 over the empty THRU
      *>   composition), THRUTIMES (GR9, three iterations of nothing), THRUVAR (GR13 over the same set).
      *>   Before the fix these read N=1, N=3 and nine executions of TAIL-SEC respectively.
      *> 14.9.28.4 GR10 - "If the condition is true when the PERFORM statement is entered, and the TEST
      *>   BEFORE phrase is specified or implied, no transfer to the specified set of statements takes
      *>   place": ZEROTRIP's condition N = 0 holds on entry, so the statement is a zero-trip loop => N=0.
      *> ⛔ THE INLINE TWIN'S BODY IS `CONTINUE`, AND THAT IS THE RULE, NOT A CONCESSION (kb/Work PB396).
      *> 14.9.28.2 Format 2 prints `PERFORM [ times|until|varying ] imperative-statement-1 END-PERFORM`
      *> with imperative-statement-1 UNBRACKETED, and 5.2.6.2 gives the omission licence to BRACKETED
      *> portions only — so the standard offers NO inline spelling of an empty imperative-statement-1.
      *> The EMPTY procedure RANGE this program is about is an out-of-line fact (14.4.2 lets a section
      *> consist of zero paragraphs); its GR4 inline twin is a body that DOES NOTHING, which 14.9.9 spells
      *> CONTINUE — "a no operation" — so the loop's induction-variable behaviour, and this program's
      *> expected output, are unchanged. Written empty, the statement parsed only because the grammar
      *> said `statementBlock*`; it is now COBOLNET2072.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB440EMPTY23.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 7.
       01 B PIC 9 VALUE 7.
       01 C PIC 9 VALUE 7.
       01 D PIC 9 VALUE 7.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN SECTION.
       MAIN-P.
           PERFORM EMPTY-SEC
                   VARYING A FROM 1 BY 1 UNTIL A > 3
                     AFTER B FROM 1 BY 1 UNTIL B > 3
           PERFORM VARYING C FROM 1 BY 1 UNTIL C > 3
                     AFTER D FROM 1 BY 1 UNTIL D > 3
               CONTINUE
           END-PERFORM
           DISPLAY "OUTOFLINE A=" A " B=" B
           DISPLAY "INLINE    C=" C " D=" D
           MOVE 0 TO N
           PERFORM EMPTY-SEC
           DISPLAY "PLAIN N=" N
           MOVE 0 TO N
           PERFORM EMPTY-SEC THRU EMPTY-SEC
           DISPLAY "THRUBASIC N=" N
           MOVE 0 TO N
           PERFORM EMPTY-SEC THRU EMPTY-SEC 3 TIMES
           DISPLAY "THRUTIMES N=" N
           MOVE 0 TO N
           MOVE 7 TO A
           MOVE 7 TO B
           PERFORM EMPTY-SEC THRU EMPTY-SEC
                   VARYING A FROM 1 BY 1 UNTIL A > 3
                     AFTER B FROM 1 BY 1 UNTIL B > 3
           DISPLAY "THRUVAR A=" A " B=" B " N=" N
           MOVE 0 TO N
           PERFORM EMPTY-SEC UNTIL N = 0
           DISPLAY "ZEROTRIP N=" N
           STOP RUN.
       EMPTY-SEC SECTION.
       TAIL-SEC SECTION.
       TAIL-P.
           ADD 1 TO N.
