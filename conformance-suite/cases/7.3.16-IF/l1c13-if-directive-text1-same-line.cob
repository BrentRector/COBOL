      *>--- conformance-case
      *> id: l1c13-if-directive-text1-same-line
      *> rules: [SR-7.3.16.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.16.3 SR2 — "Text-1 shall begin on a new line."
      *>   cite.py: OK  §7.3.16.3 2)  (Syntax rules)
      *> Text-1 (DISPLAY "A") is written on the >>IF line itself,
      *> after the conditional expression, instead of beginning on a
      *> new line.  (§7.3.3 SR3 likewise lets a directive be followed
      *> only by spaces and an inline comment.)  Moving DISPLAY "A" to
      *> the next line makes the program legal.
       >>DEFINE V AS 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C13F.
       PROCEDURE DIVISION.
       MAIN-P.
       >>IF V = 1 DISPLAY "A"
       >>END-IF
           STOP RUN.
