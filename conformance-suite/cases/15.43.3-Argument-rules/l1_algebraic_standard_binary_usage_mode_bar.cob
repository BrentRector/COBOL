*>--- conformance-case
*> id: l1_algebraic_standard_binary_usage_mode_bar
*> rules: [AR-15.43.3-2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.43.3 r2 — the ADMIT arm of HIGHEST-ALGEBRAIC's standard-binary usage bar.
      *> "If standard-decimal arithmetic is in effect, argument-1 shall not be a data item whose data
      *> description entry specifies a standard binary floating-point usage."
      *> (cite.py --check 15.43.3 "If standard-decimal arithmetic is in effect, argument-1 shall not be a
      *> data item whose data description entry specifies a standard binary floating-point usage" -> OK,
      *> §15.43.3 rule 2.)
      *>
      *> ⛔ THE RULE IS CONDITIONAL ON THE ARITHMETIC MODE, AND THIS IS THE ARM THAT PROVES IT. NATIVE
      *> arithmetic is in effect here — there is no ARITHMETIC clause, and §11.9.5.2 GR4 says "If the
      *> ARITHMETIC clause is not specified in this source element or a containing source element, it is as
      *> if the ARITHMETIC clause were specified with the NATIVE phrase" — so r2's antecedent is FALSE and a
      *> standard binary floating-point usage is a perfectly legal argument-1. §3.166 names that family
      *> ("usages float-binary-32, float-binary-64, and float-binary-128"), i.e. exactly the USAGE
      *> FLOAT-BINARY-32 / -64 items below (FLOAT-BINARY-128 is Annex A.3 item 17 documented non-support:
      *> .NET has no IEEE 754 binary128 type, so it cannot appear in a corpus program).
      *>
      *> WHY THE ADMIT ARM NEEDS ITS OWN FIXTURE. The guard this row is about was once a BLANKET refusal of
      *> every float for all three algebraic functions, and its own diagnostic asserted that COMP-1/COMP-2
      *> are "barred by rule 2 under STANDARD-DECIMAL" — false twice over, since neither is a §3.166 usage
      *> and the mode was never consulted. A screen that rejected here would be indistinguishable, from the
      *> reject fixture alone, from one that reads the mode correctly. The pair is the measurement:
      *> conformance:cases/15.43.3-Argument-rules/l1-highest-algebraic-standard-binary-usage writes the SAME USAGE under
      *> ARITHMETIC IS STANDARD-DECIMAL and requires COBOLNET1516; this program writes it under native and
      *> requires a value. conformance:cases/15.43.3-Argument-rules/pb122_smallest_algebraic_float holds the third cell of the same
      *> table — a NON-§3.166 float (COMP-1 / COMP-2) under STANDARD-DECIMAL, which r2 also admits.
      *>
      *> The bounds are derived from §15.43.4 r2 ("The value returned is equal to the positive algebraic
      *> value of greatest finite magnitude that may be represented in argument-1") with §13.18.60.4 GR14
      *> and GR15 pinning the two usages: GR14 makes FLOAT-BINARY-32 "a floating-point data item in 32-bit
      *> basic binary interchange format (binary32) as specified in ISO/IEC 60559:2020, 3.4" and GR15 says
      *> the same of FLOAT-BINARY-64 and binary64 — these are the usages the STANDARD pins, unlike
      *> FLOAT-SHORT/-LONG, whose representation GR13 and GR21 leave to the implementor.
      *> They are stated at SEVEN significant digits, which
      *> is the CARRIER-identifying grain: no other carrier this compiler offers has a maximum in either
      *> interval. They deliberately do not pin the last digit — the exact decimal the fold emits is the
      *> carrier's round-trip form (3.4028235E+38 for binary32, whose true maximum is
      *> 3.40282346638528859811704...E+38), which is RV-15.43.4-2's subject and not this rule's.
      *>   N32  binary32's greatest finite value: 3.402823E+38 < x < 3.402824E+38.
      *>   N64  binary64's is 1.7976931348623157E+308. The upper bound is stated on a DIVIDED value
      *>        (x / 1.0E+300 < 1.7976932E+8) because writing 1.7976932E+308 as a literal names a number
      *>        binary64 cannot hold — a property of the probe, not of the answer.
      *>   L32  §15.58.4 r2 — "The value returned is equal to the lowest finite algebraic value that may be
      *>        represented in argument-1" — which for a signed binary format is the negation. The sibling is
      *>        here because all three functions share ONE mode-aware path, so a mode misread moves this
      *>        line too.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1HASD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FB32 USAGE FLOAT-BINARY-32.
       01 FB64 USAGE FLOAT-BINARY-64.
       01 SCALED USAGE COMP-2.
       PROCEDURE DIVISION.
       MAIN.
           IF FUNCTION HIGHEST-ALGEBRAIC(FB32) > 3.402823E+38
              AND FUNCTION HIGHEST-ALGEBRAIC(FB32) < 3.402824E+38
               DISPLAY "N32 OK" ELSE DISPLAY "N32 BAD" END-IF
           COMPUTE SCALED = FUNCTION HIGHEST-ALGEBRAIC(FB64) / 1.0E+300
           IF SCALED > 1.7976931E+8 AND SCALED < 1.7976932E+8
               DISPLAY "N64 OK" ELSE DISPLAY "N64 BAD" END-IF
           IF FUNCTION LOWEST-ALGEBRAIC(FB32) < -3.402823E+38
              AND FUNCTION LOWEST-ALGEBRAIC(FB32) > -3.402824E+38
               DISPLAY "L32 OK" ELSE DISPLAY "L32 BAD" END-IF
           STOP RUN.
       END PROGRAM L1HASD.
