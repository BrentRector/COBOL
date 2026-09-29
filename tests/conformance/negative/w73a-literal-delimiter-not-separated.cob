      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1394 - ISO 8.3.5 rule 5: "The opening delimiter shall
      *>  be immediately preceded by a space, left parenthesis, or
      *>  opening pseudo-text delimiter. The closing delimiter shall be
      *>  immediately followed by one of the separators space, comma,
      *>  semicolon, period, right parenthesis, or closing pseudo-text
      *>  delimiter." DISPLAY "AB"N used to print AB005, MOVE"AB" and IF
      *>  "AB"=A compiled. COBOLNET2633 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGW73ALD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(3) VALUE 5.
       01 A PIC X(2).
       PROCEDURE DIVISION.
           DISPLAY "AB"N.
           MOVE"AB" TO A.
           IF "AB"=A DISPLAY "Y" END-IF.
           STOP RUN.
