      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB2518 - ISO/IEC 1989:2023 section 13.18.44.3 SR10: "The entries giving the new descriptions of the
      *> storage area shall follow the entries defining the area of data-name-2, without intervening entries that
      *> define new storage areas."
      *>   cite.py --check 13.18.44.3 "without intervening entries that define new storage areas" -> OK  10)
      *> The ROOT arm of the same rule: level-77 entries resolve data-name-2 among the roots of their section, not
      *> among a group's children, so the screen has to be asked there as well. COBOLNET2739.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2518N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 A PIC X(2).
       77 C PIC X.
       77 B REDEFINES A PIC X(2).
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
