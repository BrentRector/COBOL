*>--- conformance-case
*> id: l1_usage_computational_identity
*> rules: [GR-13.18.60.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60.4 GR6 — USAGE COMPUTATIONAL: the implementor's radix
      *> and format, the alignment and representation including the algebraic
      *> sign, and the range of values the item may hold (Annex A.1 item 208;
      *> docs/CONFORMANCE.md DOC-A.1-208).
      *>
      *> THE RULE. §13.18.60.4 GR6: "The USAGE COMPUTATIONAL clause specifies
      *> that a radix and format specified by the implementor is used to
      *> represent a numeric item in the storage of the computer. Each
      *> implementor specifies the precise effect of the USAGE COMPUTATIONAL
      *> clause upon the alignment and representation of the data item in the
      *> storage of the computer, including the representation of any
      *> algebraic sign, and upon the range of values that the data item may
      *> hold."
      *> THREE determinations, one leg group each: FORMAT+ALIGNMENT (W),
      *> REPRESENTATION INCLUDING THE SIGN (BE, IDENT), RANGE (HOLD, SIZE,
      *> KEEP).
      *>
      *> THE DETERMINATION, docs/CONFORMANCE.md DOC-A.1-208: COMPUTATIONAL
      *> (and its COMP synonym) is "identical to USAGE BINARY — radix 2, the
      *> item 205 width ladder and byte order, the same PICTURE-digit-count
      *> truncation discipline". DOC-A.1-205 supplies what that inherits: a
      *> two's-complement integer of the item's UNSCALED value, MOST
      *> SIGNIFICANT BYTE FIRST, 2 bytes for 3-4 digits, no alignment padding.
      *> This golden's job is to MEASURE that identity rather than restate it,
      *> because "identical to BINARY" is precisely the kind of claim that can
      *> rot into two code paths that agree on the common cases.
      *> §13.18.60.3 SR6 supplies the second spelling: "COMP is an
      *> abbreviation for COMPUTATIONAL." The vendor synonym COMP-4 is
      *> deliberately NOT written here — it is not an ISO word, and a
      *> strict-conformance golden should not depend on one.
      *>
      *> W — width and alignment. All three spellings of the same picture
      *>     answer 2 (DOC-A.1-205: 3-4 digits -> 2 bytes). FUNCTION
      *>     BYTE-LENGTH is §15.14.1, "an integer equal to the length of the
      *>     argument in bytes".
      *> BE — representation INCLUDING THE SIGN, on the COMPUTATIONAL item.
      *>     -1234 in 16 bits two's complement is 65 536 - 1234 = 64 302 =
      *>     X"FB2E", most significant byte first, so the group image holds
      *>     X"FB" then X"2E". FUNCTION ORD is 1-based (§15.70.1, "The lowest
      *>     ordinal position is 1") and the native alphanumeric ordinal of a
      *>     character is its code unit + 1, so the two bytes read 251+1 = 252
      *>     and 46+1 = 47. A little-endian implementation answers "47 252"; a
      *>     sign-magnitude or packed representation answers neither. The
      *>     bytes are reached through a GROUP move to an alphanumeric item,
      *>     which §14.9.25.4 GR4 makes a byte transfer ("treated exactly as
      *>     if it were an alphanumeric to alphanumeric elementary move"),
      *>     never a numeric conversion.
      *> IDENT — the identity claim itself, and the leg no earlier golden
      *>     carries: the same picture and the same value at COMPUTATIONAL, at
      *>     COMP and at BINARY produce the SAME four bytes, neighbours
      *>     included. conformance:cases/13.18.60-USAGE/l1_usage_binary_storage pins the
      *>     BINARY side alone; this is the side that says COMPUTATIONAL did
      *>     not drift away from it.
      *> HOLD / SIZE / KEEP — the RANGE determination, which is the one that
      *>     distinguishes DOC-A.1-208's COMP from its COMP-5 neighbour.
      *>     COMP's range is the PICTURE's digit count, so PIC S9(4) holds
      *>     9 999 (HOLD) and 10 000 is out of range: §14.7.5 case 3 makes the
      *>     size error condition exist "if, after radix point alignment and
      *>     any applicable rounding specifications, the result of an
      *>     arithmetic statement is further from zero than permitted for the
      *>     associated resultant data item", so the ON SIZE ERROR phrase is
      *>     taken (SIZE). An implementation that gave COMPUTATIONAL its
      *>     CONTAINER's range — the documented COMP-5 discipline, a different
      *>     usage — would store 10 000 in those same two bytes and answer
      *>     SIZE=NO. KEEP is §14.7.5's own consequence for the taken phrase:
      *>     "the content of that resultant identifier is not changed from the
      *>     content that existed at the start of the execution of the
      *>     arithmetic statement", so RNG still holds the 9 999 put there
      *>     before the COMPUTE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1UCMP01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CU  PIC S9(4) USAGE COMPUTATIONAL.
       01 CA  PIC S9(4) USAGE COMP.
       01 BN  PIC S9(4) USAGE BINARY.
       01 GU.
          05 GU-A PIC X VALUE "a".
          05 GU-N PIC S9(4) USAGE COMPUTATIONAL VALUE -1234.
          05 GU-Z PIC X VALUE "z".
       01 GC.
          05 GC-A PIC X VALUE "a".
          05 GC-N PIC S9(4) USAGE COMP VALUE -1234.
          05 GC-Z PIC X VALUE "z".
       01 GB.
          05 GB-A PIC X VALUE "a".
          05 GB-N PIC S9(4) USAGE BINARY VALUE -1234.
          05 GB-Z PIC X VALUE "z".
       01 XU PIC X(4).
       01 XC PIC X(4).
       01 XB PIC X(4).
       01 IDF PIC X(3).
       01 HDF PIC X(3).
       01 SEF PIC X(3).
       01 KPF PIC X(3).
       01 RNG PIC S9(4) USAGE COMPUTATIONAL.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "W=" FUNCTION BYTE-LENGTH(CU)
               " " FUNCTION BYTE-LENGTH(CA)
               " " FUNCTION BYTE-LENGTH(BN)
           MOVE GU TO XU
           MOVE GC TO XC
           MOVE GB TO XB
           DISPLAY "BE=" FUNCTION ORD(XU(2:1))
               " " FUNCTION ORD(XU(3:1))
           MOVE "NO" TO IDF
           IF XU = XC AND XU = XB
               MOVE "YES" TO IDF
           END-IF
           DISPLAY "IDENT=" IDF
           MOVE 9999 TO RNG
           MOVE "NO" TO HDF
           IF RNG = 9999
               MOVE "YES" TO HDF
           END-IF
           DISPLAY "HOLD=" HDF
           MOVE "NO" TO SEF
           COMPUTE RNG = 9999 + 1
               ON SIZE ERROR MOVE "YES" TO SEF
           END-COMPUTE
           DISPLAY "SIZE=" SEF
           MOVE "NO" TO KPF
           IF RNG = 9999
               MOVE "YES" TO KPF
           END-IF
           DISPLAY "KEEP=" KPF
           STOP RUN.
