      *>--- conformance-case
      *> id: l1c22-format3-in-program
      *> rules: [SR-14.2.2-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.2.2 SR12 — an object-oriented format (Format 3)
      *> procedure division in a PROGRAM definition.
      *> RULE (14.2.2 SR12): "Format 3 may be specified in a source
      *> element if and only if that source element is a factory
      *> definition, an instance definition, or an interface definition,
      *> but not in a method definition."
      *> cite.py --check 14.2.2 "Format 3 may be specified in a source
      *>   element if and only if that source element is a factory
      *>   definition, an instance definition, or an interface
      *>   definition, but not in a method definition"
      *>   -> OK  §14.2.2 12)  (Syntax rules)
      *> Program L1C22Q's procedure division is Format 3 (14.2.1:
      *> PROCEDURE DIVISION. { method-definition } ...): it holds the
      *> method definition SHOWIT. A program definition is not one of
      *> the three source elements SR12 permits. Formats 1 and 2 have no
      *> method-definition, so no other reading makes this legal; the
      *> rule is a general-format / pure-syntax rule, so a parse-level
      *> diagnostic is the expected witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C22Q.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWIT.
       PROCEDURE DIVISION.
           DISPLAY "M".
       END METHOD SHOWIT.
       END PROGRAM L1C22Q.
