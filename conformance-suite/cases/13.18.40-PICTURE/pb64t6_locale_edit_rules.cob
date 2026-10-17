*>--- conformance-case
*> id: pb64t6_locale_edit_rules
*> rules: [GR-13.18.40.4-17, GR-13.18.40.4-18, GR-13.18.40.5-9,
*>    GR-13.18.40.5-10, GR-13.18.40.5-12, GR-13.18.40.5-13,
*>    GR-13.18.40.5-15]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB64 T6 — the format-2 editing rules battery (ISO 13.18.40.5 r14/r15, 13.18.40.4 GR16-GR18) under the
      *> NAMED locale en-US (all-ASCII: currency "$", decimal ".", grouping "," by threes, negative convention
      *> "-$n" per CurrencyNegativePattern 1, positive sign "+" at the determined p_sign_posn 1).
      *> Hand-derived expectations, each from its rule:
      *>   A +1234.50 -> "+$    1,234.50" right-justified in 20 (r14 a; suppressed zeros AND the suppressed run's
      *>     grouping separator become spaces BETWEEN the currency string and the first significant digit - r15 a
      *>     "any character position"; nothing floats).
      *>   A -1234.50 -> "-$    1,234.50" (r13 + the locale's negative convention).
      *>   de-edit A -> -1234.50 recovered (14.9.25.4 GR5/GR6 d - "may be signed").
      *>   B (all-Z picture) ZERO -> ALL 10 positions spaces, no separator, no currency, no sign (r15 b).
      *>   B 5.25 -> " $    5.25" (suppression stops at the first nonzero digit).
      *>   C (BLANK WHEN ZERO) ZERO -> all spaces (r10 precedence); 42 -> "  $0,042" (no Z - no suppression).
      *>   D SIZE 8 vs a 12-position hypothetical: the four truncated characters are ALL suppressed-zero spaces,
      *>     so the truncation is SILENT (r14 b's exemption) - "1,234.50".
      *>   E no '.' in the picture -> no decimal separator at all (GR18 - '.' is its only source).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T6ER.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC +$ZZZZZZ9.99 LOCALE IS US SIZE IS 20.
       01 B PIC $ZZZZ.ZZ LOCALE IS US SIZE IS 10.
       01 C PIC $9999 LOCALE IS US SIZE IS 8 BLANK WHEN ZERO.
       01 D PIC ZZZZZZ9.99 LOCALE IS US SIZE IS 8.
       01 E PIC $9999999 LOCALE IS US SIZE IS 12.
       01 N PIC S9(7)V99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1234.50 TO A
           DISPLAY "[" A "]"
           MOVE -1234.50 TO A
           DISPLAY "[" A "]"
           MOVE A TO N
           IF N = -1234.50
               DISPLAY "DEEDIT OK"
           ELSE
               DISPLAY "DEEDIT BAD " N
           END-IF
           MOVE ZERO TO B
           DISPLAY "[" B "]"
           MOVE 5.25 TO B
           DISPLAY "[" B "]"
           MOVE ZERO TO C
           DISPLAY "[" C "]"
           MOVE 42 TO C
           DISPLAY "[" C "]"
           MOVE 1234.50 TO D
           DISPLAY "[" D "]"
           MOVE 1234567 TO E
           DISPLAY "[" E "]"
           STOP RUN.
