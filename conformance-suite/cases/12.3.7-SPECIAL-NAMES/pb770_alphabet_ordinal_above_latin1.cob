*>--- conformance-case
*> id: pb770_alphabet_ordinal_above_latin1
*> rules: [GR-12.3.7.4-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB770 - an ALPHABET literal phrase may name ANY character of the native alphanumeric character
      *> set, which is the 65,536 UTF-16 code units. `305 THRU 300` names U+0130 down to U+012B (ISO 12.3.7.4
      *> GR7 k1a: "The ordinal number of a character within the native character set, if the literal is
      *> numeric"), and k5 gives that descending run successive ASCENDING positions 0..5.
      *>
      *> Every OTHER character then follows in unchanged native relative order (k3), starting at position 6 -
      *> so the ASCII block is shifted by exactly six and keeps its own order. ORD reports position + 1
      *> (15.70.1 "The lowest ordinal position is 1"; 15.70.4 r1 for an alphanumeric argument):
      *>     '+' U+002B -> 6 + 43 = 49 -> 50        '0' U+0030 -> 6 + 48 = 54 -> 55
      *>     'A' U+0041 -> 6 + 65 = 71 -> 72        and 8.8.4.2.7 makes "0" < "+" FALSE.
      *>
      *> The builder used to mask each ordinal to 8 bits into a 256-wide table, so this clause silently became
      *> `'0' THRU '+'` and REVERSED them: 6, 1, 66, REVERSED. All four lines discriminate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB770ORD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX
           PROGRAM COLLATING SEQUENCE IS ALF.
       SPECIAL-NAMES.
           ALPHABET ALF IS 305 THRU 300.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N          PIC 9(6).
       01 C-PLUS     PIC X VALUE "+".
       01 C-ZERO     PIC X VALUE "0".
       PROCEDURE DIVISION.
           MOVE FUNCTION ORD("+") TO N
           DISPLAY "ORD-PLUS=" N
           MOVE FUNCTION ORD("0") TO N
           DISPLAY "ORD-ZERO=" N
           MOVE FUNCTION ORD("A") TO N
           DISPLAY "ORD-A=" N
           IF C-ZERO < C-PLUS
               DISPLAY "ORDER=REVERSED"
           ELSE
               DISPLAY "ORDER=NATIVE"
           END-IF
           STOP RUN.
