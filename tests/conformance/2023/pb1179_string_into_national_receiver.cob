      *> PB1179 - ISO 14.9.43.3 SR1 admits an identifier-3 of usage
      *>   national; 14.9.43.4 GR3 a) moves the characters national-to-
      *>   national when identifier-3 is class national, and GR7 changes
      *>   only the positions written; GR2 makes a figurative constant an
      *>   implicit ONE-character item of identifier-3's usage.
      *> cite.py --check 14.9.43.3 "all identifiers, except identifier-4,
      *>   shall be described implicitly or explicitly as usage display or
      *>   national" -> OK  14.9.43.3 1)
      *> cite.py --find "national-to-national moves" -> OK  14.9.43.4 3) a)
      *> Derivation (national receivers; unwritten positions keep content):
      *>   A NS "AB-C" into NOUT "........": "AB-C....".
      *>   B "AB" (delimited by N"-") then N"-Z" into NB "XXXXXX": "AB-ZXX".
      *>   C pointer 3 into "........": positions 3-6 written, pointer 7:
      *>     "..AB-C..".
      *>   G a GROUP-USAGE NATIONAL receiver "......": "XY" written at the
      *>     start: NG1 "XY." NG2 "...".
      *>   F SPACE (one national space) into "....": " ...".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1179.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NS PIC N(4) VALUE N"AB-C".
       01 NOUT PIC N(8) VALUE N"........".
       01 NB PIC N(6) VALUE N"XXXXXX".
       01 NP PIC 99 VALUE 3.
       01 NG GROUP-USAGE NATIONAL.
          05 NG1 PIC N(3) VALUE N"...".
          05 NG2 PIC N(3) VALUE N"...".
       01 NX PIC N(2) VALUE N"XY".
       01 NF PIC N(4) VALUE N"....".
       PROCEDURE DIVISION.
           STRING NS DELIMITED BY SIZE INTO NOUT
           DISPLAY "A[" NOUT "]"
           STRING NS DELIMITED BY N"-" N"-Z" DELIMITED BY SIZE INTO NB
           DISPLAY "B[" NB "]"
           MOVE N"........" TO NOUT
           STRING NS DELIMITED BY SIZE INTO NOUT WITH POINTER NP
           DISPLAY "C[" NOUT "] " NP
           STRING NX DELIMITED BY SIZE INTO NG
           DISPLAY "G[" NG1 "][" NG2 "]"
           STRING SPACE DELIMITED BY SIZE INTO NF
           DISPLAY "F[" NF "]"
           STOP RUN.
