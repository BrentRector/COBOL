      *> PB1110 - ISO 14.6.8.3 rule 2: a binary32 receiver takes the
      *>   algebraic value "converted and transferred ... in a manner
      *>   consistent with the specifications of ISO/IEC 60559:2020" -
      *>   ONE correctly rounded conversion, never binary64 first.
      *> cite.py --check 14.6.8.3 "in a manner consistent with the
      *>   specifications of ISO/IEC 60559:2020" -> OK  14.6.8.3 2)
      *> Derivation: the binary32 neighbours of 1 are 1 and 1 + 2**-23 =
      *>   1.00000011920928955078125, and their midpoint is 1 + 2**-24 =
      *>   1.000000059604644775390625. 1.0000000596046448 is ABOVE that
      *>   midpoint by 2.5E-17 (less than half a binary64 ulp, so a
      *>   binary64 detour lands ON the midpoint and ties down to 1):
      *>   the single conversion is 1 + 2**-23. 1.0000000596046447 is
      *>   below the midpoint: 1. The exact midpoint ties to even: 1.
      *>   Negative values mirror.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1110.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B32 USAGE FLOAT-BINARY-32.
       01 FS USAGE FLOAT-SHORT.
       01 C1 USAGE COMP-1.
       01 R PIC 9V9(23).
       01 RN PIC S9V9(23) SIGN IS LEADING SEPARATE.
       01 ABOVE PIC 9V9(16) VALUE 1.0000000596046448.
       01 BELOW PIC 9V9(16) VALUE 1.0000000596046447.
       01 TIE PIC 9V9(24) VALUE 1.000000059604644775390625.
       01 NEG PIC S9V9(16) VALUE -1.0000000596046448.
       PROCEDURE DIVISION.
           MOVE 1.0000000596046448 TO B32
           MOVE B32 TO R
           DISPLAY "LITERAL " R
           MOVE ABOVE TO B32
           MOVE B32 TO R
           DISPLAY "ABOVE   " R
           MOVE ABOVE TO FS
           MOVE FS TO R
           DISPLAY "SHORT   " R
           MOVE ABOVE TO C1
           MOVE C1 TO R
           DISPLAY "COMP-1  " R
           MOVE 1.0000000596046448E0 TO B32
           MOVE B32 TO R
           DISPLAY "FLOATLIT" R
           MOVE BELOW TO B32
           MOVE B32 TO R
           DISPLAY "BELOW   " R
           MOVE TIE TO B32
           MOVE B32 TO R
           DISPLAY "TIE     " R
           MOVE NEG TO B32
           MOVE B32 TO RN
           DISPLAY "NEGATIVE" RN
           STOP RUN.
