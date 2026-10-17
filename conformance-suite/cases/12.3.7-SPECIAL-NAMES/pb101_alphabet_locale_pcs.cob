*>--- conformance-case
*> id: pb101_alphabet_locale_pcs
*> rules: [GR-12.3.7.4-7, GR-14.6.6-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB101 (kb/Work): `ALPHABET name IS LOCALE` (ISO 1989:2023 12.3.7.2, no locale-name-2 = the locale CURRENT
      *> at each use, 12.3.7.4 GR7e) as the PROGRAM COLLATING SEQUENCE. Alphanumeric relation conditions become
      *> LOCALE-BASED comparisons (8.8.4.2.7 / 8.8.4.2.11: trailing spaces truncated, no padding, then the LC_COLLATE
      *> algorithm - COBOL.NET's derived CLDR/UCA engine; determination L11: the locale's tailoring at tertiary
      *> strength, non-ignorable). Under the NATIVE order "Zebra" < "apple" (0x5A < 0x61) and "Zebra" < "zebra"; under
      *> the locale order letters outrank case and accents: apple < Apple < zebra < Zebra, resume < resumE at level 3
      *> and resume < r-e-acute-... at level 2. The Format-2 table SORT and MAX take the same sequence (14.9.40 GR5b,
      *> 15.61); ORD/CHAR read its materialized positions (L7) and round-trip. Every assertion holds for EVERY CLDR
      *> locale (none depends on a tailoring), so the run is host-independent whatever COBOL_USER_LOCALE says.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB101LC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS LOC.
       SPECIAL-NAMES.
           ALPHABET LOC IS LOCALE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-TBL.
          05 W-E PIC X(6) OCCURS 4 TIMES.
       01 W-I PIC 9.
       01 W-A PIC X(6) VALUE "apple".
       01 W-Z PIC X(6) VALUE "Zebra".
       01 W-R PIC X(6) VALUE "resume".
       01 W-M PIC X(6).
       01 W-O1 PIC 9(6).
       01 W-O2 PIC 9(6).
       01 W-O3 PIC 9(6).
       PROCEDURE DIVISION.
       MAIN.
           IF W-A < W-Z DISPLAY "apple < Zebra" ELSE DISPLAY "apple >= Zebra" END-IF.
           IF "zebra" < "Zebra" DISPLAY "zebra < Zebra" ELSE DISPLAY "zebra >= Zebra" END-IF.
           IF "apple" < "Apple" DISPLAY "apple < Apple" ELSE DISPLAY "apple >= Apple" END-IF.
           IF W-R < "resumE" DISPLAY "resume < resumE" ELSE DISPLAY "resume >= resumE" END-IF.
           IF W-R = "resume   " DISPLAY "trailing spaces ignored" ELSE DISPLAY "trailing spaces differ" END-IF.
           MOVE "Zebra" TO W-E(1).
           MOVE "apple" TO W-E(2).
           MOVE "zebra" TO W-E(3).
           MOVE "Apple" TO W-E(4).
           SORT W-E ASCENDING.
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 4
               DISPLAY "SORT" W-I "=" W-E(W-I)
           END-PERFORM.
           MOVE FUNCTION MAX("Zebra" "apple" "zebra") TO W-M.
           DISPLAY "MAX=" W-M.
           MOVE FUNCTION ORD("a") TO W-O1.
           MOVE FUNCTION ORD("A") TO W-O2.
           MOVE FUNCTION ORD("b") TO W-O3.
           IF W-O1 < W-O2 AND W-O2 < W-O3 DISPLAY "ORD a < A < b" ELSE DISPLAY "ORD order unexpected" END-IF.
           IF FUNCTION CHAR(FUNCTION ORD("Q")) = "Q" DISPLAY "CHAR round-trip OK" ELSE DISPLAY "CHAR round-trip FAILED" END-IF.
           STOP RUN.
