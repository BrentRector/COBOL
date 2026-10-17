      *>--- conformance-case
      *> id: l1c18-method-prototype-final
      *> rules: [SR-11.7.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.7.3 SR8 — IS FINAL on a METHOD-ID inside an
      *> INTERFACE-ID (a method prototype).
      *> Rule: "The FINAL clause shall not be specified in a method
      *> prototype."
      *> cite.py: OK  §11.7.3 8)  (Syntax rules)
      *> "Method definitions within an interface definition define
      *> method prototypes."
      *> cite.py: OK  §9.3.7   (Method prototypes)
      *> The interface inherits nothing and PING is its only prototype;
      *> no OVERRIDE is written, so only SR8 is violated.  Diagnostic
      *> COBOLNET0840 with the prototype message head.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C18G.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM L1C18G.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C18GI.
       PROCEDURE DIVISION.
       METHOD-ID. PING IS FINAL.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE L1C18GI.
