      *> reject-at: 2002 2014 2023
      *> kb/Work PB1592 - a compile-time arithmetic RESULT that no
      *>   literal of the edition can spell is refused, never stored.
      *> Rules: §7.3.6.3 GR3 "the resultant value shall be considered
      *>   to be an integer numeric literal" (-> OK §7.3.6.3 3)), and
      *>   "The implementor shall allow for fixed-point numeric
      *>   literals of 1 through 31 digits in length" (-> OK
      *>   §8.3.3.3.2) - COBOL.NET's limit is 31 (COBOLNET0801).
      *> 2002/2014: standard arithmetic (Annex E.2 6) + 21)) carries
      *>   34 digits, so 9999999999999999999999999999999 squared
      *>   (about 1.0E62) is representable as an intermediate, but its
      *>   GR3 integer has 62 digits - past the 31-digit literal
      *>   capacity - so the constant entry is refused (COBOLNET1547).
      *> 2023: the documented System.Decimal mode (DOC-A.1-29) cannot
      *>   hold the 31-digit operand at all (about 1.0E31 > 7.9E28):
      *>   refused by the §7.3.6.2 SR2 range, the same code.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1592N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K-SQ CONSTANT AS
             9999999999999999999999999999999
             * 9999999999999999999999999999999.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "COMPILED"
           STOP RUN.
