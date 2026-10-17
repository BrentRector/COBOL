*>--- conformance-case
*> id: pb428-inline-invocation-no-returning
*> rules: [SR-14.9.25.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.4.3.4.1 makes an inline method invocation a REFERENCE to "a
      *> temporary data item returned from invocation of a method", and
      *> §8.4.3.4.4 GR1 b) describes that temporary entirely in terms of "the
      *> RETURNING parameter in the specification of the method identified by
      *> literal-1".  §14.8.3.1 states the same obligation from the other
      *> side: "A returning item is implicitly specified in the activating
      *> element when a function or inline method invocation is referenced."
      *> A method whose procedure division header declares no
      *> RETURNING item therefore has nothing for the identifier to reference
      *> — COBOLNET2139.  The INVOKE statement is the form for such a method,
      *> and the diagnostic says so.  kb/Work PB428.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB428N3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB428N3C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB428N3C.
       01 W   PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB428N3C "NEW" RETURNING OBJ.
           MOVE OBJ :: "DOIT" TO W.
           DISPLAY W.
           STOP RUN.
       END PROGRAM PB428N3.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB428N3C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. DOIT.
       PROCEDURE DIVISION.
       MAIN.
           CONTINUE.
       END METHOD DOIT.
       END OBJECT.
       END CLASS PB428N3C.
