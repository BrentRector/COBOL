*>--- conformance-case
*> id: l1c28_self_super_invoking_object
*> rules: [GR-8.4.3.8.4-1, GR-8.4.3.8.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.4.3.8.4 GR1/GR3 — SELF and SUPER reference the invoking
      *> object; SUPER resolution skips the containing class AND every
      *> subclass of it. Chain: A=L1C28CA <- B=L1C28CB <- C=L1C28CC.
      *> The root inherits the standard class BASE (§16.1), whose New creates
      *> every object below (kb/Work PB1548 made INHERITS FROM BASE bind).
      *> GR1: "SELF and SUPER both reference the object that was used
      *>   to invoke the method in which the reference to SELF or SUPER
      *>   appears."
      *>   cite.py: OK  §8.4.3.8.4 1)  (General rules)
      *> GR2 (the SELF legs): "If SELF is specified for a method
      *>   invocation, the method resolution is based upon the set of
      *>   methods defined for the runtime class of the object
      *>   referenced by SELF."
      *>   cite.py: OK  §8.4.3.8.4 2)  (General rules)
      *> GR3: "If SUPER is specified for a method invocation, ...
      *>   ignores all the methods defined in the class containing the
      *>   invocation and all the methods defined in any subclass of
      *>   that class."
      *>   cite.py: OK  §8.4.3.8.4 3)  (General rules)
      *> M is defined in A, overridden in B and in C; WHO is defined in
      *> A and overridden only in C; BUMP (A only) counts in A's
      *> per-object datum CNT. OC is an object of C, OB of B.
      *> Derivation, by output line:
      *>  1-2 INVOKE OC "M": C.M prints C.M, then SUPER "M" ignores C's
      *>      own M (GR3) and resolves in the superclass B: B.M.
      *>  3-4 INVOKE OC "X" (X in B only): B.X's SUPER "M" ignores B's
      *>      M AND C's M - C is a subclass of the containing class B
      *>      (GR3) - so A.M runs, although the runtime object is a C.
      *>      Inside A.M, SELF is still OC (GR1), so SELF "WHO"
      *>      resolves on runtime class C (GR2): C.WHO.
      *>  5-6 INVOKE OB "X": same path, but SELF is OB, class B has no
      *>      WHO of its own, so A.WHO.
      *>  7-9 INVOKE OC "Z": C.Z does SUPER "BUMP" then SELF "BUMP";
      *>      both reference OC (GR1), so A's per-object CNT goes 1, 2;
      *>      main then INVOKEs OC "BUMP" directly: 3. A SUPER that
      *>      referenced any other object would print CNT=1 twice.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C28M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C28CB
           CLASS L1C28CC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OC USAGE OBJECT REFERENCE L1C28CC.
       01 OB USAGE OBJECT REFERENCE L1C28CB.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE L1C28CC "NEW" RETURNING OC
           INVOKE L1C28CB "NEW" RETURNING OB
           INVOKE OC "M"
           INVOKE OC "X"
           INVOKE OB "X"
           INVOKE OC "Z"
           INVOKE OC "BUMP"
           STOP RUN.
       END PROGRAM L1C28M.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C28CA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       PROCEDURE DIVISION.
           DISPLAY "A.M"
           INVOKE SELF "WHO".
       END METHOD M.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
           DISPLAY "A.WHO".
       END METHOD WHO.
       METHOD-ID. BUMP.
       PROCEDURE DIVISION.
           ADD 1 TO CNT
           DISPLAY "CNT=" CNT.
       END METHOD BUMP.
       END OBJECT.
       END CLASS L1C28CA.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C28CB INHERITS FROM L1C28CA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C28CA.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "B.M".
       END METHOD M.
       METHOD-ID. X.
       PROCEDURE DIVISION.
           INVOKE SUPER "M".
       END METHOD X.
       END OBJECT.
       END CLASS L1C28CB.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C28CC INHERITS FROM L1C28CB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C28CB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "C.M"
           INVOKE SUPER "M".
       END METHOD M.
       METHOD-ID. WHO OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "C.WHO".
       END METHOD WHO.
       METHOD-ID. Z.
       PROCEDURE DIVISION.
           INVOKE SUPER "BUMP"
           INVOKE SELF "BUMP".
       END METHOD Z.
       END OBJECT.
       END CLASS L1C28CC.
