*> reject-at: 2023
*> ISO/IEC 1989:2023 7.3.10.3 SR1 (cite.py --check 7.3.10.3 "The COBOL-WORDS directive may be specified only before the
*> first IDENTIFICATION DIVISION" -> OK 1)) with 7.3.10.4 GR2: after EQUATE "IDENTIFICATION" WITH "IDENT", IDENT DIVISION. is the
*> first identification division header, so the COBOL-WORDS directive below it is written after the region closed.
*> It was accepted: the boundary was read on the raw text. kb/Work PB1373.
       >>COBOL-WORDS EQUATE "IDENTIFICATION" WITH "IDENT"
       IDENT DIVISION.
       >>COBOL-WORDS RESERVE "ZQX"
       PROGRAM-ID. PB1373N02.
       PROCEDURE DIVISION.
           DISPLAY "SR1-MISSED".
           STOP RUN.
