      *>--- conformance-case
      *> id: pb3293_locale_reset_walk
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB3293 (kb/Work): a CLDR reset string takes its elements
      *> from the rules built SO FAR (its NFD walked by UTS #10
      *> S2.1, as ICU's CollationBuilder reads it), not by
      *> whole-key lookup.
      *>   cite.py --check 12.3.7.4 "When the LOCALE phrase is
      *>     specified, the collating sequence identified is defined
      *>     by the locale referenced by locale-name-2 when specified"
      *>     -> OK §12.3.7.4 7) e)
      *>   cite.py --check 8.8.4.2.11 "Comparison then proceeds by the
      *>     algorithm associated with the collating sequence defined
      *>     by category LC_COLLATE from the current locale"
      *>     -> OK §8.8.4.2.11
      *> The locale ZP is "zh-u-co-pinyin": CLDR release-48-2 zh.xml,
      *> type pinyin, tailors the ideographs into pinyin order
      *> (qi U+4E03 < san U+4E09 < yi U+4E00) and LATER resets on
      *> strings holding them:  &'(Y)'<<<P  for each parenthesized
      *> ideograph P (U+3220 for U+4E00, U+3222 for U+4E09, U+3226
      *> for U+4E03). Each P is a tertiary variant of its reset
      *> string, whose ideograph is the TAILORED one, so the three
      *> P order as their ideographs do in pinyin:
      *>   U+3226 < U+3222 < U+3220.
      *> WHY EACH LEG CAN FAIL: the defect read each reset string by
      *> whole key, taking the ideographs' implicit (code point
      *> order) weights U+4E00 < U+4E03 < U+4E09, which ordered
      *> U+3220 < U+3226 < U+3222: legs 2 and 3 printed N.
      *> (Determination L11: tertiary strength, non-ignorable.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3293ZH.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE FOR NATIONAL IS LN.
       SPECIAL-NAMES.
           ALPHABET LN FOR NATIONAL IS LOCALE ZP
           LOCALE ZP IS "zh-u-co-pinyin".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-QI   PIC N(1) VALUE NX"3226".
       01 W-SAN  PIC N(1) VALUE NX"3222".
       01 W-YI   PIC N(1) VALUE NX"3220".
       PROCEDURE DIVISION.
       MAIN.
           IF W-QI < W-SAN
               DISPLAY "P-QI < P-SAN=Y"
           ELSE
               DISPLAY "P-QI < P-SAN=N"
           END-IF
           IF W-SAN < W-YI
               DISPLAY "P-SAN < P-YI=Y"
           ELSE
               DISPLAY "P-SAN < P-YI=N"
           END-IF
           IF W-QI < W-YI
               DISPLAY "P-QI < P-YI=Y"
           ELSE
               DISPLAY "P-QI < P-YI=N"
           END-IF
           STOP RUN.
