      *>--- conformance-case
      *> id: l1c13-if-directive-cce-split
      *> rules: [SR-7.3.16.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.16.3 SR1 — ">>IF conditional-expression-1 shall begin
      *> on a new line and shall be specified entirely on that line."
      *>   cite.py: OK  §7.3.16.3 1)  (Syntax rules)
      *> Here the conditional expression "V = 1" is split: ">>IF V ="
      *> on one line and "1" on the next, so it is NOT specified
      *> entirely on the >>IF line.  The >>IF line's operand is then
      *> "V =", which is not a constant-conditional-expression.
      *> Without the split (">>IF V = 1") the program is legal.
       >>DEFINE V AS 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C13E.
       PROCEDURE DIVISION.
       MAIN-P.
       >>IF V =
       1
           DISPLAY "A"
       >>END-IF
           STOP RUN.
