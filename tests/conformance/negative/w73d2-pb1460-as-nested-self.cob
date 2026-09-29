      *> reject-at: 2023
      *> kb/Work PB1460 - the SELF spelling of the same rule. ISO 8.4.6.3
      *> 2) (cite.py --check 8.4.6.3 -> OK 2)): the program possessing the
      *> common attribute may reference its own name only if it possesses
      *> the recursive attribute. W73N2Q is COMMON and NOT recursive, so
      *> its own CALL "W73N2Q" AS NESTED names no program in scope and
      *> 14.9.4.3 SR15 refuses it (COBOLNET1676).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73N2.
       PROCEDURE DIVISION.
           CALL "W73N2Q" AS NESTED
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73N2Q IS COMMON PROGRAM.
       PROCEDURE DIVISION.
           CALL "W73N2Q" AS NESTED
           GOBACK.
       END PROGRAM W73N2Q.
       END PROGRAM W73N2.
