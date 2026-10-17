      *>--- conformance-case
      *> id: l1c24_replace_free_form_result
      *> rules: [GR-7.2.4.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.2.4.4 GR10 — REPLACE result is free-form text; spaces
      *> only where they already exist
      *> RULE §7.2.4.4 GR10: "The text that results from the processing
      *>   of a REPLACE statement shall be in logical free-form
      *>   reference
      *>   format. Text-words inserted into the resultant text as a
      *>   result of processing a REPLACE statement are placed in
      *>   accordance with the rules of free-form reference format. When
      *>   inserting text-words of pseudo-text-2 into the resultant
      *>   text,
      *>   additional spaces may be introduced only between text-words
      *>   where there already exists a space or a space is assumed.
      *>   NOTE 2 A space is assumed at the end of a source line."
      *>   cite.py --check 7.2.4.4 "additional spaces may be introduced
      *>   only between text-words where there already exists a space"
      *>     -> OK §7.2.4.4 10)
      *>   cite.py --check 7.2.4.4 "A space is assumed at the end of a
      *>   source line." -> OK §7.2.4.4 10) (NOTE 2)
      *> EXPECTED OUTPUT, DERIVED:
      *>   Line 1: LW (2 chars) becomes a 40-char literal on a
      *>     fixed-form
      *>     line whose tail literal already ends AT column 72, so the
      *>     result line runs past column 72. The result is free-form,
      *>     which has no column-72 program-text limit: nothing is cut,
      *>     and DISPLAY shows the two literals concatenated in full.
      *>   Line 2: pseudo-text-2 spans three source lines. Its words
      *>     "MULTI-" and "LINE" are separated only by the space assumed
      *>     at the end of a line (NOTE 2); they stay two operands, so
      *>     DISPLAY prints MULTI-LINE.
      *>   Line 3: a literal is ONE text-word, so no space may be added
      *>     or removed inside it: "A  B   C" prints with 2 and 3
      *>     spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24J.
       PROCEDURE DIVISION.
       MAIN-P.
           REPLACE ==LW== BY
               =="LONG-PSEUDO-TEXT-2-WORD-0123456789ABCDEF"==
                   ==MLT== BY ==
                   "MULTI-"
                   "LINE" ==
                   ==SPW== BY =="A  B   C"==.
           DISPLAY LW "|TAIL-OF-THE-ORIGINAL-LINE-ENDING-AT-COLUMN-72|".
           DISPLAY MLT.
           DISPLAY SPW.
           STOP RUN.
