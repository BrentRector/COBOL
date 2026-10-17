*>--- conformance-case
*> id: l1_usage_binary_storage
*> rules: [GR-13.18.60.4-4, DOC-A.1-205]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60.4 GR4 — USAGE BINARY: computer storage allocation, alignment and
      *> representation of data (Annex A.1 item 205; docs/CONFORMANCE.md DOC-A.1-205).
      *>
      *> THE RULE. §13.18.60.4 GR4: "The USAGE BINARY clause specifies that a radix of 2 is
      *> used to represent a numeric item in the storage of the computer. Each implementor
      *> specifies the precise effect of the USAGE BINARY clause upon the alignment and
      *> representation of the data item in the storage of the computer, including the
      *> representation of any algebraic sign. Sufficient computer storage shall be allocated
      *> by the implementor to contain the maximum range of values implied by the associated
      *> decimal picture character-string."
      *> Three obligations, one leg each: a WIDTH that is sufficient, an ALIGNMENT, and a
      *> REPRESENTATION including the sign.
      *>
      *> W - THE WIDTH LADDER, MEASURED AGAINST GR4'S OWN FLOOR rather than a convention.
      *>     The closing sentence makes a width WRONG if it cannot hold the picture's range:
      *>       S9(2)  -> max 99;                   1 byte holds -128..127        -> 1 suffices.
      *>       S9(4)  -> max 9 999;                1 byte cannot, 2 (32 767) can -> 2.
      *>       S9(9)  -> max 999 999 999;          2 cannot, 4 (2 147 483 647) can -> 4.
      *>       S9(18) -> max 10**18 - 1;           4 cannot, 8 (2**63 - 1 =
      *>                                           9 223 372 036 854 775 807) can -> 8.
      *>       S9(19) -> max 10**19 - 1;           8 CANNOT (10**19-1 > 2**63-1)  -> 16.
      *>     1/2/4/8/16 is therefore the SMALLEST byte ladder GR4 permits at these digit
      *>     counts, and FUNCTION BYTE-LENGTH (§15.14.1 "an integer equal to the length of the
      *>     argument in bytes") reports it. The S9(19) leg is the one that fails on the
      *>     common "8 bytes for everything from 10 digits up" shortcut.
      *> SIGN - the width is SIGN-INDEPENDENT: S9(4) and 9(4) both answer 2. GR4 leaves this
      *>     to the implementor; COBOL.NET's determination sets the boundary by the SIGNED
      *>     worst case so one picture never splits into two widths (the GR21 precedent, which
      *>     pins SIGNED and UNSIGNED to one length for the fixed-width binary usages).
      *> ALIGN - GR4's "alignment" obligation. COBOL.NET's determination is NO alignment: the
      *>     binary leaf begins at the next byte and no implicit FILLER is generated, so
      *>     X(1) + S9(4) BINARY + X(1) is 1 + 2 + 1 = 4. An implementation aligning the
      *>     binary item to a 2-byte boundary answers 6 (1 + pad 1 + 2 + 1 + pad 1).
      *> BE - GR4's "representation ... including the representation of any algebraic sign".
      *>     Two's complement, MOST SIGNIFICANT BYTE FIRST. -1234 in 16 bits is
      *>     65 536 - 1234 = 64 302 = X"FB2E", so the group image holds X"FB" then X"2E".
      *>     FUNCTION ORD is 1-based (§15.70.1 "The lowest ordinal position is 1") and the
      *>     native alphanumeric ordinal of a character is its code unit + 1, so the two bytes
      *>     read 251+1 = 252 and 46+1 = 47. A little-endian implementation answers "47 252".
      *>     The bytes are reached through a group move to an alphanumeric item, which
      *>     §14.9.25.4 GR4 makes a byte transfer ("treated exactly as if it were an
      *>     alphanumeric to alphanumeric elementary move"), never a numeric conversion.
      *> RT - the same bytes decode back to the same value and leave the neighbours intact.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1UBIN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B2   PIC S9(2)  USAGE BINARY.
       01 B4   PIC S9(4)  USAGE BINARY.
       01 B9   PIC S9(9)  USAGE BINARY.
       01 B18  PIC S9(18) USAGE BINARY.
       01 B19  PIC S9(19) USAGE BINARY.
       01 U4   PIC  9(4)  USAGE BINARY.
       01 GB.
          05 GB-A PIC X(1) VALUE "a".
          05 GB-N PIC S9(4) USAGE BINARY VALUE -1234.
          05 GB-Z PIC X(1) VALUE "z".
       01 GB2.
          05 GB2-A PIC X(1).
          05 GB2-N PIC S9(4) USAGE BINARY.
          05 GB2-Z PIC X(1).
       01 XB   PIC X(4).
       01 OK   PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "W=" FUNCTION BYTE-LENGTH(B2)
               " " FUNCTION BYTE-LENGTH(B4)
               " " FUNCTION BYTE-LENGTH(B9)
               " " FUNCTION BYTE-LENGTH(B18)
               " " FUNCTION BYTE-LENGTH(B19)
           DISPLAY "SIGN=" FUNCTION BYTE-LENGTH(B4)
               " " FUNCTION BYTE-LENGTH(U4)
           DISPLAY "ALIGN=" FUNCTION BYTE-LENGTH(GB)
           MOVE GB TO XB
           DISPLAY "BE=" FUNCTION ORD(XB(2:1))
               " " FUNCTION ORD(XB(3:1))
           MOVE XB TO GB2
           MOVE "NO" TO OK
           IF GB2-N = -1234 AND GB2-A = "a" AND GB2-Z = "z"
               MOVE "YES" TO OK
           END-IF
           DISPLAY "RT=" OK
           STOP RUN.
