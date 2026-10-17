*>--- conformance-case
*> id: pb40-numeric-function-in-integer-position
*> rules: [SR-8.4.3.2.3-11]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.2.3 SR11: "A numeric function shall not be specified where an
      *> integer operand is required, EVEN THOUGH A PARTICULAR REFERENCE OF THE
      *> NUMERIC FUNCTION MIGHT YIELD AN INTEGER VALUE." 15.15.3 r1 makes CHAR's
      *> argument an integer, and 15.7.1's table makes FUNCTION ABS over a SCALED
      *> item a NUMERIC function, not an integer one.
      *>
      *> IT COMPILED CLEAN (fix-queue PB40). The 15.3 screen resolves through
      *> 8.5.2.1 Table 2's CLASS column, and 15.2 items 5 and 6 put BOTH integer
      *> and numeric functions in class numeric — so the 'i' code could not tell
      *> them apart and admitted both. 15.3 type 6 is INTEGER, and integer is not
      *> a class; that is why a class screen was structurally unable to enforce it.
      *>
      *> ⚠ SR11 decides on the function's TYPE, not on the value this reference
      *> would produce — which is what makes the rejection safe. The sibling
      *> shapes that must KEEP compiling (an integer literal, an integer item, an
      *> always-integral arithmetic expression, an INTEGER function) are asserted
      *> by pb40_integer_argument_positions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB40NEGFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-F PIC 9V9 VALUE 6.5.
       01 W-R PIC X.
       PROCEDURE DIVISION.
           MOVE FUNCTION CHAR(FUNCTION ABS(W-F)) TO W-R
           STOP RUN.
