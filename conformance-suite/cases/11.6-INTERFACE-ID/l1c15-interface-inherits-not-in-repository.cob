      *>--- conformance-case
      *> id: l1c15-interface-inherits-not-in-repository
      *> rules: [SR-11.6.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.6.3 SR2 — an INHERITS interface-name-2 that is not in
      *> the REPOSITORY paragraph of the inheriting interface.
      *> Rule: "Interface-name-2 shall be the name of an interface
      *> specified in the REPOSITORY paragraph of this source element."
      *> cite.py: OK  §11.6.3 2)  (Syntax rules)
      *> L1C15PA is a real interface of the compilation group, but
      *> L1C15PB's REPOSITORY does not specify it (it specifies only
      *> L1C15PC), so the INHERITS FROM L1C15PA violates SR2. Nothing
      *> else is wrong: the INHERITS graph is acyclic and the program
      *> is otherwise legal. Diagnostic COBOLNET0840 (interface
      *> definition rules; the message names §11.6.3 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C15P.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM L1C15P.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C15PA.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE L1C15PA.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C15PC.
       PROCEDURE DIVISION.
       METHOD-ID. WALK.
       PROCEDURE DIVISION.
       END METHOD WALK.
       END INTERFACE L1C15PC.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C15PB INHERITS FROM L1C15PA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE L1C15PC.
       END INTERFACE L1C15PB.
