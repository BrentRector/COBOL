*>--- conformance-case
*> id: pb862-unavailable-device-name
*> rules: [SR-12.3.7.3-8, DOC-A.1-189]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB862 - ISO 12.3.7.3 SR8: "The implementor shall specify the names that are available for
      *> switch-name-1, feature-name-1, and device-name-1."  COBOL.NET specifies them in ONE table
      *> (Binding/ImplementorNames.cs; docs/CONFORMANCE.md section 7, A.1 items 189/190/191) and CONSOEL is not
      *> one of them.  Before the table this entry compiled and registered PRT as the mnemonic of a device
      *> named CONSOEL; the typo surfaced only at the DISPLAY that used it, as a device "not capable of
      *> receiving data" - and a program that declared the entry but never used it compiled clean.  The
      *> refusal is now at the DECLARATION, by name: COBOLNET2241.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB862DEV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CONSOEL IS PRT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE".
           STOP RUN.
