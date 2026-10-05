      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1281 - ISO 13.18.44.3 SR6: "Data-name-2 shall not be qualified." cite.py: OK 13.18.44.3 6).
      *> The operand used to be resolved as its glued text AOFG, so this program compiled clean and B silently
      *> redefined the unrelated item AOFG (it printed WXYZ). Now the qualifier is refused by name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1020HPB1281QUAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC X(4) VALUE "ABCD".
          05 AOFG PIC X(4) VALUE "WXYZ".
          05 B REDEFINES A OF G PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY B.
           STOP RUN.
