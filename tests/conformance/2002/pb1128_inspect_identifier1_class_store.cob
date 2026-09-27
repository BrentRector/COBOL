      *> kb/Work PB1128 + PB1126 - INSPECT identifier-1's class and store, and the size of a figurative.
      *> The national group and the usage-national numeric-edited item arrive in 2002, so this is the
      *> introducing edition; every leg below is unchanged text in 2014 and 2023.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>
      *> N1  ISO 13.18.29.4 GR2 b): a national group "is treated as though it were an elementary data
      *>     item of usage national ... described with PICTURE N(m)". CONVERTING N"A" TO N"Z" over five
      *>     N"A" positions (14.9.22.4 GR20) => ZZZZZ. Fails if the national positions are stored back
      *>     through the group's BYTE image (ten UTF-16 halves into ten bytes: mojibake).
      *> N2  The same store for REPLACING (GR17 b): ABACA, ALL N"A" BY N"Q" => QBQCQ.
      *> N3  GR1: "For purposes of determining its length, identifier-1 is treated as a sending data
      *>     item". NOD's table depends on NCNT INSIDE the group, so as a SENDING operand it is its
      *>     current extent (13.18.38.4 GR8): with NCNT = 3 the image is N"3AAA"; ALL N"A" BY N"Z"
      *>     makes it N"3ZZZ" and positions 5-6 are not part of the operand, so at NCNT = 5 the group
      *>     reads 5ZZZAA. Fails if the store uses the RECEIVING maximum length (4 positions over 6).
      *> N4  STRING's receiver is the same group-value store (14.9.43.4): STRING N"AB" INTO NG3
      *>     holding N"XXXYY" writes positions 1-2 and leaves the rest => ABXYY.
      *> N5  8.5.2.1 Table 2: numeric-edited "(if usage is national)" is class NATIONAL, and GR4 c)
      *>     inspects it "as though it had been redefined as category national". NE PIC ZZ9 USAGE
      *>     NATIONAL holds N"  5"; TALLYING FOR ALL N" " (legal by 14.9.22.3 SR4) => 02.
      *> N6  14.9.22.3 SR3 bars ALL only from literal-1..literal-4, so literal-5 may be ALL "Q"; GR22
      *>     sizes it to literal-4 and 8.3.3.6.4 GR2 repeats it "character by character":
      *>     ABCDEF CONVERTING "ABC" TO ALL "Q" => QQQDEF; TO ALL "QR" => QRQDEF.
      *> N7  GR14: a figurative literal-3 has "the size of the data item referenced by identifier-3",
      *>     here a function-identifier: FUNCTION UPPER-CASE(P) with P = "ab" is "AB", size 2, so XABX
      *>     REPLACING ALL UPPER-CASE(P) BY SPACES => "X  X". Fails if SPACES stays one character
      *>     (the operand is then skipped: XABX).
      *> N8  GR22, the literal-5 twin: CONVERTING FUNCTION LOWER-CASE(F) TO ZEROS with F = "ABC"
      *>     maps a,b,c to 0 => 000def (not 0bcdef).
      *> N9  GR2: a zero-length identifier-1 (ZG's table at count 0 with no fixed part, 8.5.4) leaves
      *>     identifier-2 "unchanged" and control goes "immediately" to the end of the statement. The
      *>     counter's group holds "7 ", which is not a canonical PIC 99 image; the tallying pass must
      *>     not run at all, so CG still reads "7 " (not "07").
      *>
      *>   N1=ZZZZZ
      *>   N2=QBQCQ
      *>   N3=5ZZZAA
      *>   N4=ABXYY
      *>   N5=02
      *>   N6=QQQDEF QRQDEF
      *>   N7=X  X
      *>   N8=000def
      *>   N9=7
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1128INSP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NG GROUP-USAGE NATIONAL.
          05 NC PIC N OCCURS 5 VALUE N"A".
       01 NG2 GROUP-USAGE NATIONAL.
          05 N2A PIC N(3) VALUE N"ABA".
          05 N2B PIC N(2) VALUE N"CA".
       01 NOD GROUP-USAGE NATIONAL.
          05 NCNT PIC 9.
          05 NDE PIC N OCCURS 1 TO 5 DEPENDING ON NCNT.
       01 NG3 GROUP-USAGE NATIONAL.
          05 N3A PIC N(3) VALUE N"XXX".
          05 N3B PIC N(2) VALUE N"YY".
       01 NE PIC ZZ9 USAGE NATIONAL VALUE N"  5".
       01 C  PIC 99 VALUE 0.
       01 X1 PIC X(6) VALUE "ABCDEF".
       01 X2 PIC X(6) VALUE "ABCDEF".
       01 S1 PIC X(4) VALUE "XABX".
       01 P  PIC X(2) VALUE "ab".
       01 W  PIC X(6) VALUE "abcdef".
       01 F  PIC X(3) VALUE "ABC".
       01 CNT0 PIC 9 VALUE 0.
       01 ZG.
          05 ZE PIC X OCCURS 0 TO 5 DEPENDING ON CNT0.
       01 CG.
          05 C2 PIC 99.
       PROCEDURE DIVISION.
           INSPECT NG CONVERTING N"A" TO N"Z"
           DISPLAY "N1=" NG
           INSPECT NG2 REPLACING ALL N"A" BY N"Q"
           DISPLAY "N2=" NG2
           MOVE 5 TO NCNT
           MOVE N"AAAAA" TO NDE(1) NDE(2) NDE(3) NDE(4) NDE(5)
           MOVE 3 TO NCNT
           INSPECT NOD REPLACING ALL N"A" BY N"Z"
           MOVE 5 TO NCNT
           DISPLAY "N3=" NOD
           STRING N"AB" DELIMITED BY SIZE INTO NG3
           DISPLAY "N4=" NG3
           INSPECT NE TALLYING C FOR ALL N" "
           DISPLAY "N5=" C
           INSPECT X1 CONVERTING "ABC" TO ALL "Q"
           INSPECT X2 CONVERTING "ABC" TO ALL "QR"
           DISPLAY "N6=" X1 " " X2
           INSPECT S1 REPLACING ALL FUNCTION UPPER-CASE(P) BY SPACES
           DISPLAY "N7=" S1
           INSPECT W CONVERTING FUNCTION LOWER-CASE(F) TO ZEROS
           DISPLAY "N8=" W
           MOVE "7 " TO CG
           INSPECT ZG TALLYING C2 FOR CHARACTERS
           DISPLAY "N9=" CG
           STOP RUN.
