*>--- conformance-case
*> id: pb400-evaluate-boolean-width-vs-truefalse
*> rules: [SR-14.9.13.3-6, SR-14.9.13.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.13.3 SR6 a)/b) reclassify a boolean operand to a boolean CONDITION
      *> only when it "results in one boolean character".  W-B2 is PICTURE 1(2), so its
      *> result is TWO boolean characters (§8.8.2 rule 10 — "a boolean value whose
      *> length shall be the number of boolean positions of the larger item referenced")
      *> and it stays boolean-expression-1.  §14.9.13.3 SR10 Table 15 leaves the
      *> boolean-expression × TRUE-or-FALSE cell BLANK, so this is a syntax-rule
      *> violation and therefore a compile-time diagnostic.
      *> It used to compile and die at run time with "boolean-literal condition"; the
      *> length test SR6 turns on was implemented nowhere (kb/Work PB400).  The
      *> ONE-character spelling of the same program is legal and is pinned by
      *> tests/conformance/2002/pb400_evaluate_boolean_sr6_pair.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB400NBOOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-B2 PIC 1(2) USAGE BIT VALUE B"01".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE W-B2
               WHEN TRUE
                   DISPLAY "HIT"
               WHEN FALSE
                   DISPLAY "MISS"
           END-EVALUATE.
           STOP RUN.
