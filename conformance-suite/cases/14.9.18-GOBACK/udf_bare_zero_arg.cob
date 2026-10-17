*>--- conformance-case
*> id: udf_bare_zero_arg
*> rules: [GR-14.9.18.4-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R35 - 8.4.3.2.3 SR2 permits omitting the word FUNCTION for a REPOSITORY-declared
      *> USER function exactly as for a declared intrinsic, and a zero-argument function's reference
      *> is then a BARE NAME. PB7 fixed the intrinsic arm of the bare-name form; the UDF arm was
      *> never asked (the two-arm-dispatch shape a sixth time), so MOVE WITHOUTPAR TO X fell to the
      *> data path - compile-clean + runtime death before R30, "undefined" after. The differential's
      *> run_functions:4457 (GnuCOBOL's own testsuite) found it.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. R35FN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R-OUT PIC 9(4).
       PROCEDURE DIVISION RETURNING R-OUT.
           MOVE 42 TO R-OUT.
           GOBACK.
       END FUNCTION R35FN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. R35BARE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION R35FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9(4).
       PROCEDURE DIVISION.
           MOVE R35FN TO X.
           DISPLAY X.
           STOP RUN.
