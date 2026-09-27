      *> kb/Work PB1592 - compile-time arithmetic at COBOL-2002/2014 is
      *>   STANDARD ARITHMETIC, the mode the previous standards
      *>   prescribed; the 2023 twin is 2023/gl2m2_ct_arith_mode.
      *> Rules: Annex E.2 6) "The mode of arithmetic used in evaluating
      *>   compile-time arithmetic expressions and the handling of
      *>   intermediate results is now explicitly implementor defined."
      *>   and "The previous COBOL Standard required the use of an
      *>   arithmetic mode that is no longer supported." (both -> OK
      *>   §E.2 6)); Annex E.2 21) lists "Standard Arithmetic" among the
      *>   removed obsolete elements (-> OK §E.2 21)) - the one mode
      *>   2023 removed, so the mode 2002/2014 prescribed.
      *>   §7.3.6.3 GR2 "The implementor shall define and document which
      *>   mode of arithmetic is to be used when evaluating compile-time
      *>   arithmetic." -> OK §7.3.6.3 2) (the 2023 latitude only).
      *>   §7.3.6.3 GR3 "The final result of the arithmetic expression
      *>   shall be truncated to the integer part of the value" -> OK
      *>   §7.3.6.3 3).
      *> COBOL.NET's standard arithmetic is the standard-decimal
      *>   intermediate data item: "a maximum precision of 34 decimal
      *>   digits" (-> OK §8.8.1.5.2); an inexact intermediate rounds
      *>   NEAREST-AWAY-FROM-ZERO, "If the INTERMEDIATE ROUNDING clause
      *>   is not specified, the NEAREST-AWAY-FROM-ZERO phrase is
      *>   implied." (-> OK §11.9.11.2 a), under GR3 standard-decimal).
      *> DERIVATION of every .out line (34 digits, away from zero, GR3):
      *>   D1=002  2/3 = 0.666...667 (34 digits); *3 = 2.000...0001,
      *>           rounded to 34 digits 2.000...000; GR3 -> 2.
      *>   D2=100  100/7 = 14.285...1429 (34); *7 = 100.000...0003,
      *>           rounded -> 100.000...000; GR3 -> 100.
      *>   D3=000  1/3 = 0.333...333 (34); *3 = 0.999...999; GR3 -> 0.
      *>   T1=002  5E-28/2 = 2.5E-28 exactly; *1E28 = 2.5; GR3 -> 2.
      *>   T2=007  15E-28/2 = 7.5E-28 exactly; *1E28 = 7.5; GR3 -> 7.
      *>           (The 2023 System.Decimal mode rounds 7.5E-28 to 8E-28
      *>           at 28 places and gives 8 - the edition difference.)
      *>   OV=99999999999999999999999999990  the exact 29-digit product
      *>           fits 34 digits (2023 refuses it: beyond 7.9E28).
      *>   OV2=9999999999999999999999999999  K-OV / 10, the
      *>           constant-name operand substituting its 29-digit
      *>           value (§13.10.3 SR2 -> OK §13.10.3 2)).
      *>   TIE=10010  1000000000000000000000000000001 * 10005 is exactly
      *>           10005000000000000000000000000010005 (35 digits): the
      *>           34-digit result drops a lone final 5 - a tie - and
      *>           away from zero gives ...10010 (nearest-even
      *>           ...10000);
      *>           minus the exact 1E30 * 10005 leaves 10010.
      *>   IF-OV / IF-T2=7  the same mode in a >>DEFINE value and a >>IF
      *>           constant conditional expression (§7.3.8 via §7.3.6);
      *>           V-H and V-BIG are single literals, so they keep their
      *>           values (§7.3.11.4 GR5: "treated as a literal, not as
      *>           an arithmetic-expression" -> OK §7.3.11.4 5)), and
      *>           V-H / 2 * V-BIG is T2's expression: 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1592S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K-D1 CONSTANT AS 2 / 3 * 3.
       01 K-D2 CONSTANT AS 100 / 7 * 7.
       01 K-D3 CONSTANT AS 1 / 3 * 3.
       01 K-T1 CONSTANT AS
             0.0000000000000000000000000005 / 2
             * 10000000000000000000000000000.
       01 K-T2 CONSTANT AS
             0.0000000000000000000000000015 / 2
             * 10000000000000000000000000000.
       01 K-OV CONSTANT AS 9999999999999999999999999999 * 10.
       01 K-OV2 CONSTANT AS K-OV / 10.
       01 K-TIE CONSTANT AS
             1000000000000000000000000000001 * 10005
             - 1000000000000000000000000000000 * 10005.
       01 W-N PIC 999.
       01 W-T PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE K-D1 TO W-N
           DISPLAY "D1=" W-N
           MOVE K-D2 TO W-N
           DISPLAY "D2=" W-N
           MOVE K-D3 TO W-N
           DISPLAY "D3=" W-N
           MOVE K-T1 TO W-N
           DISPLAY "T1=" W-N
           MOVE K-T2 TO W-N
           DISPLAY "T2=" W-N
           DISPLAY "OV=" K-OV
           DISPLAY "OV2=" K-OV2
           MOVE K-TIE TO W-T
           DISPLAY "TIE=" W-T
       >>DEFINE V-OV AS 9999999999999999999999999999 * 10
       >>IF V-OV = 99999999999999999999999999990
           DISPLAY "IF-OV"
       >>ELSE
           DISPLAY "IF-OV-WRONG"
       >>END-IF
       >>DEFINE V-H AS 0.0000000000000000000000000015
       >>DEFINE V-BIG AS 10000000000000000000000000000
       >>IF V-H / 2 * V-BIG = 7
           DISPLAY "IF-T2=7"
       >>ELSE
           DISPLAY "IF-T2-NOT-7"
       >>END-IF
           STOP RUN.
