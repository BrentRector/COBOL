*>--- conformance-case
*> id: pb770-alphabet-unsupported-code-name
*> rules: [SR-12.3.7.3-15, DOC-A.1-184]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 SR15: "The implementor shall specify the names supported for code-name-1 and code-name-2
      *> in the ALPHABET clause, if any." The set is not empty any more - owner decision kb/Work PB793 added
      *> ASCII and EBCDIC as code-name-1 - so this golden names a code-name that is NOT in it. FIELDATA is a
      *> real historical coded character set (the 6-bit UNIVAC/military code) and exactly the kind of spelling
      *> another vendor might support; here it is a source error, and the diagnostic names the supported set.
      *>
      *> An unsupported code-name used to be reinterpreted as a LITERAL PHRASE SPELLING OUT ITS OWN LETTERS:
      *> the alphabet's first four positions became F, I, E, L, and every downstream reference read that
      *> (kb/Work PB770 leg e). A bare word is not a literal at all (SR14 b2), so the literal-phrase reading was
      *> never available to fall back on. CONFORMANCE.md DOC-A.1-184 carries the statement of the supported set.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB770ALPHABETUNSUPPO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET A-FLD IS FIELDATA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER PIC X.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
