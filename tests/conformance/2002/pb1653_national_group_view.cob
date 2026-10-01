      *> kb/Work PB1653 - a NATIONAL GROUP that REDEFINES another item (a Tier-B window over the class's one
      *> byte backing) is the elementary national item of its as-if PICTURE N(m) everywhere, exactly as the same
      *> group is when it redefines nothing.
      *> ISO 13.18.29.4 GR2 b): "a national group is treated as though it were an elementary data item of usage
      *>   national and class and category national described with PICTURE N(m), where m is the length of the
      *>   group" (cite.py --check 13.18.29.4 -> OK 13.18.29.4 2) b)). NGV below has m = 5 national positions;
      *>   the implementation stores a national position in two bytes (documented, CONFORMANCE.md D-N1), so the
      *>   view's window is 10 bytes holding 5 positions - the defect read the ten BYTES as ten characters.
      *> ISO 14.9.25.4 GR4: "Bit group items and national group items are treated as elementary items in the
      *>   MOVE statement", and GR6 a): "alignment and any necessary space filling shall take place as defined
      *>   in 14.6.8, Alignment and transfer of data into data items" (cite.py --check 14.9.25.4 -> OK, both):
      *>   a shorter national sender is left-aligned and space-filled, a longer one truncated on the right.
      *> DERIVATION (every line is printed for the VIEW NGV and the PLAIN twin NGP, which must agree):
      *>   a  MOVE N"ABCDE"            -> ABCDE; V1 = AB (positions 1-2), V2 = CDE (positions 3-5)
      *>   b  MOVE N"XY"               -> "XY" + 3 national spaces
      *>   c  MOVE N"1234567"          -> 12345 (right truncation)
      *>   d  MOVE N"QRSTU" via NZ     -> QRSTU
      *>   e  NGV(2:3)                 -> RST (national positions 2 through 4)
      *>   f  MOVE N"MN" TO NGV(2:2)   -> QMNTU
      *>   g  INITIALIZE               -> five national spaces
      *>   h  INSPECT REPLACING ALL N"L" BY N"Z" over HELLO -> HEZZO ; TALLYING ALL N"Z" -> 002
      *>   i  UNSTRING NU DELIMITED BY N"," INTO NGV (NU = N"AB,CD") -> "AB" + 3 spaces
      *>   j  STRING N"123" DELIMITED SIZE INTO NGV after N"ZYXWV" -> 123WV (STRING leaves the tail)
      *>   k  CALL BY REFERENCE: the formal LG (also m = 5) sees the group's 5 positions and its
      *>      MOVE N"EDCBA" TO LG comes home -> EDCBA ; BY CONTENT delivers 5 positions and returns none
      *>   l  a table of national groups under REDEFINES, an EXTERNAL national group and a file record that is a
      *>      national group (WRITE then READ back) hold their positions: KLMNO PQRST, ABCDE, UVWXY / 12345
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1653A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1653a.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FREC GROUP-USAGE NATIONAL.
          05 FA PIC N(2).
          05 FB PIC N(3).
       WORKING-STORAGE SECTION.
       01 A PIC X(10).
       01 NGV REDEFINES A GROUP-USAGE NATIONAL.
          05 V1 PIC N(2).
          05 V2 PIC N(3).
       01 NGP GROUP-USAGE NATIONAL.
          05 P1 PIC N(2).
          05 P2 PIC N(3).
       01 NZ PIC N(5).
       01 NU PIC N(5) VALUE N"AB,CD".
       01 CNT PIC 9(3).
       01 NGE EXTERNAL GROUP-USAGE NATIONAL.
          05 E1 PIC N(2).
          05 E2 PIC N(3).
       01 A2 PIC X(20).
       01 TBL REDEFINES A2.
          05 NGT GROUP-USAGE NATIONAL OCCURS 2.
             10 T1 PIC N(2).
             10 T2 PIC N(3).
       PROCEDURE DIVISION.
           MOVE N"ABCDE" TO NGV
           MOVE N"ABCDE" TO NGP
           DISPLAY "a=" NGV "|" NGP "|" V1 "|" V2 "|" P1 "|" P2
           MOVE N"XY" TO NGV
           MOVE N"XY" TO NGP
           DISPLAY "b=[" NGV "][" NGP "]"
           MOVE N"1234567" TO NGV
           MOVE N"1234567" TO NGP
           DISPLAY "c=" NGV "|" NGP
           MOVE N"QRSTU" TO NZ
           MOVE NZ TO NGV
           MOVE NZ TO NGP
           DISPLAY "d=" NGV "|" NGP
           DISPLAY "e=" NGV(2:3) "|" NGP(2:3)
           MOVE N"MN" TO NGV(2:2)
           MOVE N"MN" TO NGP(2:2)
           DISPLAY "f=" NGV "|" NGP
           INITIALIZE NGV
           INITIALIZE NGP
           DISPLAY "g=[" NGV "][" NGP "]"
           MOVE N"HELLO" TO NGV
           MOVE N"HELLO" TO NGP
           INSPECT NGV REPLACING ALL N"L" BY N"Z"
           INSPECT NGP REPLACING ALL N"L" BY N"Z"
           MOVE ZERO TO CNT
           INSPECT NGV TALLYING CNT FOR ALL N"Z"
           DISPLAY "h=" NGV "|" NGP "|" CNT
           UNSTRING NU DELIMITED BY N"," INTO NGV
           UNSTRING NU DELIMITED BY N"," INTO NGP
           DISPLAY "i=[" NGV "][" NGP "]"
           MOVE N"ZYXWV" TO NGV
           MOVE N"ZYXWV" TO NGP
           STRING N"123" DELIMITED SIZE INTO NGV
           STRING N"123" DELIMITED SIZE INTO NGP
           DISPLAY "j=" NGV "|" NGP
           MOVE N"ABCDE" TO NGV
           MOVE N"ABCDE" TO NGP
           CALL "SUBX" USING BY REFERENCE NGV
           CALL "SUBX" USING BY REFERENCE NGP
           DISPLAY "k=" NGV "|" NGP
           CALL "SUBY" USING BY CONTENT NGV
           CALL "SUBY" USING BY CONTENT NGP
           DISPLAY "k2=" NGV "|" NGP
           MOVE N"KLMNO" TO NGT(1)
           MOVE N"PQRST" TO NGT(2)
           MOVE N"ABCDE" TO NGE
           DISPLAY "l=" NGT(1) "|" NGT(2) "|" T1(2) "|" T2(1)
               "|" NGE "|" E1 "|" E2
           OPEN OUTPUT F
           MOVE N"UVWXY" TO FREC
           WRITE FREC
           MOVE N"12345" TO FREC
           WRITE FREC
           CLOSE F
           OPEN INPUT F
           READ F
           DISPLAY "l2=" FREC "|" FA "|" FB
           READ F
           DISPLAY "l3=" FREC "|" FA "|" FB
           CLOSE F
           STOP RUN.
       END PROGRAM PB1653A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG GROUP-USAGE NATIONAL.
          05 L1 PIC N(2).
          05 L2 PIC N(3).
       PROCEDURE DIVISION USING LG.
           DISPLAY "x=" LG "|" L1 "|" L2
           MOVE N"EDCBA" TO LG
           GOBACK.
       END PROGRAM SUBX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBY.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LH GROUP-USAGE NATIONAL.
          05 H1 PIC N(2).
          05 H2 PIC N(3).
       PROCEDURE DIVISION USING LH.
           DISPLAY "y=" LH "|" H1 "|" H2
           MOVE N"-----" TO LH
           GOBACK.
       END PROGRAM SUBY.
