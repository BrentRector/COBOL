      *> reject-at: 85 2002 2014 2023
      *> ISO 12.3.7.3 SR16 e2 (cite.py --check 12.3.7.3 "When the IN
      *> phrase is not specified, the ordinal position specified by
      *> integer-1 shall exist in the native alphanumeric character set"
      *> -> OK 12.3.7.3 16)). The native set has 65,536 characters, so
      *> ordinal 12345678901 does not exist in it.
      *>
      *> kb/Work PB1557's sibling: the binder read integer-1 with
      *> int.TryParse and SILENTLY SKIPPED the pair when that failed -
      *> an ordinal too long for a 32-bit int bound nothing and drew no
      *> diagnostic, so this program compiled. The .err pins the rule's
      *> own diagnostic on the clause, not a later "not defined".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1557SYMLONG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS S1 IS 12345678901.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER PIC X.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
