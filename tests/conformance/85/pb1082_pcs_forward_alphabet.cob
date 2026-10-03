      *> kb/Work PB1082 - ISO 12.3.6.3 SR1: "Alphabet-name-1 shall reference an alphabet that defines an alphanumeric
      *> collating sequence" (cite.py --check 12.3.6.3 "Alphabet-name-1 shall reference an alphabet that defines an
      *> alphanumeric collating sequence"). This is the positive side (the refusals are the negative corpus, pb1082-*):
      *> the OBJECT-COMPUTER paragraph PRECEDES SPECIAL-NAMES in source, so REV is a FORWARD reference - the screen must
      *> look for the alphabet after the SPECIAL-NAMES paragraph has bound, not at the clause.
      *> REV IS "Z" THRU "A" gives Z the first position and A the twenty-sixth (12.3.7.4 GR7 k5: a THROUGH run is
      *> assigned successive ascending positions, in either direction), so under the program collating sequence "A"
      *> is NOT less than "B" (A is at position 26, B at 25) - the native order would say it is.
      *>   REV-GT - "A" > "B" under REV.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1082POS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. ANY-COMPUTER PROGRAM COLLATING SEQUENCE IS REV.
       SPECIAL-NAMES.
           ALPHABET REV IS "Z" THRU "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CA PIC X VALUE "A".
       01  CB PIC X VALUE "B".
       PROCEDURE DIVISION.
           IF CA > CB DISPLAY "REV-GT" ELSE DISPLAY "REV-LE" END-IF
           STOP RUN.
