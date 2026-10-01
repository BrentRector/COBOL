      *> reject-at: 2002 2014 2023
      *> 6.2.3.2 SR5: 'A floating literal continuation indicator shall
      *> not be specified on a line that contains a fixed literal
      *> continuation indicator.' The continuation line below carries
      *> the column-7 hyphen AND ends in the floating indicator
      *> (kb/Work PB1359).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1359SR5.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 X PIC X(80) VALUE "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA             
000600-    "BC"-
000700     "DE".
000800 PROCEDURE DIVISION.
000900 DISPLAY X.
001000     STOP RUN.
