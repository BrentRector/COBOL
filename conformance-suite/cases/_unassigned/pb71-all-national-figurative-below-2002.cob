*>--- conformance-case
*> id: pb71-all-national-figurative-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The figurative ALL N"…" carries a national literal-1 (ISO §8.3.3.6.3 SR2), a COBOL-2002 introduction
      *> (§8.3.3.5; the NATLIT posture) - below 2002 it is the introduction diagnostic, exactly as the bare literal
      *> is. kb/Work PB71: the figurative's literal was never gated (the version pass saw literals only under
      *> nonNumericLiteral); `ALL B"1"` compiled at --std 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB71NNAT85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AR PIC X(3).
       PROCEDURE DIVISION.
           MOVE ALL N"Q" TO AR.
           STOP RUN.
