      *> reject-at: 2023
      *> kb/Work PB1460. ISO 8.4.6.3 2): a COMMON program may be referenced
      *> by the programs its container contains, "except that the program
      *> possessing the common attribute and any programs contained within
      *> it may reference the program-name only if the program possesses
      *> the recursive attribute" (cite.py --check 8.4.6.3 -> OK 2)).
      *> W73N1Q is COMMON and NOT recursive, so from W73N1Q1 - contained in
      *> it - its name is out of scope, and 14.9.4.3 SR15 refuses the
      *> AS NESTED literal (COBOLNET1676). It used to compile and die at run
      *> time on EC-PROGRAM-NOT-FOUND.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73N1.
       PROCEDURE DIVISION.
           CALL "W73N1Q" AS NESTED
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73N1Q IS COMMON PROGRAM.
       PROCEDURE DIVISION.
           CALL "W73N1Q1" AS NESTED
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73N1Q1.
       PROCEDURE DIVISION.
           CALL "W73N1Q" AS NESTED
           GOBACK.
       END PROGRAM W73N1Q1.
       END PROGRAM W73N1Q.
       END PROGRAM W73N1.
