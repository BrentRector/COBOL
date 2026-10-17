*>--- conformance-case
*> id: pb247_substitute_flat_zero_length_arg3
*> rules: [AR-15.87.3-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB247 - ISO 15.87.3 r3's NEGATIVE SPACE on the table(ALL) (flat) bind path. r3 is
      *> "Neither argument-1 nor argument-2 shall be of zero length" - it names argument-1 and argument-2 and
      *> NOT argument-3, so a zero-length argument-3 (a deletion) is legal source and must keep compiling.
      *> The r3 walk over the pairs' argument-2 positions used to sit BELOW BindSubstitute's `if (flat)` early
      *> return, so on this path the rule was not enforced at all; moving it above the return risks the opposite
      *> error, and this fixture is what makes the over-rejection visible. 15.87.2 repeats the pair
      *> `[ANYCASE] [FIRST|LAST] argument-2 argument-3`, and 15.3 makes the ALL enumeration "as if each table
      *> element ... were specified", so the written call stands for
      *>     SUBSTITUTE("aXbXc" "X" "" "X" "Y")   - pair 1 = ("X" -> zero length), pair 2 = ("X" -> "Y")
      *> and 15.87.4 r3/r4's single left-to-right pass substitutes, at each position, the FIRST pair in listed
      *> order whose argument-2 matches: pair 1 wins at both occurrences of "X", replacing each with a
      *> zero-length string, so the returned value is "abc" - and pair 2 is never reached, which is exactly what
      *> shows the zero-length operand landed on the ARGUMENT-3 of pair 1 and not on an argument-2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB247SUBFLAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SB2-T.
          05 SB2 PIC X OCCURS 2.
       01 A20   PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "X" TO SB2(1)
           MOVE "Y" TO SB2(2)
           MOVE FUNCTION SUBSTITUTE("aXbXc" "X" "" SB2(ALL)) TO A20
           DISPLAY "FLT=[" A20 "]"
           STOP RUN.
