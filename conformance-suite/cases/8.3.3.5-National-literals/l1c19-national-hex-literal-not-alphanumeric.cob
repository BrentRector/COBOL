      *>--- conformance-case
      *> id: l1c19-national-hex-literal-not-alphanumeric
      *> rules: [GR-8.3.3.5.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.3.5.4 2) — a hex-national literal (format 2) is class
      *>   national
      *> "National literals are of the class and category national"
      *> OK  §8.3.3.5.4 2)  (General rules)
      *> 15.66.3 1) NATIONAL-OF: "Argument-1 shall be of class
      *>   alphabetic or
      *> class alphanumeric."  OK  §15.66.3 1)  (Argument rules)
      *> NATIONAL-OF(X"4142") is valid (hexadecimal alphanumeric
      *>   literal).
      *> NATIONAL-OF(NX"00410042") names a class-national argument (GR2
      *>   is for
      *> ALL FORMATS), so the source shall be rejected.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C19J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WN PIC N(2).
       PROCEDURE DIVISION.
           MOVE FUNCTION NATIONAL-OF(X"4142") TO WN
           MOVE FUNCTION NATIONAL-OF(NX"00410042") TO WN
           STOP RUN.
