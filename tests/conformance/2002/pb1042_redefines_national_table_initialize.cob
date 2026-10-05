      *> kb/Work PB1042 (found while there) — INITIALIZE of a REDEFINES view over a NATIONAL table.
      *> ISO/IEC 1989:2023 §13.18.60.4 GR8 (USAGE NATIONAL) leaves a national character size to the
      *> implementor (D-N1: two bytes), and §13.18.44.4 GR1 lays the redefining table over the
      *> redefined bytes, so KT(n) starts 4 x (n - 1) bytes into K. §14.9.20.4 GR5 b) 2) gives
      *> INITIALIZE one move per occurrence; each KT occurrence takes N"ab" (REPLACING NATIONAL).
      *> The INITIALIZE cursor used to stride by the character-position count, so KT(1) and KT(2)
      *> overlapped and KT(3) kept two bytes of the old content.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1042NT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K.
          05 KX PIC X(12).
       01 KR REDEFINES K.
          05 KT PIC N(2) OCCURS 3.
       PROCEDURE DIVISION.
           MOVE ALL "Z" TO KX
           INITIALIZE KR REPLACING NATIONAL BY N"ab"
           DISPLAY "[" KT(1) "][" KT(2) "][" KT(3) "]"
           STOP RUN.
