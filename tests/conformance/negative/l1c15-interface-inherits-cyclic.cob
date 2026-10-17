      *> reject-at: 2002 2014 2023
      *> ISO §11.6.3 SR3 — an interface that inherits, INDIRECTLY, from
      *> itself (L1C15QA -> L1C15QB -> L1C15QA).
      *> Rule: "Interface-name-2 shall not inherit directly or
      *> indirectly from interface-name-1."
      *> cite.py: OK  §11.6.3 3)  (Syntax rules)
      *> Each INHERITS name is in the inheriting interface's own
      *> REPOSITORY (SR2 satisfied), and neither interface names itself,
      *> so only the indirect cycle is wrong. Diagnostic COBOLNET2791
      *> with the cycle message head.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C15Q.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM L1C15Q.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C15QA INHERITS FROM L1C15QB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE L1C15QB.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE L1C15QA.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C15QB INHERITS FROM L1C15QA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE L1C15QA.
       PROCEDURE DIVISION.
       METHOD-ID. WALK.
       PROCEDURE DIVISION.
       END METHOD WALK.
       END INTERFACE L1C15QB.
