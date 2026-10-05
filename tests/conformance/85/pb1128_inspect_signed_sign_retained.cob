      *> kb/Work PB1128 - ISO 14.9.22.4 GR4 d): "If identifier-1 is a signed numeric item, the original value of the
      *>   sign is retained upon completion of the INSPECT statement."  (cite.py --check 14.9.22.4 -> OK 4) d))
      *> A signed numeric identifier-1 whose digits are all replaced by zeros keeps its NEGATIVE sign: the item
      *> holds a negative zero. A value carrier (a native integer) has no negative zero, so a store that re-encodes
      *> the replaced digits from a VALUE turns "005-" into "000+" and fails this program.
      *> S1 - SIGN TRAILING SEPARATE holding -5: REPLACING ALL "5" BY "0" => digits 000, sign "-" retained => 000-.
      *> S2 - SIGN LEADING SEPARATE holding -5, the same replacement => -000.
      *> S3 - a POSITIVE item keeps its positive sign: +5 => 000+ (the sign is the original's, not the digits').
      *> S4 - a negative item whose result is non-zero keeps the sign: -57, REPLACING ALL "5" BY "1" => 017-.
      *> S5 - the default overpunched sign. The code of an overpunch is implementor-defined (13.18.52.4 GR4 leaves the
      *>      representation of the operational sign to the implementor); this compiler's is the EBCDIC-style zone,
      *>      "}" for a negative zero and "{" for a positive one, observed through the alphanumeric redefinition.
      *>      -5 with its 5 replaced by 0 stays NEGATIVE: 00}.
      *>
      *>   S1=000-
      *>   S2=-000
      *>   S3=000+
      *>   S4=017-
      *>   S5=00}
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1128SGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC S9(3) SIGN TRAILING SEPARATE VALUE -5.
       01 N2 PIC S9(3) SIGN LEADING SEPARATE VALUE -5.
       01 N3 PIC S9(3) SIGN TRAILING SEPARATE VALUE 5.
       01 N4 PIC S9(3) SIGN TRAILING SEPARATE VALUE -57.
       01 N5 PIC S9(3) VALUE -5.
       01 N5X REDEFINES N5 PIC X(3).
       PROCEDURE DIVISION.
           INSPECT N1 REPLACING ALL "5" BY "0"
           DISPLAY "S1=" N1
           INSPECT N2 REPLACING ALL "5" BY "0"
           DISPLAY "S2=" N2
           INSPECT N3 REPLACING ALL "5" BY "0"
           DISPLAY "S3=" N3
           INSPECT N4 REPLACING ALL "5" BY "1"
           DISPLAY "S4=" N4
           INSPECT N5 REPLACING ALL "5" BY "0"
           DISPLAY "S5=" N5X
           STOP RUN.
