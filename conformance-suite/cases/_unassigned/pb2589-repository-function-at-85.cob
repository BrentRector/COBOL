*>--- conformance-case
*> id: pb2589-repository-function-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2589 - the keyword-omitted intrinsic compiled at --std 85. The word
      *> FUNCTION may be omitted only for an intrinsic the REPOSITORY paragraph names
      *> (8.4.3.2.3 SR2), and the REPOSITORY paragraph, with its function specifier
      *> (12.3.8.2 intrinsic-function-specifier), is a COBOL-2002 introduction: the '85
      *> CONFIGURATION SECTION has no such paragraph. The declaration is what is refused
      *> below 2002 (COBOLNET0900), so no 85 program reaches SUM(1 2) with the word omitted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2589NEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION SUM INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 99.
       PROCEDURE DIVISION.
           COMPUTE N = SUM(1 2)
           DISPLAY N
           STOP RUN.
