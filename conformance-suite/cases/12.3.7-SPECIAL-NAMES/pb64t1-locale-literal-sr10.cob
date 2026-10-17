*>--- conformance-case
*> id: pb64t1-locale-literal-sr10
*> rules: [GR-12.3.7.4-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 SR10: "literal-4 shall be alphanumeric or national" - a numeric literal as the external
      *> identification draws the ONE SPECIAL-NAMES text-literal rule (COBOLNET0898) the ORDER TABLE clause's literal-9
      *> shares (kb/Work PB64 T1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1SR10.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS 42.
       PROCEDURE DIVISION.
           STOP RUN.
