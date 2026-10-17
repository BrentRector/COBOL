      *>--- conformance-case
      *> id: l1c22-format3-in-method
      *> rules: [SR-14.2.2-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.2.2 SR12 — an object-oriented format (Format 3)
      *> procedure division in a METHOD definition.
      *> RULE (14.2.2 SR12): "Format 3 may be specified in a source
      *> element if and only if that source element is a factory
      *> definition, an instance definition, or an interface definition,
      *> but not in a method definition."
      *> cite.py --check 14.2.2 "Format 3 may be specified in a source
      *>   element if and only if that source element is a factory
      *>   definition, an instance definition, or an interface
      *>   definition, but not in a method definition"
      *>   -> OK  §14.2.2 12)  (Syntax rules)
      *> Factory method OUTER of class L1C22R has a Format 3 procedure
      *> division (14.2.1: PROCEDURE DIVISION. { method-definition }
      *> ...): it holds method definition INNER. SR12 excludes Format 3
      *> in a method definition even though the method sits inside a
      *> factory definition. Formats 1 and 2 have no method-definition,
      *> so no other reading makes this legal; the rule is a
      *> general-format / pure-syntax rule, so a parse-level
      *> diagnostic is the expected witness. Moving INNER out beside
      *> OUTER (both directly in the factory) yields a valid class.
       IDENTIFICATION DIVISION.
       CLASS-ID. L1C22R.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. OUTER.
       PROCEDURE DIVISION.
       METHOD-ID. INNER.
       PROCEDURE DIVISION.
           DISPLAY "INNER".
       END METHOD INNER.
       END METHOD OUTER.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS L1C22R.
