*>--- conformance-case
*> id: l1_usage_fixed_width_lengths
*> rules: [DOC-A.1-207]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60.4 GR13 + GR21 — USAGE BINARY-SHORT / BINARY-LONG / BINARY-DOUBLE and
      *> FLOAT-SHORT / FLOAT-LONG / FLOAT-EXTENDED: representation and length of the data item
      *> (Annex A.1 item 207; docs/CONFORMANCE.md DOC-A.1-207).
      *>
      *> THE RULES.
      *>   GR21: "The representation and length of a data item described with USAGE
      *>         BINARY-CHAR, BINARY-SHORT, BINARY-LONG, BINARY-DOUBLE, FLOAT-SHORT,
      *>         FLOAT-LONG, or FLOAT-EXTENDED is implementor-defined. The length and
      *>         alignment of a data item described with the SIGNED phrase shall be the same
      *>         as the length and alignment of a data item described with the UNSIGNED
      *>         phrase."
      *>   GR13: "The usages float-short, float-long and float-extended define signed numeric
      *>         data items ... The size and permitted range of values for these fields is
      *>         defined by the implementor. Any value that may be held in a data item of
      *>         usage float-short shall also be expressible in a data item of usage
      *>         float-long. Any value that may be held in a data item of usage float-long
      *>         shall also be expressible in a data item of usage float-extended."
      *>
      *> LEN - GR21's SECOND sentence is NOT implementor latitude, it is a requirement, and it
      *>     is the only part of this row a length table can get wrong silently: the eight
      *>     probes are the four fixed-width binary usages x SIGNED / UNSIGNED, and the pairs
      *>     shall be EQUAL. The values 1 / 2 / 4 / 8 are the implementor determination (the
      *>     C-family interchange widths, which is also what GR12's minimum ranges are sized
      *>     for: BINARY-CHAR must reach 255 unsigned = 8 bits, BINARY-SHORT 65 535 = 16 bits,
      *>     BINARY-LONG 2**32-1 = 32 bits, BINARY-DOUBLE 2**64-1 = 64 bits, so no NARROWER
      *>     width is admissible and these are the minimum widths GR12 leaves open).
      *> FLEN - the FLOAT determination: FLOAT-SHORT is IEEE 754 binary32 (4 bytes),
      *>     FLOAT-LONG and FLOAT-EXTENDED are both binary64 (8 bytes). GR13 constrains this
      *>     only by NESTING, which the equal-width choice satisfies trivially — see SL / LE.
      *> SL - GR13's first nesting sentence, probed at the binary32 integer boundary
      *>     (§13.18.60.4 NOTE 1: "The largest positive integer value with a nonzero trailing
      *>     digit that can be represented [in binary32] is +(2**24 - 1), or 16 777 213").
      *>     That value moved to FLOAT-LONG and compared back shall be EQUAL.
      *> LE - GR13's second nesting sentence, probed at the binary64 integer boundary (NOTE 2:
      *>     "+(2**53 -1), or 9 007 199 254 740 991"). Moved to FLOAT-EXTENDED and compared
      *>     back it shall be EQUAL — which is what forbids a FLOAT-EXTENDED narrower than
      *>     FLOAT-LONG. A binary32 FLOAT-EXTENDED answers NO here.
      *> BE - the REPRESENTATION half of GR21, for the float family: big-endian IEEE 754
      *>     interchange bytes. 1.5 in binary32 is sign 0, biased exponent 127 = X"7F",
      *>     significand 0.5 -> X"3FC00000"; most significant byte first the group image holds
      *>     X"3F" then X"C0". FUNCTION ORD is 1-based (§15.70.1), so 63+1 = 64 and
      *>     192+1 = 193. A little-endian implementation answers "1 1" (X"00" X"00"). The
      *>     bytes are read through a group move to an alphanumeric item, which §14.9.25.4 GR4
      *>     makes a byte transfer ("treated exactly as if it were an alphanumeric to
      *>     alphanumeric elementary move"). XF is X(5) = 4 (the FLOAT-SHORT leaf) + 1, so the
      *>     receiver size ALSO pins the float width independently of the BYTE-LENGTH fold.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1UFW01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BCS  USAGE BINARY-CHAR SIGNED.
       01 BCU  USAGE BINARY-CHAR UNSIGNED.
       01 BSS  USAGE BINARY-SHORT SIGNED.
       01 BSU  USAGE BINARY-SHORT UNSIGNED.
       01 BLS  USAGE BINARY-LONG SIGNED.
       01 BLU  USAGE BINARY-LONG UNSIGNED.
       01 BDS  USAGE BINARY-DOUBLE SIGNED.
       01 BDU  USAGE BINARY-DOUBLE UNSIGNED.
       01 FS   USAGE FLOAT-SHORT.
       01 FL   USAGE FLOAT-LONG.
       01 FE   USAGE FLOAT-EXTENDED.
       01 GF.
          05 GF-S USAGE FLOAT-SHORT VALUE 1.5.
          05 GF-P PIC X(1) VALUE "p".
       01 XF   PIC X(5).
       01 OK   PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "LEN=" FUNCTION BYTE-LENGTH(BCS)
               " " FUNCTION BYTE-LENGTH(BCU)
               " " FUNCTION BYTE-LENGTH(BSS)
               " " FUNCTION BYTE-LENGTH(BSU)
               " " FUNCTION BYTE-LENGTH(BLS)
               " " FUNCTION BYTE-LENGTH(BLU)
               " " FUNCTION BYTE-LENGTH(BDS)
               " " FUNCTION BYTE-LENGTH(BDU)
           DISPLAY "FLEN=" FUNCTION BYTE-LENGTH(FS)
               " " FUNCTION BYTE-LENGTH(FL)
               " " FUNCTION BYTE-LENGTH(FE)
           MOVE 16777213 TO FS
           MOVE FS TO FL
           MOVE "NO" TO OK
           IF FL = FS
               MOVE "YES" TO OK
           END-IF
           DISPLAY "SL=" OK
           MOVE 9007199254740991 TO FL
           MOVE FL TO FE
           MOVE "NO" TO OK
           IF FE = FL
               MOVE "YES" TO OK
           END-IF
           DISPLAY "LE=" OK
           MOVE GF TO XF
           DISPLAY "BE=" FUNCTION ORD(XF(1:1))
               " " FUNCTION ORD(XF(2:1))
           STOP RUN.
