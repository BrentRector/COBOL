*>--- conformance-case
*> id: l1-formatted-date-one-argument
*> rules: [FMT-15.39.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.39.2 prints "FUNCTION FORMATTED-DATE ( argument-1 argument-2 )". The parenthesised part is NOT
      *> bracketed and carries no ellipsis, so the format states exactly two arguments and neither is optional —
      *> §15.39.3 r3 then makes argument-2 "a value in integer date form", the value the function exists to
      *> convert (§15.39.1). A one-argument reference does not match the general format and shall be rejected.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NFDTAR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-S PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION FORMATTED-DATE("YYYYMMDD") TO W-S
           STOP RUN.
       END PROGRAM L1NFDTAR.
