      *> ISO 15.37.4 r4 (FIND-STRING), 15.87.4 r5 (SUBSTITUTE) and 15.68.3 r4 f (NUMVAL-C / TEST-NUMVAL-C) define
      *> ANYCASE by "the rules for the LOWER-CASE function without the LOCALE argument". 15.57.4 r3: when
      *> no LOCALE phrase is given and a locale is in effect for character classification (12.3.6 OBJECT-COMPUTER
      *> CHARACTER CLASSIFICATION), the correspondence of uppercase to lowercase letters comes from that
      *> locale's LC_CTYPE. So under a Turkish classification (the lowercase of "I" is the DOTLESS small i
      *> U+0131, and "i" stays the dotted small i) every ANYCASE matcher folds "I" to U+0131, exactly as
      *> LOWER-CASE does.
      *>
      *> kb/Work PB2631: LOWER-CASE took the classification and the three ANYCASE families never did - and two of
      *> them used a different fold (OrdinalIgnoreCase) from the third. Expected values, derived from the rule:
      *>   FIND-STRING("ISTANBUL" "i" ANYCASE)  -> 0   hay lowers to U+0131 STANBUL; no dotted i anywhere
      *>   FIND-STRING("ISTANBUL" "ı" ANYCASE)  -> 1   needle U+0131 matches the folded "I"
      *>   SUBSTITUTE("ISTANBUL" ANYCASE "i" "X") -> "ISTANBUL" unchanged (no dotted i after the fold)
      *>   SUBSTITUTE("ISTANBUL" ANYCASE "ı" "X") -> "XSTANBUL"
      *>   TEST-NUMVAL-C("I12" "i" ANYCASE) -> 1  the currency string "i" does not match the folded "I" at position 1
      *>   TEST-NUMVAL-C("I12" "ı" ANYCASE) -> 0  it matches U+0131 and 12 follows
      *>   NUMVAL-C("I12" "ı" ANYCASE)      -> 12
      *> Non-ASCII appears only inside literals (UTF-8 source); every DISPLAY is ASCII.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2631ACF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X CHARACTER CLASSIFICATION IS TR.
       SPECIAL-NAMES.
           LOCALE TR IS "tr-TR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 HAY PIC X(8) VALUE "ISTANBUL".
       01 DOTTED PIC X VALUE "i".
       01 DOTLESS PIC X VALUE "ı".
       01 SRC PIC X(3) VALUE "I12".
       01 N PIC 9(3).
       01 R PIC S9(3).
       01 OUT PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N = FUNCTION FIND-STRING(HAY DOTTED ANYCASE).
           DISPLAY "1-FIND-DOTTED=" N.
           COMPUTE N = FUNCTION FIND-STRING(HAY DOTLESS ANYCASE).
           DISPLAY "2-FIND-DOTLESS=" N.
           MOVE FUNCTION SUBSTITUTE(HAY ANYCASE DOTTED "X") TO OUT.
           DISPLAY "3-SUBSTITUTE-DOTTED=" OUT.
           MOVE FUNCTION SUBSTITUTE(HAY ANYCASE DOTLESS "X") TO OUT.
           DISPLAY "4-SUBSTITUTE-DOTLESS=" OUT.
           COMPUTE N = FUNCTION TEST-NUMVAL-C(SRC DOTTED ANYCASE).
           DISPLAY "5-TEST-NUMVAL-C-DOTTED=" N.
           COMPUTE N = FUNCTION TEST-NUMVAL-C(SRC DOTLESS ANYCASE).
           DISPLAY "6-TEST-NUMVAL-C-DOTLESS=" N.
           COMPUTE R = FUNCTION NUMVAL-C(SRC DOTLESS ANYCASE).
           DISPLAY "7-NUMVAL-C-DOTLESS=" R.
           STOP RUN.
