      *> reject-at: 2002 2014 2023
      *> ISO 13.18.49.3 SR3: 'Neither the description of data-name-1 nor the
      *>   description of any data items subordinate to the subject of the
      *>   entry shall directly or indirectly contain a SAME AS clause that
      *>   references the subject of the entry or any group item to which this
      *>   entry is subordinate.' G (data-name-1 of H) holds Y SAME AS H. The
      *>   subordinate is written FIRST, the order that used to truncate the
      *>   cycle into a cascade COBOLNET0881 (kb/Work PB1302).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1302NSA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 X PIC X.
          05 Y SAME AS H.
       01 H SAME AS G.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "UNREACHABLE"
           STOP RUN.
