      *> kb/Work PB1406 - the operands of a concatenation expression
      *> (ISO 8.8.3.1: literal-1 & literal-2) include the two WORDS that
      *> stand for a literal, and its class is folded PAIRWISE (8.8.3.3
      *> GR1 a/b/c per (accumulated, next) pair). Expected values:
      *>   SYM-A / SYM-B = ordinals 66 / 67 of the native alphanumeric
      *>     set = "A" / "B" (12.3.7.4 GR11 b; ordinal n is code unit
      *>     n-1), each ONE character in a concatenation (8.3.3.6.4 GR3a)
      *>     and of its partner's class (8.8.3.3 GR1a)
      *>   V  = "V" & KA & K2 = "V" "Q" "QR" = VQQR, padded to X(6)
      *>     (13.10.3 SR2: a constant-name stands for its literal, also
      *>     inside another constant's literal-1)
      *>   "X" & SYM-A = XA ; SYM-A & "X" = AX
      *>   SYM-A & SYM-B & "!" = AB! (GR1b: SYM-A & SYM-B is class
      *>     alphanumeric, then GR1c with "!")
      *>   KN & N"Y" & SPACE = ZY + national space (GR1a: SPACE takes
      *>     class national)
      *>   B"0" & KB & ZERO = 010 (GR1c, then GR1a: ZERO is boolean 0)
      *>   WB = B"0" & KB & ZERO is a boolean relation (class boolean)
      *>   LENGTH(KA & "" & K2) = 3 (8.8.3.3 GR2)
      *>   "" & "" is a zero-length literal (GR2 second sentence), and
      *>     a zero-length sending item moved to X(4) fills it with
      *>     spaces (14.6.8.5 NOTE): [    ]
      *>   CLASS NHI FOR NATIONAL IS N"A" & HIGH-VALUE: inside SPECIAL-
      *>     NAMES the HIGH-VALUE of a NATIONAL clause is the highest
      *>     character of the NATIVE national sequence (12.3.7.4 GR10),
      *>     U+FFFF - so NX"FFFF" is in the class and NX"00FF" is not
      *>   CLASS NSP FOR NATIONAL IS SPACE & N"A": class national by
      *>     GR1a, so the national space is in the class
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1406CWORD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-A SYM-B ARE 66 67
           CLASS NHI FOR NATIONAL IS N"A" & HIGH-VALUE
           CLASS NSP FOR NATIONAL IS SPACE & N"A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KA CONSTANT AS "Q".
       01 KN CONSTANT AS N"Z".
       01 KB CONSTANT AS B"1".
       01 K2 CONSTANT AS KA & "R".
       01 V PIC X(6) VALUE "V" & KA & K2.
       01 W PIC X(4).
       01 WN PIC N(3).
       01 WB PIC 1(3).
       01 N1 PIC N(1).
       01 L PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "V=[" V "]".
           MOVE "X" & SYM-A TO W.
           DISPLAY "W1=[" W "]".
           MOVE SYM-A & "X" TO W.
           DISPLAY "W2=[" W "]".
           MOVE SYM-A & SYM-B & "!" TO W.
           DISPLAY "W3=[" W "]".
           MOVE KN & N"Y" & SPACE TO WN.
           DISPLAY "N=[" WN "]".
           MOVE B"0" & KB & ZERO TO WB.
           DISPLAY "B=[" WB "]".
           IF WB = B"0" & KB & ZERO
               DISPLAY "BREL=EQ"
           ELSE
               DISPLAY "BREL=NE"
           END-IF.
           MOVE FUNCTION LENGTH(KA & "" & K2) TO L.
           DISPLAY "L=" L.
           MOVE "ABCD" TO W.
           MOVE "" & "" TO W.
           DISPLAY "Z=[" W "]".
           MOVE NX"FFFF" TO N1.
           IF N1 IS NHI DISPLAY "HI=IN" ELSE DISPLAY "HI=OUT" END-IF.
           MOVE NX"00FF" TO N1.
           IF N1 IS NHI DISPLAY "FF=IN" ELSE DISPLAY "FF=OUT" END-IF.
           MOVE N" " TO N1.
           IF N1 IS NSP DISPLAY "SP=IN" ELSE DISPLAY "SP=OUT" END-IF.
           STOP RUN.
