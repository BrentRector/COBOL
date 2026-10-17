*>--- conformance-case
*> id: l1-numvalf-arity
*> rules: [FMT-15.69.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.69.2 general format — "FUNCTION NUMVAL-F ( argument-1 )". Both FUNCTION and NUMVAL-F are
      *> underlined (required words) and the format has exactly ONE argument position: no repetition brace,
      *> no bracketed second position, no keyword phrase. §15.3 opens with "The definition of a function
      *> specifies the number of arguments required, which may be zero, one, or more", and §15.69.2 is that
      *> definition — so a two-argument reference is not a NUMVAL-F reference at all. COBOLNET1504.
      *>
      *> The ADMIT side — the one-argument reference written exactly as §15.69.2 prints it, with values
      *> derived from §15.69.3 r1/r4 and §15.69.4 r1 — is
      *> conformance:2023/numvalf_decimal_comma_and_spaces. The pair is what pins the format: that one shows
      *> the printed shape is accepted and evaluated, this one shows the arity in the format is a bound and
      *> not decoration.
      *>
      *> reject-at omits 85: NUMVAL-F was introduced by ISO/IEC 1989:2002, so below 2002 the reference draws
      *> the introduction gate instead — a different rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NVFARITY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S1 PIC X(8) VALUE "1.5E+1".
       01 S2 PIC X(8) VALUE "2.5E+2".
       01 R  PIC 999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION NUMVAL-F(S1 S2) TO R.
           DISPLAY R.
           STOP RUN.
       END PROGRAM L1NVFARITY.
