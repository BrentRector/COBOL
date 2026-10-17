*>--- conformance-case
*> id: pb2727-special-names-channel-entry
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2727 - CHANNEL integer IS mnemonic-name is a vendor SPECIAL-NAMES entry that no
      *> ISO format prints: the 12.3.7.2 general format is a closed list (alphabet-name-clause, CLASS,
      *> CRT STATUS, CURRENCY SIGN, CURSOR, DECIMAL-POINT, the dynamic-length-structure-clause,
      *> LOCALE, the switch / device entry, symbolic-characters-clause, ORDER TABLE).  It used to be
      *> parsed into an arm nothing bound, so it compiled clean and did nothing at every edition; this
      *> implementation does not support it, so it is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N2727CH.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CHANNEL 1 IS TOP-OF-CH.
       PROCEDURE DIVISION.
           STOP RUN.
