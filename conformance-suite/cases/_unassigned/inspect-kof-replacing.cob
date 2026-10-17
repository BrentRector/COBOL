*>--- conformance-case
*> id: inspect-kof-replacing
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R15 - the keyword-omitted function reference is ONE reference with the keyword form
      *> (ISO 8.4.3.2.3 SR2), so the SR1 receiving bar applies to it identically: INSPECT REPLACING
      *> makes identifier-1 a receiving operand (14.9.22.4 GR7), which a function-identifier cannot be.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R15NEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           INSPECT EXCEPTION-STATUS REPLACING ALL "-" BY "*".
           STOP RUN.
