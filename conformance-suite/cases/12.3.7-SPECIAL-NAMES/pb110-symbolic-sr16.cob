*>--- conformance-case
*> id: pb110-symbolic-sr16
*> rules: [SR-12.3.7.3-16, GR-12.3.7.4-11]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 SR16 c: "There shall be a one-to-one correspondence between occurrences of
      *> symbolic-character-1 and occurrences of integer-1" - two names against one integer is COBOLNET1670
      *> (kb/Work PB110; the clause was accepted-inert before it).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB110S16.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS S3 S4 ARE 7.
       PROCEDURE DIVISION.
           STOP RUN.
