      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1246 - ISO 8.3.2.2.13 (cite.py OK): "A level-number, expressed as a one-digit or two-digit number"; 13.18.33.3 SR3 (cite.py OK): "A level-number in the
      *> range of 1 through 9 may be specified as 01 through 09."  `001` is neither, although its value is 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1246THREEDIGITS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       001  A PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY A.
           STOP RUN.
