*>--- conformance-case
*> id: find_string_skip_domain
*> rules: [RV-15.37.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.37.4 rule 2 — "If argument-3 is specified, argument-3 represents the number of matches
      *> to ignore before determining the character position that shall be returned." — beyond the small
      *> values every earlier case uses, up to the 18-digit reach this program registers.
      *>
      *> H = "ABCABCABC", ND = "ABC": H(i:3) equals "ABC" at i = 1, 4, 7. Occurrences: {1, 4, 7}.
      *> r2 removes argument-3 of them from consideration BEFORE r1 determines which position is
      *> returned, so in the default direction argument-3 = k returns the (k+1)th occurrence, and under
      *> LAST it returns the (k+1)th occurrence counting from the end. When argument-3 is at least the
      *> number of occurrences, nothing is left to determine and rule 3 ("If no match is found, the
      *> function shall return zero") gives zero.
      *>
      *> An integer data item can carry values far beyond 32 bits — PIC 9(18) reaches
      *> 999999999999999999 — and r2 puts NO ceiling on argument-3, so the comparison against the
      *> occurrence count is a comparison of the FULL integer value. 4294967296 is 2**32, whose low 32
      *> bits are zero: under a 32-bit narrowing it would read as "ignore 0 matches" and return 1,
      *> which r2 and r3 forbid. BIG32 / BIGLIT / BIGC5 / BIG18 pin that across the argument shapes
      *> r3 admits (a display integer item, an integer literal, a binary integer item) and across the
      *> phrases r1 and r4 add. BIGC5 carries 2**32 + 1, not 2**32 + 3: a value whose low 32 bits are 3
      *> already exceeds the three occurrences, so it would return zero under a narrowed reading too and
      *> would pin the COMP-5 carrier without discriminating the narrowing. Low 32 bits of 1 would return
      *> the second occurrence, 4.
      *> SKIP0 and SKIP2 are the population guard: r2 with a value IN range still returns a position.
      *>
      *> The reach ABOVE 2**63 is deliberately not written here, and no claim is made about it.
      *> §15.37.3 r3 puts no ceiling on argument-3's VALUE, so such an argument-3 is a legal argument and
      *> r2/r3 would require zero for it; what this compiler does there has NOT been measured, so nothing
      *> in this file or its report asserts it. Whoever measures it adds the lines here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FSSKIP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H     PIC X(9) VALUE "ABCABCABC".
       01 ND    PIC X(3) VALUE "ABC".
       01 B32   PIC 9(18) VALUE 4294967296.
       01 B18   PIC 9(18) VALUE 999999999999999999.
       01 BC5   PIC 9(18) COMP-5 VALUE 4294967297.
       01 P     PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> In range: ignore 0 of {1,4,7} leaves the first at 1; ignore 2 leaves the third at 7.
           MOVE FUNCTION FIND-STRING(H ND 0) TO P.
           DISPLAY "SKIP0=" P.
           MOVE FUNCTION FIND-STRING(H ND 2) TO P.
           DISPLAY "SKIP2=" P.
      *> Exactly exhausted: ignoring 3 of 3 leaves no match, so rule 3 applies.
           MOVE FUNCTION FIND-STRING(H ND 3) TO P.
           DISPLAY "SKIP3=" P.
      *> The same two boundaries under LAST: ignoring the trailing 2 of {1,4,7} leaves 1; ignoring 3
      *> leaves nothing.
           MOVE FUNCTION FIND-STRING(H ND LAST 2) TO P.
           DISPLAY "LAST2=" P.
           MOVE FUNCTION FIND-STRING(H ND LAST 3) TO P.
           DISPLAY "LAST3=" P.
      *> Beyond 32 bits, as an integer DATA ITEM (r3's first species): 2**32 and the largest 18-digit
      *> value both exceed 3, so every occurrence is ignored and rule 3 gives zero.
           MOVE FUNCTION FIND-STRING(H ND B32) TO P.
           DISPLAY "BIG32=" P.
           MOVE FUNCTION FIND-STRING(H ND B18) TO P.
           DISPLAY "BIG18=" P.
      *> The same value carried in a BINARY integer item — r3 constrains the item's class, not its
      *> usage, so the answer may not depend on the carrier.
           MOVE FUNCTION FIND-STRING(H ND BC5) TO P.
           DISPLAY "BIGC5=" P.
      *> Beyond 32 bits as an integer LITERAL (r3's second species).
           MOVE FUNCTION FIND-STRING(H ND 4294967296) TO P.
           DISPLAY "BIGLIT=" P.
      *> And with the other phrases written, which change WHICH occurrence r1 would choose but not
      *> that r2 has already removed all of them.
           MOVE FUNCTION FIND-STRING(H ND LAST B32) TO P.
           DISPLAY "LASTBIG=" P.
           MOVE FUNCTION FIND-STRING(H ND B32 ANYCASE) TO P.
           DISPLAY "BIGANY=" P.
           STOP RUN.
       END PROGRAM L1FSSKIP.
