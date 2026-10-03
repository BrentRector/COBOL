      *> kb/Work PB1537 - ISO 12.3.7.3 SR11 bars ONE kind of figurative constant from literal-4 and literal-9:
      *> "Literal-1, literal-2, literal-3, literal-4, literal-5, literal-6, and literal-9 shall specify neither a
      *> symbolic-character figurative constant nor a zero-length literal" (cite.py --check 12.3.7.3 "Literal-1,
      *> literal-2, literal-3, literal-4, literal-5, literal-6, and literal-9 shall specify neither a symbolic-character
      *> figurative constant nor a zero-length literal"), and 8.3.3.6.3 SR1 admits every other figurative constant
      *> wherever "literal" appears in a format (cite.py --check 8.3.3.6.3 "A figurative constant may be used whenever
      *> 'literal' appears in a format"). SPACE and QUOTE are such constants, so `ORDER TABLE BADT IS SPACE` and
      *> `LOCALE XX IS QUOTE` are LEGAL source. The screen refused every figurative constant, quoting SR11.
      *> What the clause then names is the implementor's (12.3.7.4 GR17 for the table, GR5 for the locale: one
      *> character, the space / the quotation mark, which no table or locale of this processor answers to), so the
      *> outcome is the RUN-TIME one: 15.85.4 rule 2 sets EC-ORDER-NOT-SUPPORTED at the reference, and 14.9.39.4 GR24
      *> sets EC-LOCALE-MISSING at the SET - exactly what pb101_ec_order_not_supported and pb64t1_ec_locale_missing pin
      *> for a literal that names nothing. Each declarative resumes at the next statement, so the compiled program runs.
      *>   HANDLED #1 - STANDARD-COMPARE over the SPACE-named table raises; R keeps its VALUE (the MOVE never completed).
      *>   HANDLED #2 - SET LOCALE to the QUOTE-named locale raises.
       >>TURN EC-ORDER-NOT-SUPPORTED CHECKING ON
       >>TURN EC-LOCALE-MISSING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1537FIG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ORDER TABLE BADT IS SPACE
           LOCALE XX IS QUOTE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X VALUE "?".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ORDER-NOT-SUPPORTED
               EC-LOCALE-MISSING.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE FUNCTION STANDARD-COMPARE("a" "b" BADT) TO R.
           DISPLAY "R=[" R "]".
           SET LOCALE LC_COLLATE TO XX
           DISPLAY "AFTER".
           STOP RUN.
