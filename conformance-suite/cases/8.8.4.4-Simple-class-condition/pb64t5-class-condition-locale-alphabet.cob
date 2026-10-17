*>--- conformance-case
*> id: pb64t5-class-condition-locale-alphabet
*> rules: [SR-8.8.4.4.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.4.3 SR2: "Alphabet-name-1 shall not reference an alphabet associated with a locale." An ALPHABET ...
      *> IS LOCALE defines a collating sequence, not a coded character set (12.3.7.4 GR7 Table 6), so a class condition
      *> cannot test membership of it - COBOLNET1669 locale-alphabet-not-a-charset (kb/Work PB64 T5) - for an alphabet
      *> of EITHER class: the alphanumeric LOC and the FOR NATIONAL NLOC (the one predicate, DataBinder.IsLocaleAlphabet).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T5SR2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET LOC IS LOCALE
           ALPHABET NLOC FOR NATIONAL IS LOCALE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "abc".
       01 N PIC N(3) VALUE N"abc".
       PROCEDURE DIVISION.
           IF X IS LOC DISPLAY "yes" END-IF.
           IF N IS NLOC DISPLAY "yes" END-IF.
           STOP RUN.
