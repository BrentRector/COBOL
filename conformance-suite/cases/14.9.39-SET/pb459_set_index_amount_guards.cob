*>--- conformance-case
*> id: pb459_set_index_amount_guards
*> rules: [GR-14.9.39.4-1, GR-14.9.39.4-2, GR-14.9.39.4-3, GR-14.9.39.4-4, DOC-A.1-128]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB459 - the SET-family AMOUNT guards with exception CHECKING OFF (the default): the standard
      *> states the LENIENT outcome outright, so the answer does not depend on the EC model at all.
      *>
      *> ISO 14.9.39.4 GR2 a) 1. a - "If the value of arithmetic-expression-1 does not result in an integer, and
      *>   - the EC-BOUND-SUBSCRIPT exception condition is set to exist, and
      *>   - the execution of the SET statement is unsuccessful and
      *>   - the content of the receiving operand is unchanged."
      *> ISO 14.9.39.4 GR3 - the same three consequents for Format 2's arithmetic-expression-2.
      *> ISO 14.9.39.4 GR4 a) - if the incremented/decremented occurrence number "is outside the limit specified
      *>   in General rule 2 of 13.18.38, OCCURS clause", EC-RANGE-INDEX, unsuccessful, receiving operand
      *>   unchanged.
      *> ISO 13.18.38.4 GR2 - that limit is the IMPLEMENTOR's index range. COBOL.NET's index cell is a signed
      *>   64-bit occurrence number (docs/CONFORMANCE.md 7, DOC-A.1-128), so the range is
      *>   -9223372036854775808 .. +9223372036854775807.
      *>
      *> With checking OFF the condition is not set to exist, but the other two consequents are not conditional
      *> on checking - they are what the SET statement DOES - so every line below is derived from the rule text
      *> alone. Before PB459 each of these emitted a bare (long) narrowing of the amount: the fraction was
      *> truncated away before any test could see it and the 64-bit boundary was a silent wrap.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>
      *> UPBY-FRAC   SET IX TO 2, then UP BY 1.5. GR3: 1.5 is not an integer, so the SET is unsuccessful and IX
      *>             is unchanged => +0000000000000000002. (The pre-PB459 answer was 3 - the truncated 1.)
      *> TO-FRAC     SET IX TO 1.5 on that same IX. GR2 a) 1. a: unsuccessful, IX unchanged => still 2.
      *> UPBY-2P0    SET IX TO 2, then UP BY 2.0. The test is on the VALUE, not on the SCALE: 2.0 IS an integer,
      *>             so GR4 b) applies and IX refers to occurrence 4 => +0000000000000000004.
      *> OVERFLOW    SET IX TO 1, UP BY 9223372036854775800 (result 9223372036854775801 - inside the range, so
      *>             GR4 b), taken), then UP BY 100. That result, 9223372036854775901, is outside the
      *>             implementor range, so GR4 a): unsuccessful, IX unchanged => +9223372036854775801.
      *>             (The pre-PB459 answer wrapped to a NEGATIVE occurrence number and the program carried on.)
      *> NEGOCC      SET IX TO -3. GR2 a) 1. c sets the index "to a value causing it to refer to the table
      *>             element that would correspond in occurrence number to that expression, EVEN IF THAT
      *>             OCCURRENCE IS NOT A VALID OCCURRENCE WITHIN THIS TABLE", and -3 is inside the implementor
      *>             range, so this is LEGAL and must not raise => -0000000000000000003. This line is the
      *>             control that keeps the new guard from becoming a table-bounds test.
      *>
      *>   UPBY-FRAC=+0000000000000000002
      *>   TO-FRAC=+0000000000000000002
      *>   UPBY-2P0=+0000000000000000004
      *>   OVERFLOW=+9223372036854775801
      *>   NEGOCC=-0000000000000000003
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB459SETIXG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC 9(2) OCCURS 5 TIMES INDEXED BY IX.
       01 FRAC PIC 9V9 VALUE 1.5.
       01 N PIC S9(19) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 2
           SET IX UP BY FRAC
           SET N TO IX
           DISPLAY "UPBY-FRAC=" N
           SET IX TO FRAC
           SET N TO IX
           DISPLAY "TO-FRAC=" N
           SET IX TO 2
           SET IX UP BY 2.0
           SET N TO IX
           DISPLAY "UPBY-2P0=" N
           SET IX TO 1
           SET IX UP BY 9223372036854775800
           SET IX UP BY 100
           SET N TO IX
           DISPLAY "OVERFLOW=" N
           SET IX TO -3
           SET N TO IX
           DISPLAY "NEGOCC=" N
           STOP RUN.
