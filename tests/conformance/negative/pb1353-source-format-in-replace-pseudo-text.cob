      *> reject-at: 2002 2014 2023
      *> 7.2.4.3 SR10: 'Compiler directive lines shall not be
      *> specified within pseudo-text-1, pseudo-text-2, partial-word-1,
      *> or partial-word-2'. The >>SOURCE FORMAT line below stands inside
      *> pseudo-text-2 of the REPLACE statement; logical conversion
      *> discards it before REPLACE reads its operands, so only the blank
      *> it leaves is seen (kb/Work PB1353). Fixed form.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1353SR.
000300 PROCEDURE DIVISION.
000400     REPLACE ==AA== BY ==BB
000500 >>SOURCE FORMAT FIXED
000600         CC==.
000700     DISPLAY "AA".
000800     STOP RUN.
