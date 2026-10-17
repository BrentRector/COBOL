*>--- conformance-case
*> id: pb2766_numvalc_locale_l12_argument
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB2766 - DETERMINATION L12 is ONE equivalence on both sides of
      *> the NUMVAL-C / TEST-NUMVAL-C LOCALE comparison (ISO 15.68.3 r5 b:
      *> "a format consistent with the specifications of locale category
      *> LC_MONETARY"; 15.94.4 r1). The snapshot removes every Unicode Cf
      *> character and folds U+00A0/U+202F/U+2009 to the plain space, so
      *> argument-1 is folded the same way: a Cf character (U+200F here)
      *> is ignorable, a no-break space is a space wherever a space is
      *> admitted, and TEST-NUMVAL-C still reports the ORIGINAL argument's
      *> positions. The CURRENT locale is the harness-pinned INVARIANT
      *> (currency U+00A4, decimal '.', grouping ',', parentheses negative).
      *>   CF-LEAD  - U+200F before the currency sign: value 1234.56.
      *>   CF-PAREN - "(" U+200F "U+00A4 1.5)": the negative convention, -1.5.
      *>   NBSP     - U+00A4 U+00A0 "1,234.56": conforming (0).
      *>   POS      - "0" U+200F " 1": 15.94.4 r1 b.1's example, shifted
      *>              by the removed mark to position 4.
      *>   END      - "1,23" U+200F: r1 c, LENGTH + 1 of the argument = 6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2766NV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 PIC N(10)
           VALUE NX"200F00A40031002C003200330034002E00350036".
       01 A2 PIC N(7)
           VALUE NX"0028200F00A40031002E00350029".
       01 A3 PIC N(10)
           VALUE NX"00A400A00031002C003200330034002E00350036".
       01 A4 PIC N(4)
           VALUE NX"0030200F00200031".
       01 A5 PIC N(5)
           VALUE NX"0031002C00320033200F".
       01 R PIC S9(7)V99.
       01 T PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE R = FUNCTION NUMVAL-C(A1 LOCALE)
           IF R = 1234.56 DISPLAY "CF-LEAD OK"
           ELSE DISPLAY "CF-LEAD BAD " R END-IF
           COMPUTE R = FUNCTION NUMVAL-C(A2 LOCALE)
           IF R = -1.5 DISPLAY "CF-PAREN OK"
           ELSE DISPLAY "CF-PAREN BAD " R END-IF
           MOVE FUNCTION TEST-NUMVAL-C(A3 LOCALE) TO T
           IF T = 0 DISPLAY "NBSP OK" ELSE DISPLAY "NBSP BAD " T END-IF
           MOVE FUNCTION TEST-NUMVAL-C(A4 LOCALE) TO T
           IF T = 4 DISPLAY "POS OK" ELSE DISPLAY "POS BAD " T END-IF
           MOVE FUNCTION TEST-NUMVAL-C(A5 LOCALE) TO T
           IF T = 6 DISPLAY "END OK" ELSE DISPLAY "END BAD " T END-IF
           STOP RUN.
