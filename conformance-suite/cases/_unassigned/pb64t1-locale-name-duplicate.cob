*>--- conformance-case
*> id: pb64t1-locale-name-duplicate
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.1.1.1 - a user-defined word of one type is unique within its scope; the LOCALE clause is repeatable
      *> (12.3.7.2) so several locales may be declared, each under its own name. FR twice is COBOLNET1665 (kb/Work PB64
      *> T1; DESIGN-locale-facility 7 rule b).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1DUP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR"
           LOCALE FR IS "fr-CA".
       PROCEDURE DIVISION.
           STOP RUN.
