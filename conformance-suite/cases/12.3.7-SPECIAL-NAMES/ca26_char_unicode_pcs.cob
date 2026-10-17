*>--- conformance-case
*> id: ca26_char_unicode_pcs
*> rules: [GR-12.3.7.4-7, RV-15.15.4-1, RV-15.70.4-1, FMT-15.70.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> CA26 (CONFORMANCE-FIX-QUEUE): the native alphanumeric REPERTOIRE is Unicode (UTF-16 code units) — the
      *> established design — so the alphanumeric program collating sequence has up to 0xFFFF positions (ISO §15.15.3
      *> r1). Under a non-native PROGRAM COLLATING SEQUENCE the ALPHABET remaps only its Latin-1 domain (256 code
      *> units); a code unit BEYOND it keeps its NATIVE position (§12.3.7 k)3 — unlisted characters follow the
      *> positioned set in native order). So CHAR(257) is the code unit at ordinal position 257 = U+0100 (no
      *> EC-ARGUMENT-FUNCTION), ORD round-trips it to 257, and U+0100 collates ABOVE every Latin-1 character —
      *> including CHAR(256) = X"FF" (the highest positioned character). Pre-fix the weights domain was masked to 8
      *> bits (& 0xFF): CHAR(257) yielded the §15.3 one-space default and U+0100 aliased to X"00"'s low weight.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA26.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX PROGRAM COLLATING SEQUENCE AL.
       SPECIAL-NAMES. ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(5).
       01 P PIC X.
       01 R PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION ORD(FUNCTION CHAR(257)) TO N.
           DISPLAY "ORD=" N.
           MOVE FUNCTION CHAR(257) TO P.
           MOVE FUNCTION CHAR(256) TO R.
           IF P > R DISPLAY "P-GT-R" ELSE DISPLAY "R-GT-P" END-IF.
           STOP RUN.
