*>--- conformance-case
*> id: l1-combined-datetime-one-argument
*> rules: [FMT-15.17.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.17.2 prints "FUNCTION COMBINED-DATETIME ( argument-1 argument-2 )". The parenthesised part is
      *> NOT bracketed and carries no ellipsis, so the format states exactly two arguments and neither is
      *> optional — §15.17.3 then gives each its own rule (r1 integer date form, r2 standard numeric time form),
      *> which a one-argument reference leaves with no argument-2 to govern. A reference that supplies one
      *> argument does not match the general format and shall be rejected.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NCDTAR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-C PIC 9(7)V9(5).
       PROCEDURE DIVISION.
           COMPUTE W-C = FUNCTION COMBINED-DATETIME(143951)
           STOP RUN.
       END PROGRAM L1NCDTAR.
