      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1127 - ISO 14.9.22.3 SR5 (cite.py --check 14.9.22.3 "Identifier-2 shall reference an elementary numeric data item" -> OK 5):
      *> a USAGE INDEX item is class INDEX, never numeric (8.5.2.1 Table 2), so it is not an INSPECT TALLYING
      *> counter; its storage PICTURE (category numeric, zero digits) used to pass the screen and the tally
      *> was then stored through a zero-digit profile. Expected: COBOLNET0847 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1127IX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(5) VALUE "AAXAA".
       01 N USAGE INDEX.
       PROCEDURE DIVISION.
           INSPECT X TALLYING N FOR ALL "A"
           STOP RUN.
