*>--- conformance-case
*> id: pb247-subst-zero-flat
*> rules: [AR-15.87.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB247 - ISO 15.87.3 r3: "Neither argument-1 nor argument-2 shall be of zero length", on the
      *> table(ALL) bind path. The operand list here is [ "aXbXc", "", "Q", SB2(ALL) ]: the zero-length literal
      *> is pair 1's ARGUMENT-2 and its position is decided entirely before the enumeration is reached, so r3
      *> is decidable at compile time. BindSubstitute returned inside its `if (flat)` block, which sat ABOVE the
      *> odd-position r3 walk, so the whole rule went unenforced the moment any operand was an enumeration -
      *> the two-arm dispatch with one arm fixed. The walk now runs on both exits and advances by each operand's
      *> STATIC element count, stopping only where the run-time count makes the pair role genuinely unknowable.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGSUBSTZEROFLAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SB2-T.
          05 SB2 PIC X OCCURS 2.
       01 X3    PIC X(3) VALUE "ABC".
       01 A20   PIC X(20).
       PROCEDURE DIVISION.
           MOVE FUNCTION SUBSTITUTE(X3 "" "Q" SB2(ALL)) TO A20.
           STOP RUN.
