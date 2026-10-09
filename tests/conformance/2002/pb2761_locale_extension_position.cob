      *> PB2761 (kb/Work): a CLDR /extension is NOT part of the reset
      *> position the next relation is placed after.
      *>   cite.py --check 12.3.7.4 "When the LOCALE phrase is
      *>     specified, the collating sequence identified is defined
      *>     by the locale referenced by locale-name-2 when specified"
      *>     -> OK §12.3.7.4 7)  (GR7 e))
      *>   cite.py --check 8.8.4.2.11 "Comparison then proceeds by the
      *>     algorithm associated with the collating sequence defined
      *>     by category LC_COLLATE from the current locale"
      *>     -> OK §8.8.4.2.11
      *> The locale FT is "fi-u-co-traditional": CLDR release-48-2
      *> fi.xml, type traditional, rule  &T<<þ/h<<<Þ/h  (UTS #35
      *> Part 5 "Expansions": x/y sorts as x followed by y, and the
      *> next relation in the chain is placed after x alone). So
      *>   þ = [t'] [h]   t' = a new secondary right after T's
      *>   Þ = [t''] [h]  t'' = a new tertiary right after þ's t'
      *> (two primaries t, h each; determination L11: tertiary
      *> strength, non-ignorable). WHY EACH LEG CAN FAIL:
      *>   Þa vs thb: primaries t h a < t h b -> LT. The defect gave
      *>     Þ the elements [t''] [h] [h] (the h of þ's extension
      *>     carried into the position), primaries t h h a > t h b.
      *>   þa vs thb: the first link, right before and after -> LT.
      *>   þa vs Þa: equal at levels 1-2; Þ's tertiary follows þ's
      *>     -> LT.
      *>   tha vs þa: equal at level 1; t's secondary is common, þ's
      *>     is after it -> LT.
      *> NX literals are UTF-16 code units: 00DE Þ, 00FE þ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2761FT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE FOR NATIONAL IS LN.
       SPECIAL-NAMES.
           ALPHABET LN FOR NATIONAL IS LOCALE FT
           LOCALE FT IS "fi-u-co-traditional".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-UTA  PIC N(3) VALUE NX"00DE0061".
       01 W-LTA  PIC N(3) VALUE NX"00FE0061".
       01 W-THB  PIC N(3) VALUE NX"007400680062".
       01 W-THA  PIC N(3) VALUE NX"007400680061".
       PROCEDURE DIVISION.
       MAIN.
           IF W-UTA < W-THB
               DISPLAY "UTHORN-A < THB=Y"
           ELSE
               DISPLAY "UTHORN-A < THB=N"
           END-IF
           IF W-LTA < W-THB
               DISPLAY "LTHORN-A < THB=Y"
           ELSE
               DISPLAY "LTHORN-A < THB=N"
           END-IF
           IF W-LTA < W-UTA
               DISPLAY "LTHORN-A < UTHORN-A=Y"
           ELSE
               DISPLAY "LTHORN-A < UTHORN-A=N"
           END-IF
           IF W-THA < W-LTA
               DISPLAY "THA < LTHORN-A=Y"
           ELSE
               DISPLAY "THA < LTHORN-A=N"
           END-IF
           STOP RUN.
