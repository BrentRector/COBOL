      *>--- conformance-case
      *> id: pb1551-invoke-by-reference-view
      *> rules: [SR-8.4.3.5.3-2, SR-14.9.23.3-20]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1551 review R1-1 - ISO 14.9.23.3 SR20: "If
      *> identifier-3 does not reference an address-identifier,
      *> identifier-3 is a receiving operand" (cite.py: OK 14.9.23.3
      *> 20)), and ISO 8.4.3.5.3 SR2: "An object-view shall not be
      *> specified as a receiving operand." (cite.py: OK 8.4.3.5.3 2)) -
      *> COBOLNET2871. It was a parse error re-coded to 8.9 on the word
      *> AS (COBOLNET0901).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551IVM.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB1551IVM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1551IVC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 X PIC X(4).
       PROCEDURE DIVISION.
           INVOKE O "M" USING BY REFERENCE O AS UNIVERSAL.
       END METHOD M1.
       END OBJECT.
       END CLASS PB1551IVC.
