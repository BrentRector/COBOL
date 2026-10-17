*>--- conformance-case
*> id: pb835_numvalc_locale_grouping
*> rules: [GR-14.6.13.2-4, AR-15.68.3-5, RV-15.94.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB835 - LC_MONETARY mon_grouping is ENFORCED, not only mon_thousands_sep's identity.
      *> ISO 15.68.3 r5b.6: "Argument-1 may contain one or more grouping separators in accordance with
      *> locale fields mon_thousands_sep and mon_grouping" (8.2.2: mon_grouping = "size of each group of
      *> digits"); 15.94.3 r1 imports the rule into TEST-NUMVAL-C; 15.94.4 r1 b) reports the first character
      *> in error, r1 c) LENGTH+1 for valid-but-incomplete content. DETERMINATION (CONFORMANCE.md A.4.9): no
      *> separator at all is admitted (r5b.6 "may"); once one appears, EVERY mon_grouping position carries one.
      *> The reported position is the first character no grouped completion admits (the scan's existing
      *> longest-admissible-prefix reading of r1 b). Hand-derived expectations:
      *>   US = en-US, mon_grouping 3 repeating:
      *>     "1,234,567.89" 0 (value 1234567.89) | "1234567.89" 0 (no separator)
      *>     "12,34.5"  6 - "12,34" can still become "12,345"; the '.' ends the group short
      *>     "1234,567" 5 - a leftmost run of 4 can never precede a separator
      *>     "1,2345"   6 - the 5th digit overfills a 3-digit group | "1,23" 5 - incomplete: r1 c)
      *>   IND = en-IN, mon_grouping 3 then 2 repeating (the POSIX last-element repetition):
      *>     "12,34,567.00" 0 (value 1234567) | "1,234.00" 0 | "1,234,567.00" 6 - the second group is 2 wide
      *>   E = PIC $9999999 LOCALE US SIZE 12: 14.6.13.2 r4 - a de-edit accepts only a possible editing
      *>     result, and 13.18.40.5 r12 places the separators by mon_grouping: "  $1,234,567" de-edits to
      *>     1234567; "  $12,34,567" and "    $1234567" are EC-DATA-INCOMPATIBLE.
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB835NG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US"
           LOCALE IND IS "en-IN".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 E PIC $9999999 LOCALE IS US SIZE IS 12.
       01 GX REDEFINES G PIC X(12).
       01 R PIC S9(9)V99.
       01 N PIC S9(9).
       01 T PIC 9(2).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       H-P.
           DISPLAY "  INCOMPATIBLE".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE FUNCTION TEST-NUMVAL-C("1,234,567.89" LOCALE US) TO T
           DISPLAY "US GROUPED " T
           COMPUTE R = FUNCTION NUMVAL-C("1,234,567.89" LOCALE US)
           IF R = 1234567.89 DISPLAY "US VALUE OK" ELSE DISPLAY "US VALUE BAD " R END-IF
           MOVE FUNCTION TEST-NUMVAL-C("1234567.89" LOCALE US) TO T
           DISPLAY "US UNGROUPED " T
           MOVE FUNCTION TEST-NUMVAL-C("12,34.5" LOCALE US) TO T
           DISPLAY "US SHORT " T
           MOVE FUNCTION TEST-NUMVAL-C("1234,567" LOCALE US) TO T
           DISPLAY "US WIDE LEAD " T
           MOVE FUNCTION TEST-NUMVAL-C("1,2345" LOCALE US) TO T
           DISPLAY "US OVERFULL " T
           MOVE FUNCTION TEST-NUMVAL-C("1,23" LOCALE US) TO T
           DISPLAY "US INCOMPLETE " T
           MOVE FUNCTION TEST-NUMVAL-C("12,34,567.00" LOCALE IND) TO T
           DISPLAY "IN GROUPED " T
           COMPUTE R = FUNCTION NUMVAL-C("12,34,567.00" LOCALE IND)
           IF R = 1234567 DISPLAY "IN VALUE OK" ELSE DISPLAY "IN VALUE BAD " R END-IF
           MOVE FUNCTION TEST-NUMVAL-C("1,234.00" LOCALE IND) TO T
           DISPLAY "IN FIRST GROUP " T
           MOVE FUNCTION TEST-NUMVAL-C("1,234,567.00" LOCALE IND) TO T
           DISPLAY "IN THREES " T
           MOVE 1234567 TO E
           DISPLAY "EDIT [" GX "]"
           MOVE 0 TO N
           MOVE E TO N
           IF N = 1234567 DISPLAY "DEEDIT OK" ELSE DISPLAY "DEEDIT BAD " N END-IF
           MOVE "  $12,34,567" TO GX
           DISPLAY "DEEDIT MISPLACED"
           MOVE E TO N
           MOVE "    $1234567" TO GX
           DISPLAY "DEEDIT UNGROUPED"
           MOVE E TO N
           STOP RUN.
