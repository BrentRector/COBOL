      *> reject-at: 2023
      *> ISO §7.3.6.2 SR2 (Annex A.1 item 29) — the DOCUMENTED magnitude
      *>   restriction on compile-time intermediate results
      *> Rule: "The implementor shall define and document any rules
      *>   restricting the precision and/or magnitude and/or range of
      *>   permissible values for the intermediate results needed to
      *>   evaluate the arithmetic expression."
      *>   cite.py --check 7.3.6.2 "The implementor shall define and
      *>     document any rules restricting the precision and/or
      *>     magnitude and/or range ..." -> OK  §7.3.6.2 2)
      *> The documented restriction (docs/CONFORMANCE.md DOC-A.1-29):
      *>   an intermediate result whose magnitude is not below about
      *>   7.9E28 is a compile-time error, COBOLNET1547 in a constant
      *>   entry - never a wrapped or truncated value; the row's own
      *>   example is 9999999999999999999999999999 * 10.
      *> The product below is 99999999999999999999999999990 (about
      *>   1.0E29), outside the documented range, so the constant entry
      *>   shall be refused. The entry is otherwise well formed (two
      *>   fixed-point literals, no exponentiation, no division by zero
      *>   - §7.3.6.2 SR1 a) b) c)), so the only reason to reject is
      *>   the documented range.
      *> reject-at 2023 ONLY: Annex E.2 6) makes the mode implementor
      *>   defined from 2023 ("The previous COBOL Standard required the
      *>   use of an arithmetic mode that is no longer supported." ->
      *>   OK  §E.2 6)); the earlier prescribed mode is not derivable
      *>   from specs/, so no 2002/2014 expectation is claimed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G2M2N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K-OV CONSTANT AS 9999999999999999999999999999 * 10.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "COMPILED"
           STOP RUN.
