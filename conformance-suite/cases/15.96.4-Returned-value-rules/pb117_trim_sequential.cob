*>--- conformance-case
*> id: pb117_trim_sequential
*> rules: [RV-15.96.4-1, RV-15.96.4-2, RV-15.96.4-3, RV-15.96.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB117 — IN THE 2023 CORPUS: TRIM is 2014 and its argument-2 form 2023 (E.3.3 item 31).
      *> FUNCTION TRIM with several argument-2 characters folds SEQUENTIALLY (ISO 15.96.4 r5:
      *> "each argument-2 is processed completely in the order that they are specified"; the NOTE fixes
      *> TRIM(a b c) = TRIM(TRIM(a b) c)). Hand-derived from the rule:
      *>   TRIM("bcab" "c" "b"): inner TRIM("bcab" "c") = "bcab" ('c' guards neither edge), then TRIM(.. "b")
      *>     = "ca". The former set-union gave "a" — the silent wrong answer this pins.
      *>   TRIM("bcab" LEADING "c" "b") same shape (keyword AFTER argument-1, 15.96.2): inner = "bcab", outer leading-'b' = "cab".
      *>   The NOTE's own example TRIM("aabbcc" "c" "b") = "aa" (both readings agree — kept as the r5 text pin).
      *>   Order sensitivity: TRIM("bcab" "b" "c") — inner 'b': "ca"... derive: TRIM("bcab" "b") = "ca"
      *>     (both edges 'b'), then TRIM("ca" "c") = "a". Reversed order gives a DIFFERENT answer than
      *>     "c","b" ("ca") — the order-carrying proof.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB117TR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION TRIM("bcab" "c" "b") TO R
           IF R = "ca" DISPLAY "SEQ OK" ELSE DISPLAY "SEQ BAD [" R "]" END-IF
           MOVE FUNCTION TRIM("bcab" LEADING "c" "b") TO R
           IF R = "cab" DISPLAY "LEAD OK" ELSE DISPLAY "LEAD BAD [" R "]" END-IF
           MOVE FUNCTION TRIM("aabbcc" "c" "b") TO R
           IF R = "aa" DISPLAY "NOTE OK" ELSE DISPLAY "NOTE BAD [" R "]" END-IF
           MOVE FUNCTION TRIM("bcab" "b" "c") TO R
           IF R = "a" DISPLAY "ORDER OK" ELSE DISPLAY "ORDER BAD [" R "]" END-IF
           MOVE FUNCTION TRIM("  x  ") TO R
           IF R = "x" DISPLAY "SPACE OK" ELSE DISPLAY "SPACE BAD [" R "]" END-IF
           STOP RUN.
