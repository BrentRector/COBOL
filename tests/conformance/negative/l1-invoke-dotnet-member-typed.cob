      *> reject-at: 2002 2014 2023
      *> ISO §14.9.23.4 GR2 b) — "If the method to be invoked is a
      *> non-COBOL method, the behavior of the INVOKE statement is
      *> implementor-defined." (cite.py --check 14.9.23.4 OK, GR 2.)
      *> Annex A.1 item 101 requires that behavior be documented. The
      *> documented determination (docs/CONFORMANCE.md DOC-A.1-101): the
      *> members every .NET object carries (ToString, GetHashCode,
      *> Equals, ...) "are not methods of a COBOL object: on a typed
      *> receiver INVOKE T "ToString" is the compile-time error
      *> COBOLNET0825 (§14.9.23.3 SR4 b))".
      *> SR4 b) (cite.py --check 14.9.23.3 OK, SR 4): "If identifier-1
      *> references an object reference described with an
      *> object-class-name without the FACTORY phrase, literal-1 shall
      *> be the name of a method contained in the instance interface of
      *> that object-class-name." Class L1IV101K (and BASE, which it
      *> inherits) declares no method ToString, so the name is outside
      *> the instance interface and the INVOKE is REJECTED — the .NET
      *> System.Object.ToString every runtime object has is NOT reached.
      *> Expected: COBOLNET0825.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IV101B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1IV101K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE OBJECT REFERENCE L1IV101K.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE L1IV101K "NEW" RETURNING T
           INVOKE T "ToString"
           STOP RUN.
       END PROGRAM L1IV101B.
       IDENTIFICATION DIVISION.
       CLASS-ID. L1IV101K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. HELLO.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "HELLO".
       END METHOD HELLO.
       END OBJECT.
       END CLASS L1IV101K.
