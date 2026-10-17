*>--- conformance-case
*> id: kof-undeclared-intrinsic-bare
*> rules: [SR-8.4.3.2.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R22 - the BARE-NAME shape of the same defect: a zero-argument catalogued intrinsic
      *> (CURRENT-DATE, 15.21.2's format is the name alone) written without FUNCTION and without a
      *> REPOSITORY declaration. ISO 8.4.3.2.3 SR2 requires the word FUNCTION here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R22NEGB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC X(21).
       PROCEDURE DIVISION.
           MOVE CURRENT-DATE TO WS-R.
           DISPLAY WS-R.
           STOP RUN.
