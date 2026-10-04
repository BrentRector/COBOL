      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB917 - ISO 5.2.6.2 and 5.2.7 (cite.py OK on both): an element printed once, with no ellipsis, is written at most once.  13.16.2 Format 3 is
      *> `88 condition-name-1 value-clause .` - ONE value clause; a second VALUE clause on the level-88 entry repeats it.  (A single value clause may list several
      *> values, 13.18.63.2: `VALUE 1 2`.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB917CONDVALUETWICE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K PIC 9 VALUE 1.
           88  K-ODD VALUE 1 VALUE 3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF K-ODD DISPLAY "ODD".
           STOP RUN.
