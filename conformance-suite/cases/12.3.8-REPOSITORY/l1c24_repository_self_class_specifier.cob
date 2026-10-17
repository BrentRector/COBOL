      *>--- conformance-case
      *> id: l1c24_repository_self_class_specifier
      *> rules: [SR-12.3.8.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.8.3 SR5 — a class-specifier naming its own class
      *> definition is ignored
      *> RULE §12.3.8.3 SR5: "If the specified object-class-name-1 is
      *>   the name of the class definition in which this REPOSITORY
      *>   paragraph is specified, references to object-class-name-1
      *>   are to that class definition and this class-specifier is
      *>   ignored."
      *>   cite.py --check 12.3.8.3 "If the specified object-class-
      *>   name-1 is the name of the class definition in which this
      *>   REPOSITORY paragraph is specified" -> OK §12.3.8.3 5)
      *>   cite.py --check 12.3.8.4 "literal-1, literal-2, literal-3,
      *>   or literal-5 is the externalized name by which the class,
      *>   interface, function, or program, respectively, is known to
      *>   the operating environment" -> OK §12.3.8.4 2)
      *> SET-UP: class L1C24K's own REPOSITORY writes CLASS L1C24K AS
      *>   "L1C24L"; L1C24L is a real class in this compilation group
      *>   whose factory WHO prints a different line. Only factory
      *>   methods are used (no object creation).
      *> EXPECTED OUTPUT, DERIVED:
      *>   OTHER-CLASS-WHO  control: in the main program the specifier
      *>     CLASS L1C24X AS "L1C24L" is NOT self-named, so by GR2
      *>     L1C24X denotes the class known as L1C24L.
      *>   SELF-CLASS-WHO   inside L1C24K, INVOKE L1C24K "WHO" refers to
      *>     L1C24K itself (SR5): the AS "L1C24L" is ignored. A wrong
      *>     implementation would print OTHER-CLASS-WHO again.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C24K
           CLASS L1C24X AS "L1C24L".
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE L1C24X "WHO".
           INVOKE L1C24K "CALLWHO".
           STOP RUN.
       END PROGRAM L1C24M.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C24K.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C24K AS "L1C24L".
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
       WHO-P.
           DISPLAY "SELF-CLASS-WHO".
       END METHOD WHO.
       METHOD-ID. CALLWHO.
       PROCEDURE DIVISION.
       CALLWHO-P.
           INVOKE L1C24K "WHO".
       END METHOD CALLWHO.
       END FACTORY.
       END CLASS L1C24K.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C24L.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
       WHO-P.
           DISPLAY "OTHER-CLASS-WHO".
       END METHOD WHO.
       METHOD-ID. CALLWHO.
       PROCEDURE DIVISION.
       CALLWHO-P.
           DISPLAY "OTHER-CLASS-CALLWHO".
       END METHOD CALLWHO.
       END FACTORY.
       END CLASS L1C24L.
