      *>--- conformance-case
      *> id: l1_tan_format
      *> rules: [AR-15.89.3-1, FMT-15.89.2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.89.2 general format — FUNCTION TAN ( argument-1 )
      *> "<u>FUNCTION</u> <u>TAN</u> ( argument-1 )": both words
      *> underlined (required), exactly one argument inside required
      *> parentheses, nothing bracketed - so unlike 15.75.2 RANDOM
      *> there is NO no-argument form, and unlike the repeating
      *> families there is no "{ argument } ..." to admit a table(ALL)
      *> subscript. No conformance golden or negative fixture named
      *> TAN before this one; the only corpus occurrences were
      *> nist/programs/IF139A.cob, a regression net that cannot close
      *> a row.
      *>
      *> THE FORMAT IS EXERCISED OVER EVERY SHAPE argument-1 ADMITS.
      *> ISO 15.3 type 10: "An arithmetic expression or a numeric data
      *> item shall be specified" - LIT is a numeric literal, ITEM a
      *> numeric data item, EXPR an arithmetic expression and NEST a
      *> nested function-identifier. A format silently narrowed to a
      *> bare identifier passes LIT and ITEM and fails EXPR and NEST,
      *> which is why all four are written.
      *> The complement - a SECOND argument, which the format has no
      *> place for - is the negative fixture l1-tan-two-arguments.
      *>
      *> VALUES, from ISO 15.89.4 r1 ("The returned value is the
      *> approximation of the tangent of argument-1"): tan(0) = 0
      *> exactly, and at the eighth-turn tan is +1 / -1. The
      *> eighth-turn argument is written as the 17-digit decimal value
      *> of pi/4 and the receiver ROUNDS at 6 fraction digits (ISO
      *> 14.7.4), so the expected digits are the exact mathematical
      *> values and any approximation accurate to better than 5e-7
      *> produces them - the accuracy itself is implementor-defined
      *> under native arithmetic (15.4.1), and native arithmetic is in
      *> effect because no ARITHMETIC clause is written (8.8.1.3).
      *> The non-zero pins are what stop a stub returning 0 for every
      *> argument from passing the four zero lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TANFMT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-B PIC S9(3)V9(9) SIGN LEADING SEPARATE.
       01 T-6 PIC S9V9(6) SIGN LEADING SEPARATE.
       01 W-Z PIC S9V9(9) VALUE 0.
       01 W-Q PIC S9V9(16) VALUE 0.7853981633974483.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE T-B = FUNCTION TAN(0)
           IF T-B = 0
               DISPLAY "LIT=ZERO"
           ELSE
               DISPLAY "LIT=NONZERO"
           END-IF
           COMPUTE T-B = FUNCTION TAN(W-Z)
           IF T-B = 0
               DISPLAY "ITEM=ZERO"
           ELSE
               DISPLAY "ITEM=NONZERO"
           END-IF
           COMPUTE T-B = FUNCTION TAN(W-Z + 0)
           IF T-B = 0
               DISPLAY "EXPR=ZERO"
           ELSE
               DISPLAY "EXPR=NONZERO"
           END-IF
           COMPUTE T-B = FUNCTION TAN(FUNCTION TAN(0))
           IF T-B = 0
               DISPLAY "NEST=ZERO"
           ELSE
               DISPLAY "NEST=NONZERO"
           END-IF
           COMPUTE T-6 ROUNDED = FUNCTION TAN(W-Q)
           DISPLAY "EIGHTH=" T-6
           COMPUTE T-6 ROUNDED = FUNCTION TAN(0 - W-Q)
           DISPLAY "NEIGHTH=" T-6
           STOP RUN.
