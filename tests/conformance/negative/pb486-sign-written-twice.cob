      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB486 - ISO 13.16.2 Format 1 prints [ sign-clause ] once with no
      *> ellipsis; 5.2.6.2 (cite.py OK): brackets "indicate that the syntax element
      *> contained within the brackets ... may be explicitly specified or that
      *> portion of the general format may be omitted". The second SIGN clause
      *> used to win silently and drop the SEPARATE character position.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB486SG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC S9(3) SIGN IS LEADING SEPARATE
                         SIGN IS TRAILING VALUE -12.
          05 F PIC X VALUE "|".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY G
           STOP RUN.
