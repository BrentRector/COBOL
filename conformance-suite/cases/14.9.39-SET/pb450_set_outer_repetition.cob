      *>--- conformance-case
      *> id: pb450_set_outer_repetition
      *> rules: [GR-14.9.39.4-8, FMT-14.9.39.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> !! THE PRINTED OUTER REPETITION, ON BOTH FORMATS THAT PRINT IT. kb/Work PB450.
      *>
      *> ISO 14.9.39.2 gives Format 3 (switch-setting) and Format 4 (condition-setting) the SAME printed
      *> skeleton: an inner brace `{ name } ...` and an OUTER brace around the whole
      *> `{ name } ... TO { keyword | keyword }` unit, with a trailing `...` on the outer brace. The figure
      *> notes say so in words for both:
      *>   Format 3 - "The whole `{ mnemonic-name-1 } ... TO { ON | OFF }` phrase is itself wrapped in an
      *>               outer brace pair whose trailing `...` repeats it, so several switch-setting phrases
      *>               may follow one SET."
      *>   Format 4 - "The outer braces enclose the single repeated unit; the trailing `...` repeats the
      *>               whole braced portion, so several `condition-name ... TO TRUE|FALSE` groups may be
      *>               written in one SET statement. The inner `...` after `{ condition-name-1 }` repeats
      *>               the condition-name itself."
      *> (Both read off the RENDERED page - PDF p760 / folio 730 - not the OCR, because the ellipsis
      *> placement is a diagram fact. CLAUDE.md rule 1.)
      *> The grammar carried Format 3's outer `...` and had DROPPED Format 4's, so the Format-4 legs below
      *> were `error COBOL0001: unexpected 'TO'` on conforming source while their Format-3 twins compiled.
      *>
      *> Expected values, COMPUTED FROM THE STANDARD (not measured):
      *>   14.9.39.4 GR8 - "If multiple condition-names are specified, the results are the same as if a
      *>   separate SET statement had been written for each condition-name-1" - so the two-group Format-4
      *>   statement is exactly the two single-group statements, in written order, and GR6 places each
      *>   condition-name's FIRST VALUE literal (13.18.63.4 GR20) in its own conditional variable:
      *>     SET F-YES TO TRUE G-HIGH TO TRUE  -> WS-F = "Y" and WS-G = 9
      *>   The inner `...` is exercised on the same statement's first group (two condition-names of ONE
      *>   conditional variable would be a different fact, so the second group's `H-ONE H-TWO` share theirs:
      *>   both name WS-H, the LAST store wins in written order -> WS-H = 2).
      *>   14.9.39.4 GR4/GR5 - a Format-3 group sets its switches to the status its own ON/OFF names, so
      *>     SET SW-A TO ON SW-B TO OFF -> 12.3.7's switch-status conditions answer A-ON and B-OFF.
      *>
      *> COBOL-85 is the earliest edition of both formats (the FALSE arm alone is a COBOL-2002 addition -
      *> constructs.json set-condition-false-2002 - which is why this program writes TRUE only and its
      *> mixed twin lives in the 2002 corpus).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB450OUTREP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW-A ON STATUS IS A-ON OFF STATUS IS A-OFF
           SWITCH-2 IS SW-B ON STATUS IS B-ON OFF STATUS IS B-OFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-F PIC X VALUE "N".
          88 F-YES VALUE "Y".
       01 WS-G PIC 9 VALUE 0.
          88 G-HIGH VALUE 9.
       01 WS-H PIC 9 VALUE 0.
          88 H-ONE VALUE 1.
          88 H-TWO VALUE 2.
       PROCEDURE DIVISION.
       MAIN-P.
           SET F-YES TO TRUE G-HIGH TO TRUE
           DISPLAY "F=" WS-F " G=" WS-G
           SET H-ONE H-TWO TO TRUE
           DISPLAY "H=" WS-H
           SET SW-A TO ON SW-B TO OFF
           IF A-ON DISPLAY "A=ON" ELSE DISPLAY "A=OFF" END-IF
           IF B-OFF DISPLAY "B=OFF" ELSE DISPLAY "B=ON" END-IF
           SET SW-A TO OFF SW-B TO ON
           IF A-OFF DISPLAY "A2=OFF" ELSE DISPLAY "A2=ON" END-IF
           IF B-ON DISPLAY "B2=ON" ELSE DISPLAY "B2=OFF" END-IF
           DISPLAY "DONE"
           STOP RUN.
