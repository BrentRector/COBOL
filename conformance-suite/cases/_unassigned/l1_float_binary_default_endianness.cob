      *>--- conformance-case
      *> id: l1_float_binary_default_endianness
      *> rules: []
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 11.9.8.3 SR1 FLOAT-BINARY clause - the DEFAULT endianness
      *> of the standard binary floating-point usages (Annex A.1 item
      *> 48; docs/CONFORMANCE.md row 48).
      *> SR1: "When the FLOAT-BINARY clause is not specified, the
      *> implementor shall specify whether the HIGH-ORDER-LEFT phrase or
      *> the HIGH-ORDER-RIGHT phrase is implied for the data description
      *> entry of any data item described with a standard binary
      *> floating-point usage in which an endianness-phrase is not
      *> specified."  13.18.60.4 GR19c sends the question here: "For the
      *> standard binary floating-point usages, if neither the
      *> HIGH-ORDER-LEFT phrase nor the HIGH-ORDER-RIGHT phrase is
      *> specified, 11.9.8, FLOAT-BINARY clause, specifies which of
      *> these phrases is implied."  GR19a fixes what the answer MEANS:
      *> "The HIGH-ORDER-LEFT phrase specifies that the endianness of
      *> the data item is big-endian."  COBOL.NET's determination is
      *> HIGH-ORDER-LEFT.
      *>
      *> THIS IS THE OTHER ARM OF pb164_float_hor_image. That golden
      *> SPECIFIES the clause (FLOAT-BINARY DEFAULT IS HIGH-ORDER-RIGHT)
      *> and so measures 11.9.8.3 SR3. The SR1 arm - the one every
      *> program that writes no OPTIONS paragraph takes - IS already
      *> measured, but only for a FLOAT-BINARY-32: the D leg of
      *> pb174_float_item_endianness_default (05 B32-D USAGE
      *> FLOAT-BINARY-32 VALUE 1.5., no OPTIONS paragraph and no item
      *> endianness-phrase) answers D=[64 1], which IS SR1's implied
      *> phrase. THIS FILE ADDS the FLOAT-BINARY-64 default leg and a
      *> COMP-1 control under that same implied phrase, and it is the
      *> EXACT MIRROR of pb164_float_hor_image - same items, same
      *> values, same read positions - with the OPTIONS paragraph
      *> REMOVED, so every standard-usage expected value is the
      *> opposite one. A round-trip cannot see
      *> byte order (both lanes would reverse, self-inverse), so the
      *> discriminating legs read SINGLE BYTES of the group image with
      *> FUNCTION ORD (15.70 - the ordinal position, natively the byte
      *> value + 1).
      *> B32 1.5f is binary32 0x3FC00000; big-endian bytes 3F C0 00 00,
      *>     so byte 1 is ORD 64 and byte 4 is ORD 1. The
      *>     HIGH-ORDER-RIGHT twin answers 1 and 64 at the very same
      *>     positions.
      *> B64 -2.25 is binary64 0xC002000000000000; big-endian bytes C0
      *>     02 00 00 00 00 00 00 at image positions 5..12, so
      *>     ORD(5)=193, ORD(6)=3, ORD(11)=1, ORD(12)=1. The twin
      *>     answers 1, 1, 3, 193.
      *> C1  USAGE COMP-1 is an IMPLEMENTOR float usage, outside GR19c's
      *>     scope (13.18.60.4 GR13/GR21 pin it big-endian whatever the
      *>     FLOAT-BINARY clause says): 3F C0 00 00, so ORD(13)=64 and
      *>     ORD(16)=1 - the SAME answer the HIGH-ORDER-RIGHT twin gets,
      *>     which is what makes it the control leg. Under this default
      *>     the standard and implementor usages agree, which is why
      *>     records interchange.
      *> RT  the parse lane inverts under the same implied phrase.
      *> The FLOAT-DECIMAL half of item 48 (11.9.9.3 SR6) does not
      *> arise: the standard DECIMAL floating-point usages are
      *> documented processor-dependent non-support, rejected with
      *> COBOLNET1564 (pinned by
      *> FloatFamilyTests.ProcessorDependent_Rejected1564), so no
      *> default encoding or endianness is ever implied for one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FBDEF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GH.
          05 B32 USAGE FLOAT-BINARY-32 VALUE 1.5.
          05 B64 USAGE FLOAT-BINARY-64 VALUE -2.25.
          05 C1 USAGE COMP-1 VALUE 1.5.
       01 GH2.
          05 B32-2 USAGE FLOAT-BINARY-32.
          05 B64-2 USAGE FLOAT-BINARY-64.
          05 C1-2 USAGE COMP-1.
       01 XH PIC X(16).
       PROCEDURE DIVISION.
       MAIN.
           MOVE GH TO XH
           DISPLAY "B32=[" FUNCTION ORD(XH(1:1)) " "
               FUNCTION ORD(XH(4:1)) "]"
           DISPLAY "B64=[" FUNCTION ORD(XH(5:1)) " "
               FUNCTION ORD(XH(6:1)) " " FUNCTION ORD(XH(11:1)) " "
               FUNCTION ORD(XH(12:1)) "]"
           DISPLAY "C1 =[" FUNCTION ORD(XH(13:1)) " "
               FUNCTION ORD(XH(16:1)) "]"
           MOVE XH TO GH2
           DISPLAY "RT =[" B32-2 " " B64-2 " " C1-2 "]"
           STOP RUN.
