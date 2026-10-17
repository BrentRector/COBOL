*>--- conformance-case
*> id: ca6_binary_operand_composite
*> rules: [SR-14.9.2.3-1, GR-14.9.2.4-1, GR-14.9.2.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> CA6 (CONFORMANCE-FIX-QUEUE): a data item of usage BINARY-CHAR/-SHORT/-LONG/-DOUBLE is EXCLUDED from the
      *> composite of operands (ISO 14.7.7 rule 2b) — the composite is then over the OTHER operands, still capped at
      *> 31. BL is BINARY-DOUBLE, so the composite is just F (11 + 13 = 24 digits) <= 31 -> the ADD is LEGAL.
      *> Pre-fix BL was superimposed (BinaryItem DOUBLE unsigned = 20 integer digits) -> a 33-digit composite -> a
      *> spurious COBOLNET0805 rejection. Runtime: F = 3 + 5 = 8 -> PIC 9(11)V9(13) -> 000000000080000000000000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BL USAGE BINARY-DOUBLE UNSIGNED VALUE 5.
       01 F  PIC 9(11)V9(13) VALUE 3.
       PROCEDURE DIVISION.
           ADD BL TO F.
           DISPLAY F.
           STOP RUN.
