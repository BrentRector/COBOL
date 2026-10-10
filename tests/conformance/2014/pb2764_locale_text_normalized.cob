      *> ISO 15.52 LOCALE-DATE, 15.53 LOCALE-TIME, 15.54 LOCALE-TIME-FROM-
      *> SECONDS - kb/Work PB2764. DETERMINATIONS L10/L12/L13 of docs/
      *> CONFORMANCE.md: every locale-sourced string a result carries is
      *> normalized (Unicode Cf removed, U+00A0/U+202F/U+2009 mapped to
      *> the plain space), whether it comes from the d_fmt/t_fmt PATTERN
      *> or from a separator or designator the pattern names; and the
      *> date rendered is the Gregorian date argument-1 names (15.52.3
      *> "CURRENT-DATE positions 1-8 form"), never the culture's own
      *> calendar. A 2014 golden because 15.54 is a 2014 introduction.
      *>
      *>   AR-*   - ar-EG's d_fmt is d/M/yyyy and ICU puts U+200F inside
      *>            each "/" separator (28 ar-* cultures): the result is
      *>            "8/10/2026", 9 positions, not 11 with two invisible
      *>            marks that consume receiver positions.
      *>   ES-*   - es-US's AM/PM designators are the HOST ICU's data:
      *>            "a.<U+00A0>m." in the Windows ICU (nine es-* / yrl-*
      *>            cultures), "a.m." in the Linux ICU CI runs. So the
      *>            lines show the host-independent head (the time, the
      *>            plain space before the designator, the seconds
      *>            fraction with the locale's decimal point) and
      *>            ES-NBSP counts the U+00A0 left in all three results:
      *>            zero, wherever the designator carried one.
      *>   FA/TH  - fa-IR (Persian calendar) and th-TH (Thai Buddhist
      *>            calendar) render the GREGORIAN year, month and day
      *>            of 2026-10-08, not 1405/7/16 or 8/10/2569.
      *> Every DISPLAY is ASCII after normalization.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2764LT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE ARA IS "ar-EG"
           LOCALE ESU IS "es-US"
           LOCALE FAR IS "fa-IR"
           LOCALE THA IS "th-TH".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S               PIC X(30).
       01  N               PIC 99.
       01  K               PIC 99.
       PROCEDURE DIVISION.
           MOVE FUNCTION LOCALE-DATE("20261008" ARA) TO S
           DISPLAY "AR-DATE=[" FUNCTION TRIM(S) "]"
           MOVE FUNCTION LENGTH(FUNCTION LOCALE-DATE("20261008" ARA))
             TO N
           DISPLAY "AR-LEN=" N
           MOVE 0 TO K
           MOVE FUNCTION LOCALE-TIME("093000" ESU) TO S
           DISPLAY "ES-AM=[" S(1:9) "]"
           INSPECT S TALLYING K FOR ALL X"A0"
           MOVE FUNCTION LOCALE-TIME("214500" ESU) TO S
           DISPLAY "ES-PM=[" S(1:9) "]"
           INSPECT S TALLYING K FOR ALL X"A0"
           MOVE FUNCTION LOCALE-TIME-FROM-SECONDS(34200.5 ESU) TO S
           DISPLAY "ES-FRAC=[" S(1:11) "]"
           INSPECT S TALLYING K FOR ALL X"A0"
           DISPLAY "ES-NBSP=" K
           MOVE FUNCTION LOCALE-DATE("20261008" FAR) TO S
           DISPLAY "FA-DATE=[" FUNCTION TRIM(S) "]"
           MOVE FUNCTION LOCALE-DATE("20261008" THA) TO S
           DISPLAY "TH-DATE=[" FUNCTION TRIM(S) "]"
           STOP RUN.
