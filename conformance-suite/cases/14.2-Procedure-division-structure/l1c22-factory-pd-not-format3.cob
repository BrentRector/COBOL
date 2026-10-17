      *>--- conformance-case
      *> id: l1c22-factory-pd-not-format3
      *> rules: [SR-14.2.2-13]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.2.2 SR13 — a statement directly in the procedure
      *> division of a FACTORY definition
      *> (not in a method definition).
      *> RULE (14.2.2 SR13): "A procedure division in an interface
      *> definition, an instance definition, or a factory definition
      *> that is not in a method definition shall be an object-oriented
      *> format procedure division."
      *> cite.py --check 14.2.2 "shall be an object-oriented format
      *>   procedure division" -> OK  §14.2.2 13)  (Syntax rules)
      *> The procedure division below is PROCEDURE DIVISION. followed by
      *> a sentence (DISPLAY), i.e. Format 2, not Format 3 (14.2.1:
      *> PROCEDURE DIVISION. [ { method-definition } ... ]). By
      *> construction any non-Format-3 procedure division here also
      *> breaks the converse rule 14.2.2 SR10 (cite.py --check 14.2.2
      *> "Formats 1 and 2 may be specified in a source element if and
      *> only if that source element is a function definition" -> OK
      *> §14.2.2 10)); the two rules state one restriction from both
      *> sides. Removing the DISPLAY sentence yields a valid unit. This
      *> is a general-format / pure-syntax rule, so a parse-level
      *> diagnostic is the expected witness.
       IDENTIFICATION DIVISION.
       CLASS-ID. L1C22T.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
           DISPLAY "FAC".
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS L1C22T.
