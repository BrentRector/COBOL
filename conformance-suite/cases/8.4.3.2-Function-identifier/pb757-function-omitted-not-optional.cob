*>--- conformance-case
*> id: pb757-function-omitted-not-optional
*> rules: [SR-8.4.3.2.3-9, GR-14.2.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB757 -- ISO 8.4.3.2.3 SR9: "If the word OMITTED is specified, the OPTIONAL phrase shall be
      *> specified for the corresponding formal parameter."  PB757G's formal A carries no OPTIONAL phrase,
      *> so the OMITTED argument is a syntax-rule violation (COBOLNET2238, the user-defined-function twin of
      *> CALL's COBOLNET1685 and INVOKE's COBOLNET2237).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB757G.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC X(4).
       01 R PIC X.
       PROCEDURE DIVISION USING A RETURNING R.
           MOVE "P" TO R.
           GOBACK.
       END FUNCTION PB757G.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB757N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB757G.
       PROCEDURE DIVISION.
           DISPLAY FUNCTION PB757G(OMITTED).
           STOP RUN.
       END PROGRAM PB757N2.
