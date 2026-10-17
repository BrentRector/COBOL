*>--- conformance-case
*> id: pb1083-contained-program-intrinsic-name
*> rules: [SR-12.3.8.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §12.3.8.3 SR12 with §12.3.4 GR1 (kb/Work PB1083):
      *> the program-name MOD of a CONTAINED program is a user-defined word
      *> declared within the scope of its container's REPOSITORY paragraph,
      *> which identifies the intrinsic-function-name MOD (FUNCTION MOD
      *> INTRINSIC). It used to compile and run. COBOLNET1649.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1083CP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION MOD INTRINSIC.
       PROCEDURE DIVISION.
           CALL "MOD"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MOD.
       PROCEDURE DIVISION.
           DISPLAY "INNER"
           GOBACK.
       END PROGRAM MOD.
       END PROGRAM PB1083CP.
