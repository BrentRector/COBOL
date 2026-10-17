      *>--- conformance-case
      *> id: pb528_picture_composition_comma_85
      *> rules: [SR-13.18.40.3-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB528 - the POSITIVE half of ISO 1989:2023 13.18.40.3 SR13, "When the DECIMAL-POINT IS COMMA
      *> clause is specified, the symbol comma is the decimal separator and the symbol period is the grouping
      *> separator. The rules for the symbol period apply to the symbol comma, and the rules for the symbol
      *> comma apply to the symbol period", and 13.18.40.6's "the precedence rules for the symbols comma and
      *> period are interchanged". The composition validator takes the two ROLES as a parameter rather than
      *> keeping a comma copy of every period rule, so this golden is what proves the parameter is actually
      *> read: every picture here has MORE THAN ONE PERIOD, which SR12 b forbids of the DECIMAL separator and
      *> permits of the GROUPING separator. Both are NIST shapes (NC107A, SM103A) - a validator that missed
      *> SR13 would reject them, which is why they are pinned here and not only in the NIST corpus.
      *>
      *> M01 9.999.999,99 with 1234567.89 - two grouping periods, one decimal comma. 13.18.40.5 rule 3 makes
      *>     the grouping separator a simple insertion => "1.234.567,89".
      *> M02 ZZ.ZZZ.ZZZ,99 with 1234.5 - rule 7 a suppresses the leading zero positions AND the grouping
      *>     separators embedded in the suppression string ("Any of the simple insertion editing symbols
      *>     embedded in this string or to the immediate right of this string are part of the string"), so
      *>     the first six positions blank and the run resumes at the first nonzero => "     1.234,50".
      *> M03 99,99 with 12.34 - the comma IS the decimal separator here => "12,34".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB528DPL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 M01 PIC 9.999.999,99.
       01 M02 PIC ZZ.ZZZ.ZZZ,99.
       01 M03 PIC 99,99.
       PROCEDURE DIVISION.
           MOVE 1234567,89 TO M01
           MOVE 1234,5 TO M02
           MOVE 12,34 TO M03
           DISPLAY "M01=[" M01 "]"
           DISPLAY "M02=[" M02 "]"
           DISPLAY "M03=[" M03 "]"
           STOP RUN.
