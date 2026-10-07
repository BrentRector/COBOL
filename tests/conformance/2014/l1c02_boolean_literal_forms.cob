      *> ISO §8.3.3.4.2 FMT — all four boolean literal spellings, both
      *> delimiters of Format 1 and Format 2, with and without content.
      *> Rule (general format): Format 1 (boolean)
      *>   B"[boolean-character-1] ..." | B'[boolean-character-1] ...'
      *> Format 2 (hexadecimal-boolean)
      *>   BX"[hexadecimal-digit-1] ..." | BX'[hexadecimal-digit-1] ...'
      *> The character sequence is OPTIONAL under both delimiters.
      *> cite.py OK lines:
      *>  OK §8.3.3.4.2 (General format) Format 2 (hexadecimal-boolean)
      *>  OK §8.3.3.4.4 1) The separators that delimit the boolean
      *>     literal are not included in the value of the boolean
      *>     literal.
      *>  OK §8.3.3.4.4 3) If Boolean-character-1 is specified, the
      *>     value of a boolean literal is the value of the sequence of
      *>     occurrences of boolean-character-1.
      *>  OK §8.3.3.4.4 4) If boolean-character-1 is not specified, the
      *>     literal is a zero-length literal.
      *>  OK §8.3.3.4.4 5) Each hexadecimal digit has the following
      *>     boolean equivalent value ...
      *>  OK §8.3.3.4.4 6) ... replacing the leading BX" separator with
      *>     the B" separator.
      *>  OK §8.3.3.4.4 7) If hexadecimal-digit-1 is not specified, the
      *>     literal is a zero-length literal.
      *>  OK §8.3.3.1 The hexadecimal digits are the basic digits '0'
      *>     through '9' and the basic letters 'A' through 'F'.
      *>  OK §14.9.25.4 3) If literal-1 is a boolean zero-length literal
      *>     and the receiving operand is other than a dynamic-length
      *>     elementary item, literal-1 is treated as if it were
      *>     the figurative constant ZERO.
      *> Derivation (each receiver is sized exactly to its literal, so
      *> no padding/truncation rule is involved):
      *>  Q1 B"0101" GR1+GR3 value 0101      -> Q1=[0101]
      *>  A1 B'0110' same, apostrophe form    -> A1=[0110]
      *>  QX BX"A5" GR5/GR6 A=1010 5=0101     -> QX=[10100101]
      *>  AX BX'3C' 3=0011 C=1100             -> AX=[00111100]
      *>  VQ VALUE B'1'                       -> VQ=[1]
      *>  VX VALUE BX'F' F=1111               -> VX=[1111]
      *>  Z1..Z4: each field first set to B"1111", then MOVE of B"",
      *>  B'', BX"", BX'' - GR4/GR7 make each a zero-length boolean
      *>  literal, which §14.9.25.4 GR3 treats as figurative ZERO,
      *>  so each receiver becomes all boolean zeros -> [0000]. An
      *>  implementation rejecting an empty spelling fails to compile;
      *>  one treating it as anything but a zero-length literal
      *>  would leave 1111 or differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C02B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q1 PIC 1(4).
       01 A1 PIC 1(4).
       01 QX PIC 1(8).
       01 AX PIC 1(8).
       01 VQ PIC 1    VALUE B'1'.
       01 VX PIC 1(4) VALUE BX'F'.
       01 Z1 PIC 1(4).
       01 Z2 PIC 1(4).
       01 Z3 PIC 1(4).
       01 Z4 PIC 1(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE B"0101" TO Q1
           MOVE B'0110' TO A1
           MOVE BX"A5" TO QX
           MOVE BX'3C' TO AX
           DISPLAY "Q1=[" Q1 "]"
           DISPLAY "A1=[" A1 "]"
           DISPLAY "QX=[" QX "]"
           DISPLAY "AX=[" AX "]"
           DISPLAY "VQ=[" VQ "]"
           DISPLAY "VX=[" VX "]"
           MOVE B"1111" TO Z1 Z2 Z3 Z4
           MOVE B"" TO Z1
           MOVE B'' TO Z2
           MOVE BX"" TO Z3
           MOVE BX'' TO Z4
           DISPLAY "Z1=[" Z1 "]"
           DISPLAY "Z2=[" Z2 "]"
           DISPLAY "Z3=[" Z3 "]"
           DISPLAY "Z4=[" Z4 "]"
           STOP RUN.
