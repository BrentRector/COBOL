*>--- conformance-case
*> id: l1_fdt_general_format
*> rules: [FMT-15.40.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.2 general format - FUNCTION FORMATTED-DATETIME
      *> ( argument-1 argument-2 argument-3 [ argument-4 ] ): the two
      *> required words, the required argument TRIO, and the ONE optional
      *> trailing argument. Both words are UNDERLINED in the printed format,
      *> so both are required; §8.4.3.2.3 SR2 is the sole licence to omit the
      *> first - "If intrinsic-function-name-1 or the ALL phrase is specified
      *> in the REPOSITORY paragraph ... the word FUNCTION may be omitted
      *> from the function-identifier; otherwise the word FUNCTION is
      *> required" - and the REPOSITORY paragraph below supplies it.
      *>
      *> THE ARITY IS THE HALF A POSITIVE GOLDEN CANNOT PROVE ALONE: a
      *> catalog row reading 2..4 or 3..5 would pass every line here. The
      *> reject side is the negative corpus - l1-fdt-arity-two-arguments and
      *> l1-fdt-arity-five-arguments (COBOLNET1504); the un-licensed omission
      *> of FUNCTION is the generic §8.4.3.2.3 SR2 arm, pinned by
      *> negative/kof-undeclared-intrinsic-args (COBOLNET1543).
      *>
      *> Every expected value is derived from the rule text, not observed:
      *>   §15.5.2 - integer date 1 is 1601-01-01, so 143951 is 1995-02-15
      *>             (143950 days after 1601-01-01).
      *>   §15.5.5 - a standard numeric time form value is seconds past
      *>             midnight, so 45296 = 12*3600 + 34*60 + 56 is 12:34:56.
      *>   §15.3.3.7 - a basic combined format is a basic date format, an
      *>             uppercase T, and a basic time format; the T appears in
      *>             the data.
      *>   §15.40.4 r3 - for an OFFSET format argument-3 is reflected
      *>             DIRECTLY in the time portion and argument-4 DIRECTLY in
      *>             the offset portion: +300 minutes is 5h00m, so +0500.
      *>   §15.40.3 r7 - argument-4 omitted for an offset format evaluates as
      *>             though 0 were specified, and §15.3.3.6.1 requires a plus
      *>             sign when both offset subfields are zero: +0000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDT01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION FORMATTED-DATETIME INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(40).
       01 D PIC 9(7) VALUE 143951.
       01 S PIC 9(5) VALUE 45296.
       PROCEDURE DIVISION.
       MAIN.
      *> The THREE-argument form: the bracketed argument-4 omitted.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" D S) TO R
           DISPLAY "A3-LOCAL=" R
      *> The FOUR-argument form: the bracketed argument-4 written.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S 300) TO R
           DISPLAY "A4-OFFSET=" R
      *> The SAME format with the bracket EMPTY - the optional argument is
      *> the only difference between this reference and the one above.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S) TO R
           DISPLAY "A3-OFFSET=" R
      *> §8.4.3.2.3 SR2 - under the REPOSITORY declaration the word FUNCTION
      *> may be omitted; the reference is the SAME function-identifier and
      *> must give the same value at both arities.
           MOVE FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S 300) TO R
           DISPLAY "KOF-A4=" R
           MOVE FORMATTED-DATETIME("YYYYMMDDThhmmss" D S) TO R
           DISPLAY "KOF-A3=" R
           STOP RUN.
