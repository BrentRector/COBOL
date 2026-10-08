      *> reject-at: 2002 2014 2023
      *> kb/Work PB2518 - ISO/IEC 1989:2023 section 13.18.44.3 SR10: "The entries giving the new descriptions of the
      *> storage area shall follow the entries defining the area of data-name-2, without intervening entries that
      *> define new storage areas."
      *>   cite.py --check 13.18.44.3 "without intervening entries that define new storage areas" -> OK  10)
      *> The REDEFINES rules are about the entries as WRITTEN, and a type declaration is written whether or not any
      *> entry says TYPE T: the declaration is where T's members are diagnosed. TYPEDEF is a 2002 clause, so the rule
      *> is asked from 2002. COBOLNET2739.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2518N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF.
          05 A PIC X(2).
          05 C PIC X.
          05 B REDEFINES A PIC X(2).
       01 U PIC X.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
