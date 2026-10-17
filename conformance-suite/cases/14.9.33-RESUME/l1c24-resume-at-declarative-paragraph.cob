      *>--- conformance-case
      *> id: l1c24-resume-at-declarative-paragraph
      *> rules: [SR-14.9.33.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.33.3 SR3 — RESUME AT a DECLARATIVE paragraph
      *> "Procedure-name-1 shall be a procedure-name in the
      *> nondeclarative portion of the function, method, or program."
      *>   cite.py --check 14.9.33.3 "Procedure-name-1 shall be a
      *>   procedure-name in the nondeclarative portion of the
      *>   function, method, or program."
      *>     -> OK §14.9.33.3 3) (Syntax rules)
      *> The RESUME is inside a (non-GLOBAL) declarative (SR1, SR2
      *> satisfied); its target HZ-Q is a paragraph of that same
      *> declarative section, i.e. in the DECLARATIVE portion. The only
      *> violation is SR3. RESUME first appears in ISO/IEC 1989:2002.
       >>TURN EC-USER-RZ CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24D.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HZ SECTION. USE AFTER EXCEPTION CONDITION EC-USER-RZ.
       HZ-P.
           RESUME AT HZ-Q.
       HZ-Q.
           DISPLAY "HZ-Q".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           RAISE EXCEPTION EC-USER-RZ.
           STOP RUN.
