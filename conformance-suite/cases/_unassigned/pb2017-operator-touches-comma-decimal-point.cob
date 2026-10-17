      *>--- conformance-case
      *> id: pb2017-operator-touches-comma-decimal-point
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      * kb/Work PB2017 - under DECIMAL-POINT IS COMMA the comma of
      * *,1 is the decimal point of the literal ,1 (12.3.7.4 14) a),
      * 8.3.3.3.2 3)), so the rule broken is 8.7.1 at the * - no
      * space follows it.  The diagnostic used to be COBOLNET2631,
      * advising a space after the COMMA, which yields the conforming
      * but different program 1 *, 1 (1 times 1, not 1 times 0,1).
      * The Conformance test OperatorSpacingDiagnosticTests pins that
      * COBOLNET2631 is no longer reported.  COBOLNET2993.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2017N5.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(4)V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = 1 *,1.
           STOP RUN.
       END PROGRAM PB2017N5.
