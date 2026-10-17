*>--- conformance-case
*> id: find_string
*> rules: [RV-15.37.4-1, RV-15.37.4-2, RV-15.37.4-3, RV-15.37.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.37 FIND-STRING — the 1-based position of argument-2 within argument-1. Default: the FIRST
      *> occurrence; LAST (rule 1) the last; [START AFTER] argument-3 (rule 2) ignores that many matches first;
      *> ANYCASE (rule 4) folds case. No match / zero-length argument returns 0 (rules 3, 5). HAY = "ABCABCABC"
      *> has "ABC" at non-overlapping positions 1, 4, 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INTRFIND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-HAY  PIC X(9)  VALUE "ABCABCABC".
       01 WS-NDL  PIC X(3)  VALUE "ABC".
       01 WS-TXT  PIC X(11) VALUE "Hello World".
       01 WS-P    PIC 9.
       01 WS-BIG  PIC 9(19) VALUE 9999999999999999999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FIND-STRING(WS-HAY WS-NDL) TO WS-P.
           DISPLAY "FIRST=" WS-P.
           MOVE FUNCTION FIND-STRING(WS-HAY WS-NDL LAST) TO WS-P.
           DISPLAY "LAST=" WS-P.
           MOVE FUNCTION FIND-STRING(WS-HAY WS-NDL START AFTER 1) TO WS-P.
           DISPLAY "SKIP1=" WS-P.
           MOVE FUNCTION FIND-STRING(WS-HAY WS-NDL LAST START AFTER 1) TO WS-P.
           DISPLAY "LASTSKIP1=" WS-P.
           MOVE FUNCTION FIND-STRING(WS-TXT "WORLD" ANYCASE) TO WS-P.
           DISPLAY "ANYCASE=" WS-P.
           MOVE FUNCTION FIND-STRING(WS-TXT "WORLD") TO WS-P.
           DISPLAY "CASED=" WS-P.
           MOVE FUNCTION FIND-STRING(WS-HAY "ZZZ") TO WS-P.
           DISPLAY "NONE=" WS-P.
      *> ARGUMENT-3 IS TOTAL (kb/Work PB254). ISO §15.37.3 r3 is the whole argument rule —
      *> "argument-3 shall be an integer data item or integer literal" — and places NO
      *> constraint on the value, while §15.37.4 r2/r3 answer for every integer: ignore that
      *> many matches, and if none is left return zero. So a value beyond the long the body
      *> once took is CONFORMING source and must reach rule 2 intact.
      *> EXHAUST3 — the in-range boundary: HAY has matches at 1, 4 and 7, so ignoring 3 of
      *> them leaves none and rule 3 answers 0.
           MOVE FUNCTION FIND-STRING(WS-HAY WS-NDL START AFTER 3)
               TO WS-P.
           DISPLAY "EXHAUST3=" WS-P.
      *> WIDESKIP / WIDELASTSKIP — a 19-digit LITERAL and a 19-digit DATA ITEM, both above
      *> 9 223 372 036 854 775 807. Rule 2 ignores every match, rule 3 returns 0. Before
      *> PB254 the §15.3 narrowing landing substituted its checking-off default 0 for the
      *> ARGUMENT — "ignore no matches" — and the function answered 1, and 7 under LAST.
           MOVE FUNCTION FIND-STRING(WS-HAY WS-NDL START AFTER
               9999999999999999999) TO WS-P.
           DISPLAY "WIDESKIP=" WS-P.
           MOVE FUNCTION FIND-STRING(WS-HAY WS-NDL LAST START AFTER
               WS-BIG) TO WS-P.
           DISPLAY "WIDELASTSKIP=" WS-P.
           STOP RUN.
       END PROGRAM INTRFIND.
