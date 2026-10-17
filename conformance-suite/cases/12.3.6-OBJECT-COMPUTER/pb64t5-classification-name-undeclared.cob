*>--- conformance-case
*> id: pb64t5-classification-name-undeclared
*> rules: [SR-12.3.6.3-3, FMT-12.3.6.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.6.3 SR3: "Locale-name-1 and locale-name-2 shall be locale names defined in the SPECIAL-NAMES paragraph."
      *> TR is no LOCALE clause's name - the ONE undeclared-locale-name diagnostic (COBOLNET1664), citing this rule
      *> (kb/Work PB64 T5; DESIGN-locale-facility 7 rule a).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T5UND.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X CHARACTER CLASSIFICATION IS TR.
       PROCEDURE DIVISION.
           STOP RUN.
