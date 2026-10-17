*>--- conformance-case
*> id: pb64t1-alphabet-locale-name-undeclared
*> rules: [SR-12.3.7.3-24]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 SR24: "locale-name-2 shall be a locale-name defined by the LOCALE clause" - the ALPHABET clause's
      *> named IS LOCALE form referencing a name no LOCALE clause declares: the same COBOLNET1664 every reference site
      *> draws, citing its own rule (kb/Work PB64 T1). The program otherwise binds (the alphabet falls back to the
      *> current-locale form) so the diagnostic is the only thing between this source and a compiled program.
      *> The ALPHABET clause is written FIRST, in the 12.3.7.2 format order, and the LOCALE clause after it declares
      *> only ES. Locale-names resolve after the whole paragraph is bound (kb/Work PB1558), so the .err pins
      *> "declared: ES": the refusal is SR24's, not an artifact of which clause came first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1AUND.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET SWE IS LOCALE SV
           LOCALE ES IS "es-ES".
       PROCEDURE DIVISION.
           STOP RUN.
