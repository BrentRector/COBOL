*>--- conformance-case
*> id: pb495_group_usage_standard_float
*> rules: [GR-13.18.60.4-12, GR-13.18.60.4-13, GR-13.18.60.4-14, GR-13.18.60.4-15, GR-13.18.60.4-21]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB495 — the COBOL-2014 leg of ISO §13.18.60.4 GR1 ("If the USAGE clause is specified or implied
      *> at a group level, it applies only to each elementary item in the group"), over the phrases 2014 added
      *> and over the SIGNED/UNSIGNED phrase GR21's second sentence governs.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   FLOAT-BINARY-32 = 4 bytes and FLOAT-BINARY-64 = 8. §13.18.60.4 GR14/GR15 PIN each to a named
      *>     ISO/IEC 60559:2020 basic binary interchange format — binary32 and binary64 — whose widths are 4 and
      *>     8 bytes by that standard, not by an implementor choice. The values pin the FORMAT, not just the
      *>     width: 16777217 = 2**24+1 is not representable in binary32 and rounds to 16777216, while binary64
      *>     holds it exactly, which is the sharpest single discriminator between the two.
      *>   BINARY-SHORT SIGNED and UNSIGNED are both 2 bytes — §13.18.60.4 GR21 sentence 2: "The length and
      *>     alignment of a data item described with the SIGNED phrase shall be the same as the length and
      *>     alignment of a data item described with the UNSIGNED phrase." The UNSIGNED phrase written on the
      *>     GROUP entry reaches the leaf with the usage, so US holds 65535 (GR12's unsigned range) where the
      *>     signed twin could not.
      *> Before PB495 every leaf below was a ZERO-LENGTH cell with no diagnostic: the group clause "specified
      *> nothing", where GR14/GR15 say the phrase specifies that the data item IS in that interchange format.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB495GR1F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G-B32 USAGE FLOAT-BINARY-32.
           05 A-B32.
       01 G-B64 USAGE FLOAT-BINARY-64.
           05 A-B64.
       01 G-SGN USAGE BINARY-SHORT SIGNED.
           05 A-SGN.
       01 G-UNS USAGE BINARY-SHORT UNSIGNED.
           05 A-UNS.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 16777217 TO A-B32
           MOVE 16777217 TO A-B64
           MOVE -32768 TO A-SGN
           MOVE 65535 TO A-UNS
           DISPLAY "W=" FUNCTION BYTE-LENGTH(A-B32)
               " " FUNCTION BYTE-LENGTH(A-B64)
               " " FUNCTION BYTE-LENGTH(A-SGN)
               " " FUNCTION BYTE-LENGTH(A-UNS)
           DISPLAY "F=" A-B32 " " A-B64
           DISPLAY "N=" A-SGN " " A-UNS
           STOP RUN.
