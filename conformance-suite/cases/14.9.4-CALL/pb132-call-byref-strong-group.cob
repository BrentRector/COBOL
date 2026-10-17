*>--- conformance-case
*> id: pb132-call-byref-strong-group
*> rules: [SR-14.9.4.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR10 (FORMAT 1): a strongly-typed group item shall not pass BY REFERENCE - a
      *> prototype-less callee cannot preserve the type discipline (kb/Work PB132).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TPT TYPEDEF STRONG.
          02 F1 PIC 9(4).
       01 V TYPE TPT.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB" USING V
           STOP RUN.
