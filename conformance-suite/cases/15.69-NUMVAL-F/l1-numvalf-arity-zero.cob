*>--- conformance-case
*> id: l1-numvalf-arity-zero
*> rules: [FMT-15.69.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.69.2 general format — "FUNCTION NUMVAL-F ( argument-1 )". This is the DEFICIT direction
      *> of the same single argument position that conformance:negative/l1-numvalf-arity pins in the
      *> excess direction. The figure prints exactly ONE argument position: no repetition brace, no
      *> bracketed second position, and nothing making the one position optional. §15.3 opens "The
      *> definition of a function specifies the number of arguments required, which may be zero, one, or
      *> more" — and §15.69.2 IS that definition — so the count in the figure is a bound in BOTH
      *> directions, and a reference supplying none is not a NUMVAL-F reference. COBOLNET1504
      *> ("FUNCTION NUMVAL-F takes 1 argument(s); 0 given (ISO §15.3)").
      *>
      *> ⚠ ITS OWN FILE, DELIBERATELY. An empty argument list is the one arity shape that could in
      *> principle be refused by the PARSER rather than by the arity check; kept apart from
      *> l1-numvalf-arity so that a parse abort here can never mask the excess-direction fixture's
      *> diagnostic, and so each direction fails or passes on its own evidence.
      *>
      *> The ADMIT side of the format — the one-argument reference written exactly as §15.69.2 prints
      *> it — is conformance:2023/numvalf_decimal_comma_and_spaces.
      *>
      *> reject-at omits 85: NUMVAL-F was introduced by ISO/IEC 1989:2002, so below 2002 the reference
      *> draws the introduction gate instead — a different rule, and matching the .err there would be a
      *> false green.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NVFARIT0.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC 999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION NUMVAL-F ( ) TO R.
           DISPLAY R.
           STOP RUN.
       END PROGRAM L1NVFARIT0.
