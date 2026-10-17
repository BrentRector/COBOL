      *>--- conformance-case
      *> id: l1-pi-argument-list
      *> rules: [FMT-15.73.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 15.73.2 general format, the COMPLEMENT of the shape:
      *> "<u>FUNCTION</u> <u>PI</u>" is TWO required words and NOTHING
      *> else - no parentheses, no argument, and nothing bracketed. It
      *> is the shape a "the parenthesised group is optional" grammar
      *> makes hardest to enforce, because the group is genuinely
      *> optional for 15.75.2 RANDOM, so what forbids an argument to
      *> PI is 15.3's "The definition of a function specifies the
      *> number of arguments required, which may be zero" - and PI's
      *> definition specifies zero.
      *>
      *> The positive half is l1_pi_format_native, which writes the
      *> bare two-word shape in four different statement positions. A
      *> format test that only writes the legal shape cannot tell "the
      *> format is enforced" from "an argument list is tolerated and
      *> discarded", which is why this file exists beside it.
      *>
      *> 85 IS DELIBERATELY ABSENT from the reject-at header. PI is a
      *> COBOL-2002 intrinsic, so at 85 this program is rejected by
      *> the edition gate before the argument count is ever reached -
      *> a DIFFERENT obligation, and collapsing the two would let a
      *> regression in either hide behind the other (the same
      *> separation standard-binary-pi-2002 makes for its own clause).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PIARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(6)V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION PI(1).
           STOP RUN.
