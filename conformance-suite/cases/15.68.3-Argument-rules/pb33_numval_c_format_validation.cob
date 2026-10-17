*>--- conformance-case
*> id: pb33_numval_c_format_validation
*> rules: [AR-15.68.3-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.68.3 r4a: "Argument-1 shall have one of the following two
      *> formats" — the sign-before-currency form and the trailing sign/CR/DB
      *> form. 15.3: "If the evaluation of an argument results in an incorrect
      *> value for that argument … the EC-ARGUMENT-FUNCTION exception condition
      *> is set to exist", and when checking is not enabled the same clause
      *> leaves the result implementor-defined.
      *>
      *> NUMVAL-C ENFORCED NEITHER (fix-queue PB33). It stripped the currency and
      *> the grouping separators and delegated to NUMVAL, checking nothing —
      *> while its validating twin TEST-NUMVAL-C (15.94, whose entire job is this
      *> rule) implemented both formats exactly. The validating twin was fixed
      *> and the value-producing one was not.
      *>
      *> ⛔ AND IT WAS NOT MERELY A MISSING DEFAULT: `NUMVAL-C("12$34")` returned
      *> 1234 — a WRONG ANSWER — where TEST-NUMVAL-C correctly reports an error at
      *> position 3. Interior currency is not either format.
      *>
      *> The fix routes NUMVAL-C through TestNumvalC, so the rule has ONE
      *> implementation and the two functions cannot disagree. The digit cap
      *> already worked this way (PB33's landed half); now the whole rule does.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB33NUMVALC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9)V99.
       01 T PIC S9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> ── CONFORMING (both 15.68.3 r4a formats) — the value is unchanged. ──
           COMPUTE R = FUNCTION NUMVAL-C("$1,234.5").
           DISPLAY "01-A-FORM=" R.
           COMPUTE R = FUNCTION NUMVAL-C("-$890.05").
           DISPLAY "02-A-SIGNED=" R.
           COMPUTE R = FUNCTION NUMVAL-C("$1,234.5CR").
           DISPLAY "03-B-CR=" R.
           COMPUTE R = FUNCTION NUMVAL-C("$12.34-").
           DISPLAY "04-B-TRAIL=" R.
      *> r4a's grouping is `digit [, digit]…` with NO 3-digit constraint.
           COMPUTE R = FUNCTION NUMVAL-C("$1,23,4.5").
           DISPLAY "05-ODD-GROUPS=" R.
      *> ── NONCONFORMING — 15.3's implementor-defined result with checking OFF.
      *> Each is paired with TEST-NUMVAL-C, which reports the offending position:
      *> the two functions now share ONE validator and cannot disagree. ──
           COMPUTE R = FUNCTION NUMVAL-C("$1,2X4.5").
           COMPUTE T = FUNCTION TEST-NUMVAL-C("$1,2X4.5").
           DISPLAY "06-BADCHAR=" R " POS=" T.
      *> The wrong answer this fix removes: interior currency used to give 1234.
           COMPUTE R = FUNCTION NUMVAL-C("12$34").
           COMPUTE T = FUNCTION TEST-NUMVAL-C("12$34").
           DISPLAY "07-INTERIOR-CUR=" R " POS=" T.
           COMPUTE R = FUNCTION NUMVAL-C("1.2.3").
           COMPUTE T = FUNCTION TEST-NUMVAL-C("1.2.3").
           DISPLAY "08-TWO-DECIMALS=" R " POS=" T.
           STOP RUN.
