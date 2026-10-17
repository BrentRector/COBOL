*>--- conformance-case
*> id: l1_usage_packed_decimal_storage
*> rules: [DOC-A.1-215]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60.4 GR11 — USAGE PACKED-DECIMAL: computer storage allocation, alignment
      *> and representation of data (Annex A.1 item 215; docs/CONFORMANCE.md DOC-A.1-215).
      *>
      *> THE RULE. §13.18.60.4 GR11: "The USAGE PACKED-DECIMAL clause specifies that a radix
      *> of 10 is used to represent a numeric item in the storage of the computer.
      *> Furthermore, this clause specifies that each digit position shall occupy the minimum
      *> possible configuration in computer storage. Each implementor specifies the precise
      *> effect of the USAGE PACKED-DECIMAL clause upon the alignment and representation of
      *> the data item in the storage of the computer, including the representation of any
      *> algebraic sign. Sufficient computer storage shall be allocated by the implementor to
      *> contain the maximum range of values implied by the associated decimal picture
      *> character-string. If the WITH NO SIGN phrase is specified the representation of the
      *> data item in the storage of the computer reserves no storage for representing any
      *> sign value."
      *>
      *> W - THE WIDTH, derived from GR11's own two constraints. "Each digit position shall
      *>     occupy the MINIMUM POSSIBLE CONFIGURATION" makes a radix-10 digit a 4-bit nibble
      *>     (ten values need four bits, and a byte holds two of them); "sufficient computer
      *>     storage ... to contain the maximum range" then fixes the byte count:
      *>       d digits + one sign nibble  -> ceil((d+1)/2) = d/2 + 1 bytes (integer divide)
      *>       d digits, WITH NO SIGN      -> ceil(d/2) bytes, since the phrase "reserves no
      *>                                     storage for representing any sign value".
      *>     S9(3)=2, S9(4)=3, S9(5)=3, 9(4)=3, 9(3) NO SIGN=2, 9(4) NO SIGN=2.
      *>     Note the CONTROL in that list: at 3 digits the two forms occupy the SAME 2 bytes
      *>     (123 is X"12 3C" signed and X"01 23" unsigned-no-sign), so only the EVEN digit
      *>     counts discriminate the phrase — a width table that ignored WITH NO SIGN would
      *>     still answer 2 for 9(3) and is caught only by the 9(4) pair, 3 against 2.
      *> DIG - the digit nibbles themselves: BCD, TWO DIGITS PER BYTE, most significant first,
      *>     left-padded with a zero nibble when the digit count is even. 1234 is X"01" X"23"
      *>     ahead of its sign nibble. FUNCTION ORD is 1-based (§15.70.1 "The lowest ordinal
      *>     position is 1") so the bytes read 1+1 = 2 and 35+1 = 36. A digit-per-byte (zoned)
      *>     representation answers 49 50 here; a low-order-first one answers 53 36.
      *> SGN - GR11's "representation of any algebraic sign": a TRAILING sign nibble in the low
      *>     half of the last byte. X"D" negative, X"C" positive, X"F" for an item with no
      *>     operational sign — the last digit byte is therefore X"4D" / X"4C" / X"4F", i.e.
      *>     ORD 78 / 77 / 80. Three legs so the sign is measured, not assumed present.
      *> NOSGN - the WITH NO SIGN branch: every nibble is a digit and there is no sign nibble
      *>     at all, so 1234 is exactly X"12" X"34" -> ORD 19 and 53. If the phrase were
      *>     ignored the two bytes would be X"01" X"23" (ORD 2 and 36) plus a third.
      *> ALIGN - GR11's "alignment" obligation. COBOL.NET's determination is NO alignment: the
      *>     packed leaf starts at the next byte with no implicit FILLER, so X(1) + S9(4)
      *>     PACKED-DECIMAL is 1 + 3 = 4. An implementation aligning it would answer more.
      *> RT - the same bytes decode back to the same value, sign included, and leave the
      *>     neighbouring character leaf intact. The bytes are reached by a group move to an
      *>     alphanumeric item, which §14.9.25.4 GR4 makes a byte transfer ("treated exactly
      *>     as if it were an alphanumeric to alphanumeric elementary move").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1UPKD01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P3   PIC S9(3) USAGE PACKED-DECIMAL.
       01 P4   PIC S9(4) USAGE PACKED-DECIMAL.
       01 P5   PIC S9(5) USAGE PACKED-DECIMAL.
       01 U4   PIC  9(4) USAGE PACKED-DECIMAL.
       01 N3   PIC  9(3) USAGE PACKED-DECIMAL WITH NO SIGN.
       01 N4   PIC  9(4) USAGE PACKED-DECIMAL WITH NO SIGN.
       01 GP.
          05 GP-NEG PIC S9(4) USAGE PACKED-DECIMAL VALUE -1234.
          05 GP-POS PIC S9(4) USAGE PACKED-DECIMAL VALUE 1234.
          05 GP-UNS PIC  9(4) USAGE PACKED-DECIMAL VALUE 1234.
          05 GP-NOS PIC  9(4) USAGE PACKED-DECIMAL WITH NO SIGN
                    VALUE 1234.
       01 XP   PIC X(11).
       01 GA.
          05 GA-A PIC X(1) VALUE "a".
          05 GA-N PIC S9(4) USAGE PACKED-DECIMAL VALUE -1234.
       01 GA2.
          05 GA2-A PIC X(1).
          05 GA2-N PIC S9(4) USAGE PACKED-DECIMAL.
       01 XA   PIC X(4).
       01 OK   PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "W=" FUNCTION BYTE-LENGTH(P3)
               " " FUNCTION BYTE-LENGTH(P4)
               " " FUNCTION BYTE-LENGTH(P5)
               " " FUNCTION BYTE-LENGTH(U4)
               " " FUNCTION BYTE-LENGTH(N3)
               " " FUNCTION BYTE-LENGTH(N4)
           MOVE GP TO XP
           DISPLAY "DIG=" FUNCTION ORD(XP(1:1))
               " " FUNCTION ORD(XP(2:1))
           DISPLAY "SGN=" FUNCTION ORD(XP(3:1))
               " " FUNCTION ORD(XP(6:1))
               " " FUNCTION ORD(XP(9:1))
           DISPLAY "NOSGN=" FUNCTION ORD(XP(10:1))
               " " FUNCTION ORD(XP(11:1))
           DISPLAY "ALIGN=" FUNCTION BYTE-LENGTH(GA)
           MOVE GA TO XA
           MOVE XA TO GA2
           MOVE "NO" TO OK
           IF GA2-N = -1234 AND GA2-A = "a"
               MOVE "YES" TO OK
           END-IF
           DISPLAY "RT=" OK
           STOP RUN.
