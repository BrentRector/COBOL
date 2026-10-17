*>--- conformance-case
*> id: oo_self
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §11.7 / §8.4.3.8 / §14.9.23 — a MULTI-METHOD class + INVOKE SELF. COUNTER has TWO instance
      *> methods sharing one per-instance object datum N: BUMP increments N; DRIVE calls its sibling BUMP twice via
      *> INVOKE SELF, then DISPLAYs N. Exercises the keystone: (a) two METHOD-ID bodies on one .NET class, each its
      *> own method + dispatch range (both reuse a MAIN paragraph), (b) shared per-instance typed N, (c) INVOKE SELF
      *> → callvirt on `this`.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOSELF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS COUNTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C USAGE OBJECT REFERENCE COUNTER.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE COUNTER "NEW" RETURNING C.
           INVOKE C "DRIVE".
           STOP RUN.
       END PROGRAM OOSELF.

       IDENTIFICATION DIVISION.
       CLASS-ID. COUNTER INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. BUMP.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO N.
       END METHOD BUMP.
       METHOD-ID. DRIVE.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE SELF "BUMP".
           INVOKE SELF "BUMP".
           DISPLAY "N=" N.
       END METHOD DRIVE.
       END OBJECT.
       END CLASS COUNTER.
