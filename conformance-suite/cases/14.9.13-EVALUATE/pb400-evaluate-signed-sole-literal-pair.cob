*>--- conformance-case
*> id: pb400-evaluate-signed-sole-literal-pair
*> rules: [SR-14.9.13.3-10, GR-14.9.13.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.13.4 GR1: "If an operand of the EVALUATE statement consists of a
      *> single literal, that operand is treated as a literal, not as an expression",
      *> and §8.3.3.3.2 rule 2 puts an ADJACENT sign inside the literal ("If a sign is
      *> used, it shall appear as the leftmost character of the literal").  So `-5` is a
      *> LITERAL selection subject, `6` is a literal selection object, and §14.9.13.3
      *> SR10 Table 15 leaves the literal × literal cell BLANK — "a space indicates an
      *> invalid combination".
      *> The unsigned spelling of this program was already refused; the signed one was
      *> ACCEPTED, because the classifier routed the sole-literal question through the
      *> §8.8.4.7.3 SR2 "single data item not enclosed in parentheses" descent, for
      *> which a leading sign makes the operand compound (kb/Work PB400).  Two rules,
      *> opposite answers, one helper.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB400NLIT.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE -5
               WHEN 6
                   DISPLAY "HIT"
               WHEN OTHER
                   DISPLAY "MISS"
           END-EVALUATE.
           STOP RUN.
