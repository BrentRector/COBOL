      *> kb/Work PB1581 + PB1147 - the size error condition on the
      *> native binary64 intermediate. An expression with a floating-
      *> point operand evaluates in binary64 (CONFORMANCE.md
      *> DOC-A.1-123), so binary64's range is the intermediate's:
      *>   14.7.5 case 5 "if native arithmetic is in effect and the
      *>   implementor defines that the range of values allowed for the
      *>   intermediate data item is to be checked" (DOC-A.1-179: it is)
      *>   and no-phrase rule 3 names EC-SIZE-OVERFLOW / -UNDERFLOW
      *>   ("farther from zero or nearer to zero than is allowed").
      *>   14.7.5 case 2 "if the divisor in a divide operation or in a
      *>   DIVIDE statement is zero" - EC-SIZE-ZERO-DIVIDE (rule 2),
      *>   whatever the operands' usage.
      *> With the phrase the resultants keep their values (phrase rule
      *> 1); without it, under EC-SIZE checking, the USE declarative
      *> runs (14.6.13.1.3 item 5) and RESUME AT NEXT STATEMENT goes on.
      *> Expected values (binary64 range 4.9E-324 .. 1.8E+308):
      *>   1E300*1E300 = 1E600, 1E300**2 = 1E600: past it (OVERFLOW)
      *>   1E-300*1E-300 = 1E-600, 1E-300/1E300: nonzero, round to 0
      *>   1E308+1E308, -1E308-1E308, 1E300*1E10, SUM(1E308 1E308):
      *>   past it; 1E300*10 = 1E301 and 1E-300/1E10 = 1E-310 (a
      *>   subnormal) are inside it: NOT ON SIZE ERROR.
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1581BIN64RNG02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C   USAGE FLOAT-LONG VALUE 1.0E300.
       01 C2  USAGE FLOAT-LONG VALUE 1.0E308.
       01 T   USAGE FLOAT-LONG VALUE 1.0E-300.
       01 FL  USAGE FLOAT-LONG VALUE 5.
       01 FZ  USAGE FLOAT-LONG VALUE 0.
       01 F   USAGE FLOAT-LONG VALUE 7.
       01 A   PIC 9(3) VALUE 7.
       01 TAG PIC X(9).
       PROCEDURE DIVISION.
       DECLARATIVES.
       SZ SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
       SZ-P.
           DISPLAY "  DECLARATIVE " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE "C*C" TO TAG
           COMPUTE F = C * C
               ON SIZE ERROR PERFORM SE NOT ON SIZE ERROR PERFORM NS
           END-COMPUTE
           MOVE "C**2" TO TAG
           COMPUTE F = C ** 2
               ON SIZE ERROR PERFORM SE NOT ON SIZE ERROR PERFORM NS
           END-COMPUTE
           MOVE "T*T" TO TAG
           COMPUTE F = T * T
               ON SIZE ERROR PERFORM SE NOT ON SIZE ERROR PERFORM NS
           END-COMPUTE
           MOVE "T/C" TO TAG
           COMPUTE F = T / C
               ON SIZE ERROR PERFORM SE NOT ON SIZE ERROR PERFORM NS
           END-COMPUTE
           MOVE "ADD" TO TAG
           MOVE C2 TO F
           ADD C2 TO F
               ON SIZE ERROR PERFORM SE NOT ON SIZE ERROR PERFORM NS
           END-ADD
           MOVE "SUBTRACT" TO TAG
           MOVE -1.0E308 TO F
           SUBTRACT C2 FROM F
               ON SIZE ERROR PERFORM SE NOT ON SIZE ERROR PERFORM NS
           END-SUBTRACT
           MOVE "MULTIPLY" TO TAG
           MOVE 1.0E10 TO F
           MULTIPLY C BY F
               ON SIZE ERROR PERFORM SE NOT ON SIZE ERROR PERFORM NS
           END-MULTIPLY
           MOVE "SUM" TO TAG
           MOVE 7 TO F
           COMPUTE F = FUNCTION SUM (C2 C2)
               ON SIZE ERROR PERFORM SE NOT ON SIZE ERROR PERFORM NS
           END-COMPUTE
           MOVE "DIVIDE" TO TAG
           DIVIDE FZ INTO F
               ON SIZE ERROR PERFORM SE NOT ON SIZE ERROR PERFORM NS
           END-DIVIDE
           MOVE "FL/FZ" TO TAG
           COMPUTE F = FL / FZ
               ON SIZE ERROR PERFORM SE NOT ON SIZE ERROR PERFORM NS
           END-COMPUTE
           COMPUTE A = FL / FZ
               ON SIZE ERROR DISPLAY "A=FL/FZ   SIZE ERROR A=" A " "
                   FUNCTION EXCEPTION-STATUS
           END-COMPUTE
           MOVE "C*10" TO TAG
           COMPUTE F = C * 10
               ON SIZE ERROR PERFORM SE NOT ON SIZE ERROR PERFORM NS
           END-COMPUTE
           MOVE "T/1E10" TO TAG
           COMPUTE F = T / 1.0E10
               ON SIZE ERROR PERFORM SE NOT ON SIZE ERROR PERFORM NS
           END-COMPUTE
           MOVE 7 TO F
           DISPLAY "NO PHRASE C*C"
           COMPUTE F = C * C
           DISPLAY "F=" F
           DISPLAY "IF FL / FZ > 1"
           IF FL / FZ > 1 DISPLAY "  TRUE" ELSE DISPLAY "  FALSE"
           END-IF
           DISPLAY "END"
           STOP RUN.
       SE.
           DISPLAY TAG " SIZE ERROR F=" F " "
               FUNCTION EXCEPTION-STATUS.
       NS.
           DISPLAY TAG " NOT ON SIZE ERROR F=" F.
