      *> ISO 8.4.2.3.2 / 8.4.3.3.3 SR4 - a FUNCTION-IDENTIFIER is legal in a
      *> SUBSCRIPT and in a REFERENCE-MODIFIER position (fix-queue PB17; design
      *> COBOLNET_DATA_MODEL_DESIGN.md D18). The chain: 8.4.3.1.2 Format 1 makes a
      *> function-identifier an identifier -> 15.4 "the evaluation of a function
      *> produces a returned value in a temporary elementary data item" -> 15.2
      *> items 5-6 put integer and numeric functions "of the class and category
      *> numeric" -> 8.8.1.1 "an arithmetic expression may be an identifier
      *> referencing a numeric data item" -> 8.4.2.3.2 + 8.4.2.3.4 GR1b admit
      *> arithmetic-expression-1 as a subscript, and 8.4.3.3.3 SR4 as a
      *> leftmost-position/length.
      *>
      *> BOTH SHAPES COMPILED CLEAN AND THREW NotImplementedCobolFeatureException
      *> AT RUN TIME before D18 - the PB7/DA7 wrong-stage family. The segment now
      *> re-parses through the isolated subscriptExpressionFragment rule and binds
      *> through the ONE ExpressionBinder.BindExpr, so the nested and USER-DEFINED
      *> forms below fall out of the same change rather than needing their own arms.
      *>
      *> 8.4.3.2.3 SR11/SR12 do NOT bar this: they bar functions where an INTEGER
      *> or UNSIGNED INTEGER is required, and a subscript is neither - GR1b sets
      *> EC-BOUND-SUBSCRIPT for a non-integer RESULT, a run-time condition that
      *> would be pointless if the position required an integer syntactically.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB17PICK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           COMPUTE L-R = L-X + 2.
           GOBACK.
       END FUNCTION PB17PICK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB17FNSUB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB17PICK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-G.
          05 W-E PIC 9(2) OCCURS 5 TIMES.
       01 W-H.
          05 W-F PIC 9(2) OCCURS 5 TIMES.
       01 W-A PIC X(9) VALUE "ABCDEFGHI".
       01 W-R PIC 9(2).
       01 W-T PIC X(2).
       01 W-X PIC 9(4) VALUE 1.
       01 W-I PIC 9 VALUE 1.
       01 W-K PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 11 TO W-E (1).
           MOVE 22 TO W-E (2).
           MOVE 33 TO W-E (3).
           MOVE 3 TO W-F (2).
      *> 1 - the SUBSCRIPT shape. FUNCTION INTEGER(3) = 3 -> W-E(3) = 33.
           MOVE W-E (FUNCTION INTEGER(3)) TO W-R.
           DISPLAY "SUB=" W-R.
      *> 2 - the REF-MOD leftmost-position shape. Position 3, length 2.
           MOVE W-A (FUNCTION INTEGER(3):2) TO W-T.
           DISPLAY "RM=[" W-T "]".
      *> 3 - NESTED. The inner segment's 15.4 temp must store BEFORE the outer
      *> one's, which is why the segment binds at RESOLVE time and not at the
      *> pending-list drain: W-F(2) = 3, so the outer names W-E(3) = 33.
           MOVE W-E (FUNCTION INTEGER(W-F (FUNCTION INTEGER(2)))) TO W-R.
           DISPLAY "NESTED=" W-R.
      *> 4 - a USER-DEFINED function as a subscript. The fragment binds through
      *> BindIntrinsicCore, which 12.3.8.2 GR12 dispatches to the REPOSITORY name.
           MOVE W-E (FUNCTION PB17PICK(W-X)) TO W-R.
           DISPLAY "UDFSUB=" W-R.
      *> 5 - THE 8.8.4.13 r2 PER-EVALUATION WINDOW, decisively. The subscript's
      *> OWN operand changes inside the loop, so a temp hoisted ONCE out of the
      *> PERFORM UNTIL condition would pin the subscript at 1 and W-E(1) = 11
      *> would never equal 33 - a non-terminating loop. Terminating with W-I = 3
      *> is what proves the store re-runs per condition evaluation.
           PERFORM UNTIL W-E (FUNCTION INTEGER(W-I)) = 33
               ADD 1 TO W-I
               ADD 1 TO W-K
               IF W-K > 8
                   DISPLAY "RUNAWAY"
                   STOP RUN
               END-IF
           END-PERFORM.
           DISPLAY "PEREVAL=" W-I.
           STOP RUN.
       END PROGRAM PB17FNSUB.
