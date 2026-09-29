      *> PB1090 - ISO 12.3.7.2 prints "SPECIAL-NAMES. [clauses] ." and
      *>   12.3.7.3 SR31 lets ONE of the two separator periods go when no
      *>   clause is specified, so BOTH spellings of an empty paragraph
      *>   conform: "SPECIAL-NAMES." and "SPECIAL-NAMES. ." (SOURCE-
      *>   COMPUTER and OBJECT-COMPUTER already accepted theirs).
      *> cite.py --check 12.3.7.3 "One of the separator periods may be
      *>   omitted if none of the clauses in the SPECIAL-NAMES paragraph
      *>   is specified" -> OK  12.3.7.3 31)
      *> Derivation: the program compiles and displays its one line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1090.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES. .
       PROCEDURE DIVISION.
           DISPLAY "EMPTY-SPECIAL-NAMES-OK"
           STOP RUN.
