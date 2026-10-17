*>--- conformance-case
*> id: pb135_oo_environment
*> rules: [SR-14.9.11.3-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB135 - the OO environment plumbing whole. (1) 10.6.1 prints [options-paragraph] in
      *> every skeleton: the CLASS-level OPTIONS DEFAULT ROUNDED MODE NEAREST-EVEN reaches M1's bare
      *> ROUNDED (COMPUTE 5/2 -> 2, nearest-even; the old grammar made the paragraph a bare parse error),
      *> and M2's OWN paragraph overrides per 11.9.4 GR1 for ITS body only (5/2 -> 3, nearest-away) with
      *> no leak back. (2) The class SPECIAL-NAMES device mnemonic (SYSOUT IS MYCONSOLE) resolves inside
      *> a method - the old MnemonicRegistry walk never met the class configuration (COBOLNET0817 on
      *> legal source) while the SWITCH map from the SAME rule resolved. Derived: CLS-EVEN=0002,
      *> MTH-AWAY=0003.
       IDENTIFICATION DIVISION.
       CLASS-ID. CPB135 INHERITS FROM BASE.
       OPTIONS.
           DEFAULT ROUNDED MODE IS NEAREST-EVEN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSOUT IS MYCONSOLE.
       REPOSITORY.
           CLASS BASE
           CLASS CPB135.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 X PIC 9(4).
       PROCEDURE DIVISION.
       P.
           COMPUTE X ROUNDED = 5 / 2
           DISPLAY "CLS-EVEN=" X UPON MYCONSOLE
           GOBACK.
       END METHOD M1.
       IDENTIFICATION DIVISION.
       METHOD-ID. M2.
       OPTIONS.
           DEFAULT ROUNDED MODE IS NEAREST-AWAY-FROM-ZERO.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 X PIC 9(4).
       PROCEDURE DIVISION.
       P.
           COMPUTE X ROUNDED = 5 / 2
           DISPLAY "MTH-AWAY=" X
           GOBACK.
       END METHOD M2.
       END OBJECT.
       END CLASS CPB135.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB135M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB135.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CPB135.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CPB135 "NEW" RETURNING O
           INVOKE O "M1"
           INVOKE O "M2"
           STOP RUN.
       END PROGRAM PB135M.
