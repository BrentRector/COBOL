*>--- conformance-case
*> id: pb132_bit_byref_aligned
*> rules: [SR-14.9.4.3-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB132 - ISO 14.9.4.3 SR6's LEGAL side: a byte-aligned bit item (B8 starts the record;
      *> BT's 8-bit stride keeps every occurrence aligned, so the constant-subscripted BT(2) is provable
      *> at bit 8) passes BY REFERENCE, and 14.2.3 GR8 makes the callee's store visible to the caller.
      *> Derived: the callee moves B"01011110" through the reference formal into each argument.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132BA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          02 B8 PIC 1(8) USAGE BIT.
       01 T.
          02 BT PIC 1(8) USAGE BIT OCCURS 3.
       PROCEDURE DIVISION.
       MAIN.
           MOVE B"10100001" TO B8
           MOVE B"11111111" TO BT(2)
           CALL "PB132BS" AS NESTED USING B8
           CALL "PB132BS" AS NESTED USING BT(2)
           DISPLAY "B8=" B8
           DISPLAY "BT2=" BT(2)
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132BS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION USING LB.
       P.
           MOVE B"01011110" TO LB
           GOBACK.
       END PROGRAM PB132BS.
       END PROGRAM PB132BA.
