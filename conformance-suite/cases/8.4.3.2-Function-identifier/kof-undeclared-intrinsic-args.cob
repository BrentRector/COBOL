*>--- conformance-case
*> id: kof-undeclared-intrinsic-args
*> rules: [SR-8.4.3.2.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R22 - a bare catalogued-intrinsic name with an argument list and NO REPOSITORY
      *> declaration compiled with zero diagnostics and died at RUN time; ISO 8.4.3.2.3 SR2 allows the
      *> FUNCTION-keyword omission only for an intrinsic named in REPOSITORY. Only the reserved names
      *> (SIGN/SUM/RANDOM) drew COBOLNET1543 before; this pins the generic catalogued-name arm.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R22NEGA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC X(2).
       PROCEDURE DIVISION.
           MOVE UPPER-CASE("ab") TO WS-R.
           DISPLAY WS-R.
           STOP RUN.
