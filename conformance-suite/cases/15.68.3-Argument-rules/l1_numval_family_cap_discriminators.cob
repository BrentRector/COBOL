*>--- conformance-case
*> id: l1_numval_family_cap_discriminators
*> rules: [AR-15.68.3-6, AR-15.69.3-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.68.3 rule 6 (NUMVAL-C) and §15.69.3 rule 2 (NUMVAL-F) — "If native arithmetic is in
      *> effect, the total number of digits in argument-1 [NUMVAL-F: in the significand] shall not
      *> exceed 31." Native arithmetic is in effect here: there is no OPTIONS paragraph, and §11.9.5.2
      *> GR4 says "If the ARITHMETIC clause is not specified in this source element or a containing
      *> source element, it is as if the ARITHMETIC clause were specified with the NATIVE phrase."
      *>
      *> ⛔ WHY THIS EXISTS BESIDE pb33_numval_family_digit_cap. pb33 pins the REJECT side for all three
      *> family members, and it carries a 31-digit control — but that control is FUNCTION NUMVAL(S31),
      *> on NUMVAL alone. For NUMVAL-C and NUMVAL-F pb33 is ONE-SIDED: it holds no NUMVAL-C and no
      *> NUMVAL-F reference that returns a value, so an implementation answering the §15.3 implementor
      *> default 0 for EVERY NUMVAL-C and NUMVAL-F argument would satisfy every line pb33 asserts. A cap
      *> is a BOUNDARY, and a boundary is measured only by a matched pair astride it, on the function
      *> whose rule is being measured — never by its sibling's control.
      *>
      *> NVC-31 / NVC-34 — the NUMVAL-C pair. What rule 6 bounds is the DIGIT count, and the currency
      *>     string is not a digit: §15.68.4 r2, "The currency string, if any, and any grouping
      *>     separators preceding the decimal separator are ignored." So "$" + 25 + 6 digits is a
      *>     31-digit argument-1, at the bound and therefore REQUIRED to be accepted, and its value is
      *>     what comes back; "$" + 25 + 9 digits is 34, over the native cap, and §15.3's closing
      *>     sentence supplies the implementor default ("If the EC-ARGUMENT-FUNCTION exception condition
      *>     is set to exist and checking for EC-ARGUMENT-FUNCTION is not enabled, the implementor
      *>     defines the result of the function reference" — 0, documented in CONFORMANCE.md).
      *>
      *> NVF-SIG31 / NVF-SIG34 — the NUMVAL-F pair, and NVF-SIG31 is the only case in the corpus that
      *>     discriminates the WORD rule 2 uses. Rule 2 bounds the SIGNIFICAND, not the argument:
      *>     "123456789012345678901234.5678901E+1" carries 31 significand digits — exactly at the bound,
      *>     so the rule requires it accepted — inside a string holding 32 digits. An implementation
      *>     that counted the whole argument would reject it and print the §15.3 default instead of the
      *>     value. pb33's F34 cannot separate the two readings: 34 significand digits inside a 36-digit
      *>     string is over BOTH caps and yields the same observable either way.
      *>     The value is the same number NUMVAL reaches from a 31-digit argument carrying no exponent
      *>     (pb33's NUMVAL-31 line), so the two goldens print the same 31 characters by two routes.
      *>
      *> Checking is DISABLED here (no >>TURN), which is what makes 0 the observable for the reject half
      *> rather than a fatal termination — the same convention pb33 runs under.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NVCAPDISC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> 31 digits behind a currency string - AT the native cap, legal.
       01 C31 PIC X(44) VALUE "$1234567890123456789012345.678901".
      *> 34 digits - over the native cap.
       01 C34 PIC X(44) VALUE "$1234567890123456789012345.678901234".
      *> 31 SIGNIFICAND digits inside a 32-digit string - legal, and the discriminator.
       01 F31 PIC X(44) VALUE "123456789012345678901234.5678901E+1".
      *> 34 significand digits - over the cap on the significand alone.
       01 F34 PIC X(44) VALUE "1234567890123456789012345678901234E+2".
       01 R   PIC S9(25)V9(6).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION NUMVAL-C(C31)
           DISPLAY "NVC-31=" R
           COMPUTE R = FUNCTION NUMVAL-C(C34)
           DISPLAY "NVC-34=" R
           MOVE FUNCTION NUMVAL-F(F31) TO R
           DISPLAY "NVF-SIG31=" R
           MOVE FUNCTION NUMVAL-F(F34) TO R
           DISPLAY "NVF-SIG34=" R
           STOP RUN.
       END PROGRAM L1NVCAPDISC.
