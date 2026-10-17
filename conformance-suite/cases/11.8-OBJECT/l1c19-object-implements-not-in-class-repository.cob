      *>--- conformance-case
      *> id: l1c19-object-implements-not-in-class-repository
      *> rules: [SR-11.8.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.8.3 1) — the OBJECT paragraph's IMPLEMENTS names an
      *> interface that is not in the class definition's REPOSITORY.
      *> Rule: "Interface-name-1 shall be the name of an interface
      *> specified in the REPOSITORY paragraph of the containing class
      *> definition."
      *> cite.py: OK  §11.8.3 1)  (Syntax rules)
      *> L1C19M is a real interface of the compilation group and is
      *> declared in the PROGRAM's REPOSITORY, and the object defines a
      *> conforming SPEAK, so the only defect is that the class L1C19N's
      *> own REPOSITORY paragraph (it has none) does not specify L1C19M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C19L.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C19N
           INTERFACE L1C19M.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM L1C19L.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C19M.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE L1C19M.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C19N.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS L1C19M.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
           DISPLAY "OBJECT SPEAKS".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS L1C19N.
