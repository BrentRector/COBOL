      *> reject-at: 2023
      *> NULL is NOT a figurative constant (8.3.3.6.2 lists Formats 1-7
      *> only); it is the predefined object reference / address
      *> (8.4.3.7 / 8.4.3.10) - class pointer, which 14.9.11.3 SR1
      *> excludes. It previously printed U+0000 (kb/Work PB148). The refusal
      *> is the one 8.4.3.10.3 SR1 context screen, COBOLNET2576 (kb/Work PB1427).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB148N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY NULL
           STOP RUN.
