*>--- conformance-case
*> id: pb22_integer_arg_narrowing
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB22 - the ONE narrowing from the wide intrinsic-argument carrier to the long the integer bodies take.
      *> The emitter wrote a bare (long)(...) over an Int128-typed expression, and RoslynBackend sets no
      *> checkOverflow, so the cast WRAPPED MODULO 2**64 - silently, and BEFORE the function's own range guard
      *> could see the value. 15.5.2's 1601..9999 / 1..366 check inside CobolDate.IntegerOfDay is correct and was
      *> simply unreachable.
      *>
      *> THE CASE: P * 100 + 62 = 18446744073711546662 = 2**64 + 1995046. The wrapped low 64 bits ARE 1995046
      *> (a valid 1995-046), so the function returned a perfectly plausible 143951 from an argument nineteen
      *> orders of magnitude away - with NO EC-ARGUMENT-FUNCTION even under enabled checking. A fabricated value,
      *> not an over-acceptance.
      *>
      *> 15.3 makes an incorrect argument value EC-ARGUMENT-FUNCTION, whose default result while checking is
      *> disabled is the implementor's - here 0. What this golden pins is that the answer is NOT the wrapped one.
      *> ⚠ ONE CAST FED SEVEN RENDERER ARMS OVER ELEVEN FUNCTIONS, which is why the fix is a landing rather than
      *> eleven repairs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB22NARROW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P PIC 9(18) VALUE 184467440737115466.
       01 R PIC 9(9).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION INTEGER-OF-DAY(P * 100 + 62)
           IF R = 143951
              DISPLAY "WRAPPED=YES"
           ELSE
              DISPLAY "WRAPPED=NO"
           END-IF
      *> The in-range value the wrap impersonated must still compute correctly.
           COMPUTE R = FUNCTION INTEGER-OF-DAY(1995046)
           DISPLAY "INRANGE=" R
      *> The sibling arm (INTEGER-OF-DATE) shares the same landing.
           COMPUTE R = FUNCTION INTEGER-OF-DATE(P * 100 + 62)
           IF R = 0
              DISPLAY "SIBLING-GUARDED=YES"
           ELSE
              DISPLAY "SIBLING-GUARDED=NO"
           END-IF
           STOP RUN.
