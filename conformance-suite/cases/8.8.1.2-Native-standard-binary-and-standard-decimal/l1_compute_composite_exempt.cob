*>--- conformance-case
*> id: l1_compute_composite_exempt
*> rules: [GR-8.8.1.2-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.8.1.2 7 — "Arithmetic expressions allow the user to combine arithmetic operations without the
      *> restrictions on composite of operands and receiving data items."  DERIVED, not observed:
      *>   §14.7.7 2) a) restricts "ADD, DIVIDE, MULTIPLY, and SUBTRACT statements when native arithmetic is in
      *>   effect" — "the composite of operands shall not contain more than 31 digits".  COMPUTE is ABSENT from
      *>   that list of four verbs, and §14.7.7 2) defines the composite as "a hypothetical data item
      *>   resulting from the superimposition of specified operands in a statement aligned on their decimal
      *>   points" — that sentence is rule 2's OWN trailing paragraph, indented level with a) and b) and
      *>   defining the term for both, not part of b); cite.py --check answers "2) b)" only because its
      *>   ordinal tracker carries the last seen letter into an unlettered trailing paragraph.
      *>   §14.9.2.3 1) a) makes ADD format 1's composite "all of the operands in the statement", which
      *>   for `ADD a TO b` includes b — the RECEIVING data item.  One absence therefore covers BOTH halves of
      *>   rule 7, the operand half and the receiving-data-item half.  §14.9.8.3 imposes no composite rule on
      *>   COMPUTE, and §14.9.8.4 GR1 b routes arithmetic-expression-1 to §8.8.1, where rule 7 is the exemption
      *>   ("Otherwise, arithmetic-expression-1 is evaluated to produce an algebraic value according to the
      *>   specifications in 8.8.1, Arithmetic expressions."; cite.py --check reports that text as "1) a)"
      *>   because specs/ISO_COBOL.md:23167 has lost b)'s indentation — §14.9.8.4 2), "The value obtained
      *>   according to rule 1 is then stored", proves it is rule 1's sub-item b); reported with this batch).
      *> THE OPERANDS: L1CA is 18 integer digits and no fraction digits; L1CB is 1 integer digit and 17 fraction
      *> digits.  Superimposed aligned on their decimal points that is max(18,1) + max(0,17) = 35 digits, which
      *> exceeds 31.  So `ADD L1CA TO L1CB` is ILLEGAL — conformance:negative/l1-add-composite-over-31 is the
      *> ARMED COMPLEMENT that pins the rejection over these very pictures — while every COMPUTE below combines
      *> the same operands legally.  Without that complement a green run here would be no evidence at all.
      *> NATIVE arithmetic is in effect (no OPTIONS paragraph, §8.8.1.3), which is the only mode in which
      *> §14.7.7 2) applies and therefore the only mode in which rule 7's exemption has any bite.
      *> THE VALUES avoid §8.8.1.3's implementor-defined intermediate entirely: 7 and 0.5 are exact, every
      *> operation below is an addition, subtraction or multiplication of exact multiples of one half, and no
      *> intermediate needs more than three significant digits or a division.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CMP01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1CA PIC 9(18)   VALUE 7.
       01 L1CB PIC 9V9(17) VALUE 0.5.
       01 L1CR PIC 9(4)V99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
      *> Two operands, 35-digit composite, one operator.
           COMPUTE L1CR = L1CA + L1CB.
           DISPLAY "SUM    =" L1CR.
      *> "Combine arithmetic operations" — multiplication and addition and subtraction in one expression over
      *> the same 35-digit-composite operands.  §8.8.1.2 2) puts multiplication above addition/subtraction, so
      *> this is (L1CA * 2) - (L1CB * 4) + 1 = 14 - 2 + 1.
           COMPUTE L1CR = L1CA * 2 - L1CB * 4 + 1.
           DISPLAY "COMBINE=" L1CR.
      *> §8.8.1.2 1) — parenthesized grouping over the same operands: (7 - 1) * (0.5 + 0.5).
           COMPUTE L1CR = (L1CA - 1) * (L1CB + L1CB).
           DISPLAY "PARENS =" L1CR.
           STOP RUN.
