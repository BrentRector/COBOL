      *>--- conformance-case
      *> id: l1c13-if-directive-mid-line
      *> rules: [SR-7.3.16.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.16.3 SR1 — ">>IF conditional-expression-1 shall begin
      *> on a new line and shall be specified entirely on that line."
      *>   cite.py: OK  §7.3.16.3 1)  (Syntax rules)
      *> The >>IF begins in the middle of a line, after DISPLAY "A",
      *> not on a new line.  (§7.3.3 SR2: "A compiler directive shall
      *> be preceded only by zero, one, or more space characters.")
      *> Starting ">>IF V = 1" on its own line makes it legal.
       >>DEFINE V AS 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C13G.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "A" >>IF V = 1
           DISPLAY "B"
       >>END-IF
           STOP RUN.
