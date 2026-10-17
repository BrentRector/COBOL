*>--- conformance-case
*> id: pb42_power_and_decimal_subscript
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.1.1 + 8.3.2.4.2 + 8.4.2.3.2 - EXPONENTIATION and a DECIMAL
      *> LITERAL are ordinary arithmetic, so both are legal in a subscript and in a
      *> reference-modifier position (fix-queue PB42, found by the PB17 sweep).
      *> 8.8.1.1: "An arithmetic expression may be an identifier referencing a
      *> numeric data item, A NUMERIC LITERAL, the figurative constant ZERO ..., such
      *> identifiers, figurative constants, and literals SEPARATED BY ARITHMETIC
      *> OPERATORS"; 8.3.2.4.2 lists ** as "Arithmetic operator - exponentiation";
      *> 8.4.2.3.2 makes a subscript arithmetic-expression-1 and 8.4.3.3.3 SR4 does
      *> the same for a ref-mod position.
      *>
      *> BOTH COMPILED CLEAN AND THREW NotImplementedCobolFeatureException AT RUN
      *> TIME - the same wrong-stage family as PB17 itself, one token further along.
      *> ** had no case arm in the segment renderer and neither did a decimal
      *> literal; ** merely LOOKED handled, because a scaled operand diverted the
      *> whole segment to the materializer (the PB41 compound rule) before the
      *> renderer ever reached the ** token. A probe using scaled operands reports
      *> this as working.
      *>
      *> CHECKING IS ON THROUGHOUT, which is the point of cases 1-4: 2.0 IS an
      *> integer VALUE, so 8.4.2.3.4 GR1b does NOT fire and there is no CAUGHT line.
      *> Only case 5's genuinely fractional 2.5 raises.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       >>TURN EC-BOUND-REF-MOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB42POWDEC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-G.
          05 W-E PIC 9(2) OCCURS 9 TIMES.
       01 W-A PIC X(9) VALUE "ABCDEFGHI".
       01 W-I PIC 9 VALUE 2.
       01 W-R PIC 9(2).
       01 W-U PIC X(2).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H-SUB SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H-SUB-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       H-RM SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
       H-RM-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 22 TO W-E (2).
           MOVE 44 TO W-E (4).
      *> 1 - ** in a SUBSCRIPT, with UNSCALED operands (the shape that died).
      *> 2 ** 2 = 4 -> W-E(4) = 44.
           MOVE W-E (W-I ** 2) TO W-R.
           DISPLAY "SUBPOW=" W-R.
      *> 2 - ** in a REF-MOD leftmost-position. Position 4, length 2 -> "DE".
           MOVE W-A (W-I ** 2:2) TO W-U.
           DISPLAY "RMPOW=[" W-U "]".
      *> 3 - a DECIMAL LITERAL subscript. 2.0 is an INTEGER value, so this names
      *> occurrence 2 and raises nothing.
           MOVE W-E (2.0) TO W-R.
           DISPLAY "SUBDEC=" W-R.
      *> 4 - a decimal literal as a REF-MOD leftmost-position -> "BC".
           MOVE W-A (2.0:2) TO W-U.
           DISPLAY "RMDEC=[" W-U "]".
      *> 5 - the same literal form, genuinely FRACTIONAL. GR1b fires here and only
      *> here, which is what separates "not yet rendered" from "not legal".
      *> W-R is pre-set to 99 so the line DISCRIMINATES: the declarative's RESUME AT
      *> NEXT STATEMENT abandons the MOVE, leaving 99. Without the pre-set both the
      *> abandoned MOVE and a truncating one would print 22 and the golden would
      *> pin neither.
           MOVE 99 TO W-R.
           MOVE W-E (2.5) TO W-R.
           DISPLAY "SUBFRAC=" W-R.
           STOP RUN.
