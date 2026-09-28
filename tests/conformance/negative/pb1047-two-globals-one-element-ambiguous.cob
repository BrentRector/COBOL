      *> reject-at: 85 2002 2014 2023
      *> ISO §8.4.6.2.1 3) b) 1. narrows a duplicated name to ONE source
      *> element - the nearest that declares it - and no further: two
      *> global X items in that one element remain ambiguous, so the
      *> bare reference is refused (COBOLNET1639) exactly as two local
      *> ones would be (kb/Work PB1047; positive twin:
      *> 85/pb1047_nearest_declaring_element).
      *> RULE: cite.py --check 8.4.6.2.1 "The item in source element A
      *>   if the name is declared in source element A" -> OK
      *>   §8.4.6.2.1 3) b) 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1 GLOBAL.
          05 X PIC X(3) VALUE "G1X".
       01 G2 GLOBAL.
          05 X PIC X(3) VALUE "G2X".
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "PB1047B"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047B.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "X=" X
           EXIT PROGRAM.
       END PROGRAM PB1047B.
       END PROGRAM PB1047A.
