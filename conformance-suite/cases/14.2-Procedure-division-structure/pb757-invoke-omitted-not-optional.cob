*>--- conformance-case
*> id: pb757-invoke-omitted-not-optional
*> rules: [GR-14.2.3-3, SR-14.9.23.3-18]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB757 -- ISO 14.9.23.3 SR18: "If an OMITTED phrase is specified, an OPTIONAL phrase shall
      *> be specified for the corresponding formal parameter in the procedure division header."  REQ's
      *> formal X carries no OPTIONAL phrase, so the OMITTED argument is a syntax-rule violation
      *> (COBOLNET2237, the INVOKE twin of CALL's COBOLNET1685).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB757N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB757NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB757NC.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB757NC "NEW" RETURNING O.
           INVOKE O "REQ" USING OMITTED.
           STOP RUN.
       END PROGRAM PB757N1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB757NC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. REQ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC X(4).
       PROCEDURE DIVISION USING X.
           DISPLAY X.
       END METHOD REQ.
       END OBJECT.
       END CLASS PB757NC.
