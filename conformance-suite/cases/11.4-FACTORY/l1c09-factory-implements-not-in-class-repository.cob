      *>--- conformance-case
      *> id: l1c09-factory-implements-not-in-class-repository
      *> rules: [SR-11.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.4.3 SR1 — the FACTORY paragraph's IMPLEMENTS names an
      *> interface that is not in the class definition's REPOSITORY.
      *> Rule: "Interface-name-1 shall be the name of an interface
      *> specified in the REPOSITORY paragraph of the containing class
      *> definition."
      *> cite.py: OK  §11.4.3 1)  (Syntax rules)
      *> L1C09I is a real interface of the compilation group and is
      *> declared in the PROGRAM's REPOSITORY, and the factory defines
      *> a conforming SPEAK, so the only defect is that the class
      *> L1C09K's own REPOSITORY paragraph does not specify L1C09I.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C09K
           INTERFACE L1C09I.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE L1C09K "SPEAK".
           STOP RUN.
       END PROGRAM L1C09P.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C09I.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE L1C09I.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C09K.
       IDENTIFICATION DIVISION.
       FACTORY. IMPLEMENTS L1C09I.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
           DISPLAY "FACTORY SPEAKS".
       END METHOD SPEAK.
       END FACTORY.
       END CLASS L1C09K.
