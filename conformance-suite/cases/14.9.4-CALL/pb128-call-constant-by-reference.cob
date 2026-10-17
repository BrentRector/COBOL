*>--- conformance-case
*> id: pb128-call-constant-by-reference
*> rules: [SR-14.9.4.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.15.3 SR2: no part of a CONSTANT RECORD may be a receiving operand. CALL BY REFERENCE
      *> arguments bypassed the receiving chokepoint via a direct resolve, so the callee could silently
      *> overwrite a structured constant (kb/Work PB128; batch 8's SR-14.9.4.3-5 find).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB128NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT RECORD.
          05 KA PIC X(4) VALUE "AAAA".
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB" USING BY REFERENCE KA
           STOP RUN.
