      *> reject-at: 2002 2014
      *> Annex E.3.3 item 5 (kb/Work PB1402): the Adlam letters joined Annex B at COBOL 2023, so before 2023
      *> U+1E900 is no character of a COBOL word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1402N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 𞤀𞤁 PIC X VALUE "A".
       PROCEDURE DIVISION.
           DISPLAY 𞤀𞤁.
           STOP RUN.
