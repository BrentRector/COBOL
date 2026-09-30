*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.3.21.3 SR1 (cite.py --check 7.3.21.3 "A PROPAGATE directive shall not be specified within a compilation
*> unit." -> OK 1)): the directive below is inside PB1378N01, between its PROCEDURE DIVISION header and its first paragraph.
*> kb/Work PB1378. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1378N01.
       PROCEDURE DIVISION.
       >>PROPAGATE ON
       MAIN-PARA.
           DISPLAY "X".
           STOP RUN.
