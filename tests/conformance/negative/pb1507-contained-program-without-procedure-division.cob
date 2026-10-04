      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1507 - 10.6.1: the contained program-definition slot is INSIDE the procedure-division bracket -
      *>   `[ procedure-division [ program-definition ] ... ]` - so a program that contains programs shall have a
      *>   procedure division. NPD1507 has none and contains NPD1507B. COBOLNET2274.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NPD1507.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NPD1507B.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           GOBACK.
       END PROGRAM NPD1507B.
       END PROGRAM NPD1507.
