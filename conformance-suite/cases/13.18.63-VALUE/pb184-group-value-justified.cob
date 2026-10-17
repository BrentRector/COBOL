*>--- conformance-case
*> id: pb184-group-value-justified
*> rules: [SR-13.18.63.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR14, FIRST CONJUNCT: "If a VALUE clause is specified at the group level, subordinate
      *> items within that group shall not be described with a JUSTIFIED or SYNCHRONIZED clause".
      *> SR14 is ONE sentence carrying TWO independent restrictions, and this repo's most reproducible defect
      *> shape is a two-arm rule with one arm implemented (9 recorded instances).  This fixture is the arm that
      *> is NOT PB184's symptom, so the pair cannot drift apart: neither conjunct existed before the screen and
      *> a JUSTIFIED subordinate compiled clean.
      *> Note the scope difference the code has to respect: this conjunct says "subordinate items within that
      *> group", full stop - it is NOT narrowed to alphanumeric group items the way the usage conjunct is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB184N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GJ VALUE "ABCD".
          05 J1 PIC X(2) JUSTIFIED RIGHT.
          05 J2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY GJ
           STOP RUN.
