      *>--- conformance-case
      *> id: pb396_required_imperative_85
      *> rules: [GR-14.9.13.4-5, FMT-14.9.13.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB396 - THE POSITIVE CONTROL for the required imperative-statement operand, at the edition
      *> that INTRODUCED every one of these formats (COBOL-85). The rule is edition-independent, so this is the
      *> witness edition; the must-REJECT twins are tests/conformance/negative/pb396-*.cob.
      *>
      *> WHY IT EXISTS: `statementBlock*` in the control-flow grammar restored the zero case that ISO
      *> 5.2.6.2/5.2.6.3 forbid, and every one of the phrase positions below used to accept an EMPTY body in
      *> silence. Tightening the quantifier is only half a fix — this program is the other half: each position
      *> written WITH its imperative must still compile and must still produce the value the general rules give
      *> it, or the tightening would have been "satisfied" by refusing the construct.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *> 14.9.19.4 GR1 - condition-1 true => control to the first statement of statement-1, ELSE ignored
      *>                 => IF-THEN.
      *> 14.9.19.4 GR2 - condition-1 false => the THEN phrase is ignored and control goes to statement-2
      *>                 => IF-ELSE-2.
      *> 14.9.13.4 GR4 d) + GR5 a) - the WHEN phrases are analysed left to right and the selected phrase's
      *>                 imperative-statement-1 runs; consecutive WHEN phrases share the ONE imperative that
      *>                 follows them, so WHEN 7 / WHEN 1 both select it => EVAL-SHARED.
      *> 14.9.13.4 GR5 b) - no WHEN phrase selected and a WHEN OTHER phrase specified => imperative-statement-2
      *>                 => EVAL-OTHER.
      *> 14.9.13.4 GR5 c) - the statement is terminated "when no WHEN phrase is selected and no WHEN OTHER
      *>                 phrase is specified", so the third EVALUATE runs nothing and control reaches the next
      *>                 statement => EVAL-FALLTHROUGH, with EVAL-NO-MATCH-TAKEN absent. GR5 c)'s first clause
      *>                 is the same witness read the other way: EVAL-SHARED prints once and EVAL-NOT-TAKEN
      *>                 never, because the statement ends at the end of the selected arm.
      *> 14.9.28.4 GR9/GR10 - a Format-2 inline PERFORM with UNTIL and no TEST phrase tests the condition
      *>                 BEFORE each execution of imperative-statement-1, so N is augmented until N >= 3 and
      *>                 the loop ends with N = 3 => PERFORM-N=3.
      *> 14.9.37.4 GR5 - the serial SEARCH evaluates condition-1 at successive index values; R (2) = 7 is the
      *>                 first true, the index is left at that occurrence, and the WHEN's imperative runs
      *>                 (here a TWO-statement imperative, since 14.9.37.2's brace admits imperative-statement-2
      *>                 and 14.9.19.3 SR1's "one or more imperative statements" is the shape of every such
      *>                 operand) => SEARCH-N=2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB396REQIMP85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X    PIC 9 VALUE 1.
       01 N    PIC 9 VALUE 0.
       01 T.
          05 R PIC 9 OCCURS 3 TIMES INDEXED BY I.
       PROCEDURE DIVISION.
       MAIN-P.
           IF X = 1
               DISPLAY "IF-THEN"
           ELSE
               DISPLAY "IF-ELSE"
           END-IF
           IF X = 9
               DISPLAY "IF-THEN-2"
           ELSE
               DISPLAY "IF-ELSE-2"
           END-IF
           EVALUATE X
               WHEN 7
               WHEN 1
                   DISPLAY "EVAL-SHARED"
               WHEN OTHER
                   DISPLAY "EVAL-NOT-TAKEN"
           END-EVALUATE
           EVALUATE X
               WHEN 7
                   DISPLAY "EVAL-SEVEN"
               WHEN OTHER
                   DISPLAY "EVAL-OTHER"
           END-EVALUATE
           EVALUATE X
               WHEN 7
                   DISPLAY "EVAL-NO-MATCH-TAKEN"
           END-EVALUATE
           DISPLAY "EVAL-FALLTHROUGH"
           PERFORM UNTIL N >= 3
               ADD 1 TO N
           END-PERFORM
           DISPLAY "PERFORM-N=" N
           MOVE 0 TO R (1)
           MOVE 7 TO R (2)
           MOVE 0 TO R (3)
           SET I TO 1
           SEARCH R
               AT END DISPLAY "SEARCH-NOTFOUND"
               WHEN R (I) = 7
                   SET N TO I
                   DISPLAY "SEARCH-N=" N
           END-SEARCH
           STOP RUN.
