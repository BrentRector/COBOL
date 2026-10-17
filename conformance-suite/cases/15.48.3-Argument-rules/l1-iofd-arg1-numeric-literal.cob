*>--- conformance-case
*> id: l1-iofd-arg1-numeric-literal
*> rules: [AR-15.48.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.48.3 r1 — "Argument-1 shall be a national or alphanumeric literal."
      *> A numeric literal is of neither admitted class, so this program is not
      *> legal COBOL and must be rejected. That much is the rule, and it is what
      *> this case witnesses.
      *>
      *> ⚠ WHAT IT DOES *NOT* WITNESS — stated here because an earlier draft of this
      *> header claimed the opposite twice, and a negative that misdescribes its own
      *> mechanism is how a screen gets deleted with the test still green.
      *>  · It is NOT true that "the literal-shape screen passes it". The shape
      *>    screen asks whether argument-1 bound as a string literal; a numeric
      *>    literal did not, so COBOLNET1517 fires on this program as well.
      *>  · It is NOT true that "only both together reject it". THREE overlapping
      *>    screens reject this one program: the position-0 class rule of r1, the
      *>    CROSS-argument rule of r3 ("Argument-2 shall be a data item of the same
      *>    type as argument-1" — a numeric argument-1 can agree with no admitted
      *>    argument-2 class at all), and the literal-shape screen. Two of the three
      *>    report the same diagnostic, which is the one recorded in the .err.
      *>  · Consequently this case CANNOT isolate r1's class half from r3's cross
      *>    rule: delete the position-0 class screen and the program is still
      *>    rejected. Nor can any source shape isolate them here — an argument-2 of
      *>    the same class as a NUMERIC argument-1 is itself numeric, and fails its
      *>    own class rule. Evidence that the position-0 screen specifically is
      *>    alive has to come from asserting its message text in a unit test, not
      *>    from a corpus rejection.
      *>
      *> The SHAPE half of r1 — an admitted class that is not a literal — is
      *> isolated cleanly by negative/l1-iofd-arg1-data-item (its class screen is
      *> clean and only the shape screen fires). The accepting side, both admitted
      *> classes written as literals, is 2014/l1_iofd_national_literal_arg1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NEGIOFDNUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D8 PIC X(8) VALUE "19950215".
       01 R  PIC 9(7).
       PROCEDURE DIVISION.
           COMPUTE R =
               FUNCTION INTEGER-OF-FORMATTED-DATE(19950215 D8)
           STOP RUN.
       END PROGRAM L1NEGIOFDNUM.
