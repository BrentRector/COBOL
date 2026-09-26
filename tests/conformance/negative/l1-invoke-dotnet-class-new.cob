      *> reject-at: 2002 2014 2023
      *> ISO §14.9.23.4 GR2 b) — "If the method to be invoked is a
      *> non-COBOL method, the behavior of the INVOKE statement is
      *> implementor-defined." (cite.py --check 14.9.23.4 OK, GR 2.)
      *> Annex A.1 item 101 requires that behavior be documented. The
      *> documented determination (docs/CONFORMANCE.md DOC-A.1-101): "A
      *> non-COBOL method is never invoked: COBOL.NET provides no way to
      *> designate one ... A REPOSITORY class specifier whose
      *> externalized name is a .NET type (CLASS SB AS
      *> "System.Text.StringBuilder") does not bind to that type:
      *> INVOKE SB "NEW" ... is the compile-time error COBOLNET0823".
      *> This fixture is that sentence as a program: the documented
      *> behavior is a compile-time rejection, so the witness is a
      *> negative. Its twin l1-invoke-dotnet-member-typed pins the
      *> second documented arm (a .NET member name on a typed
      *> receiver, COBOLNET0825).
      *> Expected: COBOLNET0823.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IV101A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS SB AS "System.Text.StringBuilder".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE SB "NEW" RETURNING U
           STOP RUN.
