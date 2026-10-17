*>--- conformance-case
*> id: pb459_set_capacity_amount_guards
*> rules: [GR-14.9.39.4-29]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB459 - SET Format 14's amount guard (OCCURS DYNAMIC, a COBOL-2014 feature).
      *>
      *> ISO 14.9.39.4 GR29 - "If arithmetic-expression-4 does not evaluate to a nonnegative integer, the
      *> EC-BOUND-SUBSCRIPT exception condition is set to exist and the execution of the SET statement is
      *> unsuccessful." The test is on the AMOUNT, in all three of GR30's forms (TO / UP BY / DOWN BY compute
      *> the new capacity FROM it afterwards), and GR30's own minimum-capacity clamp cannot stand in for it:
      *> the clamp is the rule for a legal new capacity below the OCCURS minimum, and GR29 rejects the operand
      *> BEFORE any new capacity is computed. EC-BOUND-SUBSCRIPT is Fatal in Table 13, so the declarative runs
      *> and RESUME AT NEXT STATEMENT (14.9.33) keeps the run unit alive past it.
      *>
      *> Before PB459 the amount reached the runtime as a bare (long) narrowing with no test at all, and the
      *> only screen downstream was the minimum clamp - so a NEGATIVE amount DESTROYED every live occurrence
      *> (clamped to the minimum, 0) and a FRACTIONAL one silently truncated and shrank the table.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>
      *> CAP1  SET CAP TO 5: 5 is a nonnegative integer, GR30 a) makes it the new capacity => 0000000005.
      *> SUB=  SET CAP TO -3 violates GR29, so EC-BOUND-SUBSCRIPT is set to exist and the declarative reports
      *>       it (FUNCTION EXCEPTION-STATUS returns the name in a 31-character field, space-padded).
      *> CAP2  GR29's other consequent: "the execution of the SET statement is unsuccessful" - no new capacity
      *>       is computed, so the capacity is still 5 => 0000000005. (The pre-PB459 answer was 0.)
      *> SUB=  SET CAP TO 3.5 violates GR29 the same way - 3.5 is not an integer - and reports again.
      *> CAP3  unsuccessful again => 0000000005. (The pre-PB459 answer was 3.)
      *> CAP4  SET CAP DOWN BY 2: the AMOUNT 2 is a nonnegative integer, so GR29 is satisfied; GR30 c)
      *>       subtracts it from the current capacity => 0000000003. This line is the control proving the new
      *>       guard tests the amount and not the direction.
      *>
      *>   CAP1=0000000005
      *>   SUB=EC-BOUND-SUBSCRIPT
      *>   CAP2=0000000005
      *>   SUB=EC-BOUND-SUBSCRIPT
      *>   CAP3=0000000005
      *>   CAP4=0000000003
      *> (each status line padded to 31 characters)
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB459SETCAPG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 D PIC 9(2) OCCURS DYNAMIC CAPACITY IN CAP.
       01 NEG PIC S9(4) VALUE -3.
       01 FRAC PIC 9V9 VALUE 3.5.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HSUB SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       HSUB-P.
           DISPLAY "SUB=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SET CAP TO 5.
           DISPLAY "CAP1=" CAP.
           SET CAP TO NEG.
           DISPLAY "CAP2=" CAP.
           SET CAP TO FRAC.
           DISPLAY "CAP3=" CAP.
           SET CAP DOWN BY 2.
           DISPLAY "CAP4=" CAP.
           STOP RUN.
