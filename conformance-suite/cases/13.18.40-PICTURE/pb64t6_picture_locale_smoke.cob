*>--- conformance-case
*> id: pb64t6_picture_locale_smoke
*> rules: [GR-13.18.40.4-16, GR-13.18.40.5-11, GR-14.6.6-6,
*>    FMT-13.18.40.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB64 T6 smoke — PICTURE format 2 end to end (ISO 13.18.40.2; kb/Work PB64 T6). P edits under the NAMED
      *> locale en-US (all-ASCII output: currency "$", decimal ".", grouping "," by threes, negative convention
      *> "-$n" per CurrencyNegativePattern 1, positive "+" at the determined p_sign_posn 1); Q edits under the
      *> CURRENT locale (the harness pins COBOL_USER_LOCALE=INVARIANT, currency U+00A4 - witnessed by FUNCTION
      *> ORD, never printed).
      *> Hand-derived (13.18.40.5 r14/r15): -1234.50 into +$ZZZZZZ9.99 SIZE 20 - the hypothetical is
      *> "-$    1,234.50" (14: suppressed zeros sit BETWEEN the currency string and the first significant digit;
      *> the suppressed run's grouping separator becomes a space too), right-justified into 20 with space fill.
      *> The de-editing MOVE back recovers -1234.50 (14.9.25.4 GR5/GR6d). ZERO is NONNEGATIVE (the p_* fields
      *> govern): "+$        0.00". Q: 1 into $9 SIZE 2 under the invariant locale is U+00A4 then "1" - ORD is
      *> the ordinal position, code point + 1 = 165.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T6SM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P PIC +$ZZZZZZ9.99 LOCALE IS US SIZE IS 20.
       01 Q PIC $9 LOCALE SIZE 2.
       01 N PIC S9(7)V99 VALUE -1234.50.
       01 D PIC S9(7)V99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE N TO P
           DISPLAY "[" P "]"
           MOVE P TO D
           IF D = -1234.50
               DISPLAY "ROUNDTRIP OK"
           ELSE
               DISPLAY "ROUNDTRIP BAD " D
           END-IF
           MOVE ZERO TO P
           DISPLAY "[" P "]"
           MOVE 1 TO Q
           IF FUNCTION ORD(Q(1:1)) = 165 AND Q(2:1) = "1"
               DISPLAY "CURRENT LOCALE OK"
           ELSE
               DISPLAY "CURRENT LOCALE BAD " FUNCTION ORD(Q(1:1))
           END-IF
           STOP RUN.
