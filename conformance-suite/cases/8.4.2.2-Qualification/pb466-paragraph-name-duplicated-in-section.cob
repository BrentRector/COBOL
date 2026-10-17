*>--- conformance-case
*> id: pb466-paragraph-name-duplicated-in-section
*> rules: [SR-8.4.2.2.3-7]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB466 - the OTHER uniqueness rule, and the one qualification cannot repair. ISO 8.4.2.2.3 SR7:
*> "If explicitly referenced, a paragraph-name shall not be duplicated within a section. A paragraph-name need
*> not be qualified when referred to from within the same section." Both halves are load-bearing here: the
*> second is the 8.4.2.2.1 rule-6 excuse that lets an unqualified DUP-P resolve inside S-ONE at all, and the
*> first is what makes THIS program non-conforming, because S-ONE declares DUP-P twice. No qualifier can
*> separate them - 8.4.2.2.2 format 4 offers a paragraph-name only its section-name, and both declarations
*> carry S-ONE - so the diagnostic must say "rename", not "qualify". That is why it is a distinct code from
*> the rule-1 ambiguity of pb466-ambiguous-procedure-name.
*> SR7 is conditioned on "if explicitly referenced", so the check belongs at the REFERENCE: a duplicated
*> paragraph-name that nothing mentions is conforming and compiles, which the positive witness
*> tests/conformance/85/pb466_procedure_name_uniqueness.cob measures. Rejected at all four editions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB466SR7.
       PROCEDURE DIVISION.
       S-ONE SECTION.
       P-REF.
           DISPLAY "START".
           GO TO DUP-P.
       DUP-P.
           DISPLAY "FIRST".
           STOP RUN.
       DUP-P.
           DISPLAY "SECOND".
           STOP RUN.
