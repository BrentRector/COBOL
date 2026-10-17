*>--- conformance-case
*> id: pb250_test_numvalc_remaining_shapes
*> rules: [FMT-15.94.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB250 - the three TEST-NUMVAL-C shapes the FMT-15.94.2 inventory row recorded as still
      *> owed. 15.94.2's general format is `FUNCTION TEST-NUMVAL-C ( argument-1 [ LOCALE [ locale-name-1 ]
      *> | argument-2 ] [ ANYCASE ] )` - a bracketed OPTIONAL stack of two ALTERNATIVES plus an orthogonal
      *> optional ANYCASE (5.2.6.2), so eight shapes are legal. Five were already covered by
      *> 2002/intrinsics_test_validators and 2002/pb64t6_numvalc_locale; these are the other three.
      *> 15.94.4 1)a): the returned value is 0 when argument-1 conforms to the NUMVAL-C argument rules.
      *> The pinned CURRENT locale is the same harness invariant pb64t6_numvalc_locale documents
      *> (decimal '.', grouping ','), and US is en-US whose int_curr_symbol is "USD" (15.68.3 5) b.3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB250SHP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
      *> SHAPE 2 of 8 - argument-1 ANYCASE, with NEITHER argument-2 nor LOCALE. 15.68.3 3) then supplies the
      *> one currency string of the compilation unit (no SPECIAL-NAMES CURRENCY clause here, so the default
      *> currency sign '$'), and 15.68.3 4) f)'s case fold is a no-op on a sign with no case: the argument
      *> conforms and 15.94.4 1)a) returns 0.
           MOVE FUNCTION TEST-NUMVAL-C("$1,234.56" ANYCASE) TO T
           IF T = 0 DISPLAY "A1CASE OK" ELSE DISPLAY "A1CASE BAD " T END-IF
      *> SHAPE 6 of 8 - argument-1 LOCALE ANYCASE, the bare LOCALE keyword with ANYCASE straight after it.
      *> The word following LOCALE is ANYCASE, so it is NOT consumed as locale-name-1 and the current locale
      *> applies. "1,234.56" carries no currency string for the fold to touch, so it conforms: 0.
           MOVE FUNCTION TEST-NUMVAL-C("1,234.56" LOCALE ANYCASE) TO T
           IF T = 0 DISPLAY "LOCCASE OK" ELSE DISPLAY "LOCCASE BAD " T END-IF
      *> SHAPE 8 of 8 - argument-1 LOCALE locale-name-1 ANYCASE, all four elements. Under en-US the
      *> international currency string is "USD"; the argument spells it "usd", so ONLY 15.68.3 4) f)'s fold
      *> makes it match. pb64t6_numvalc_locale pins the contrast at the same data: without ANYCASE this same
      *> call returns 1 (the 'u' is the first character in error), and NUMVAL-C with ANYCASE returns 1234.56.
           MOVE FUNCTION TEST-NUMVAL-C("usd1,234.56" LOCALE US ANYCASE) TO T
           IF T = 0 DISPLAY "LOCNAMECASE OK" ELSE DISPLAY "LOCNAMECASE BAD " T END-IF
           STOP RUN.
