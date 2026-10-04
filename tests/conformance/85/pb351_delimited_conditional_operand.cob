      *> kb/Work PB351. ISO 14.5.1 at --std 85, the edition the rule entered with the explicit scope
      *> terminators: "An imperative statement specifies an unconditional action to be taken by the runtime
      *> element or is a conditional statement that is delimited by its explicit scope terminator", and "Any
      *> statement with a conditional phrase that is not terminated by its explicit scope terminator is a
      *> conditional statement." So a conditional statement IS admissible as an imperative-statement operand
      *> once it is closed by its END- terminator - this golden writes one in each kind of phrase operand - and
      *> IF's statement-1 is the one operand that may END in an undelimited conditional statement (14.9.19.3
      *> SR1: "either one or more imperative statements or a conditional statement optionally preceded by one
      *> or more imperative statements"). The reject side is negative/pb351-*.
      *>
      *> DERIVATION - every expected line follows from the rule text, nothing from the compiler.
      *>  1. ADD 1 TO X (PIC 9, value 9): the sum 10 does not fit, a size error (14.7.5) - the ON SIZE ERROR
      *>     operand runs, its delimited IF takes N = 1: SIZE-1. X keeps 9 (14.7.5 GR3).
      *>  2. SEARCH K from IX = 1 over 1,2,3: WHEN K (IX) = 2 is true at occurrence 2, so the delimited IF
      *>     compares IX (occurrence number 2) with 2: FOUND-AT-2.
      *>  3. EVALUATE N, WHEN 1: the delimited IF takes ELSE (X = 9): X-NONZERO, and END-IF closed the IF,
      *>     so the next imperative of the same WHEN operand runs too: AFTER-IF.
      *>  4. PERFORM 2 TIMES ADD 5 TO X ... END-ADD: 9 + 5 = 14 does not fit, twice: OVF, OVF.
      *>  5. IF N = 1 with statement-1 = an imperative DISPLAY then the undelimited conditional ADD: the DISPLAY
      *>     runs (IMPERATIVE-FIRST), then ADD 1 TO X is a size error again (X is 9): COND-LAST.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB351P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 9.
       01 N PIC 9 VALUE 1.
       01 T.
          05 K PIC 9 OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "123" TO T.
           ADD 1 TO X
               ON SIZE ERROR
                   IF N = 1
                       DISPLAY "SIZE-1"
                   ELSE
                       DISPLAY "SIZE-OTHER"
                   END-IF
               NOT ON SIZE ERROR
                   DISPLAY "NO-SIZE"
           END-ADD.
           SET IX TO 1.
           SEARCH K
               AT END DISPLAY "NOT-FOUND"
               WHEN K (IX) = 2
                   IF IX = 2 DISPLAY "FOUND-AT-2" END-IF
           END-SEARCH.
           EVALUATE N
               WHEN 1
                   IF X = 0
                       DISPLAY "X-ZERO"
                   ELSE
                       DISPLAY "X-NONZERO"
                   END-IF
                   DISPLAY "AFTER-IF"
               WHEN OTHER
                   DISPLAY "OTHER"
           END-EVALUATE.
           PERFORM 2 TIMES
               ADD 5 TO X ON SIZE ERROR DISPLAY "OVF" END-ADD
           END-PERFORM.
           IF N = 1
               DISPLAY "IMPERATIVE-FIRST"
               ADD 1 TO X ON SIZE ERROR DISPLAY "COND-LAST".
           STOP RUN.
