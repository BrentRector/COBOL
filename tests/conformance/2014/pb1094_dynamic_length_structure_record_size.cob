      *> kb/Work PB1094 - a record that holds a DYNAMIC LENGTH STRUCTURE
      *> member CONTAINS its length field and delimiter (ISO 12.3.7.4
      *> GR18 and GR19; docs/CONFORMANCE.md section 3 D-DL3), so every
      *> record size is counted with them: 13.18.43.4 GR8's size of a
      *> record description, the number of bytes a RECORD VARYING ...
      *> DEPENDING ON item holds (GR13 a, GR15), and the fixed length of
      *> a Format 1 file's records (GR6), whose members then each take
      *> their MAXIMUM extent (length field + the item's maximum size
      *> 8.5.1.10.1 + delimiter), the data followed by blanks.
      *> A second description of the same file reads the characters the
      *> file holds, and each character's code (FUNCTION ORD less one)
      *> is displayed.  Derived from GR18 and GR19, never read from a
      *> run.  FX is RECORD CONTAINS 20: K X(2), then D1 DELIMITED LIMIT
      *> 5 (maximum extent 5 + 1 = 6), D2 SHORT PREFIXED LIMIT 4
      *> (2 + 4 = 6), Z X(1) - 15 bytes, padded to the 20 the clause
      *> states.  Record 1 (KK, AB, CDE, Z):
      *>   75 75 | 65 66 0 32 32 32 | 0 3 67 68 69 32 | 90 | 5 x 32
      *> and record 2 (LL, ABCDEFG cut to its LIMIT 5 on the right
      *> 8.5.1.10.4, two blanks + X + a blank, Y):
      *>   76 76 | 65 66 67 68 69 0 | 0 4 32 32 88 32 | 89 | 5 x 32
      *> VF is RECORD IS VARYING FROM 5 TO 40 DEPENDING ON VLEN, 5 being
      *> the smallest record (K 2 + D1's delimiter 1 + D2's length field
      *> 2); record KK / AB / CDE is 2 + 3 + 5 = 10 bytes, which VLEN
      *> holds after the READ as well as before the WRITE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1094-SIZE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DYNAMIC LENGTH STRUCTURE DS-DELIM IS DELIMITED
           DYNAMIC LENGTH STRUCTURE DS-SHORT IS SHORT PREFIXED.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FX ASSIGN TO "pb1094siz.fix"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FXRAW ASSIGN TO "pb1094siz.fix"
               ORGANIZATION IS SEQUENTIAL.
           SELECT VF ASSIGN TO "pb1094siz.var"
               ORGANIZATION IS SEQUENTIAL.
           SELECT VFRAW ASSIGN TO "pb1094siz.var"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD FX RECORD CONTAINS 20 CHARACTERS.
       01 FXR.
          05 K  PIC X(2).
          05 D1 PIC X DYNAMIC LENGTH DS-DELIM LIMIT IS 5.
          05 D2 PIC X DYNAMIC LENGTH DS-SHORT LIMIT IS 4.
          05 Z  PIC X(1).
       FD FXRAW RECORD CONTAINS 20 CHARACTERS.
       01 FXRAWR PIC X(20).
       FD VF RECORD IS VARYING IN SIZE FROM 5 TO 40 CHARACTERS
           DEPENDING ON VLEN.
       01 VR.
          05 VK PIC X(2).
          05 V1 PIC X DYNAMIC LENGTH DS-DELIM LIMIT IS 5.
          05 V2 PIC X DYNAMIC LENGTH DS-SHORT LIMIT IS 5.
       FD VFRAW RECORD IS VARYING IN SIZE FROM 1 TO 40 CHARACTERS
           DEPENDING ON RLEN.
       01 VRAW PIC X(40).
       WORKING-STORAGE SECTION.
       01 VLEN PIC 9(4).
       01 RLEN PIC 9(4).
       01 I   PIC 9(4).
       01 N   PIC 999.
       01 CODE-VALUE PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FX.
           MOVE "KK" TO K.
           MOVE "AB" TO D1.
           MOVE "CDE" TO D2.
           MOVE "Z" TO Z.
           WRITE FXR.
           MOVE "LL" TO K.
           MOVE "ABCDEFG" TO D1.
           MOVE "  X " TO D2.
           MOVE "Y" TO Z.
           WRITE FXR.
           CLOSE FX.
           OPEN INPUT FXRAW.
           PERFORM 2 TIMES
               READ FXRAW
               DISPLAY "FIXED" WITH NO ADVANCING
               PERFORM VARYING I FROM 1 BY 1 UNTIL I > 20
                   COMPUTE CODE-VALUE = FUNCTION ORD(FXRAWR(I:1)) - 1
                   MOVE CODE-VALUE TO N
                   DISPLAY " " N WITH NO ADVANCING
               END-PERFORM
               DISPLAY " "
           END-PERFORM.
           CLOSE FXRAW.
           MOVE "ZZ" TO K.
           MOVE "QQQQQQ" TO D1 D2.
           OPEN INPUT FX.
           READ FX.
           DISPLAY K "[" D1 "][" D2 "]" Z " " FUNCTION LENGTH(D1)
               "/" FUNCTION LENGTH(D2).
           READ FX.
           DISPLAY K "[" D1 "][" D2 "]" Z " " FUNCTION LENGTH(D1)
               "/" FUNCTION LENGTH(D2).
           CLOSE FX.
      *> RECORD IS VARYING ... DEPENDING ON.
           OPEN OUTPUT VF.
           MOVE "KK" TO VK.
           MOVE "AB" TO V1.
           MOVE "CDE" TO V2.
           MOVE 10 TO VLEN.
           WRITE VR.
           CLOSE VF.
           OPEN INPUT VFRAW.
           MOVE 40 TO RLEN.
           READ VFRAW.
           DISPLAY "VARYING " RLEN WITH NO ADVANCING.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > RLEN
               COMPUTE CODE-VALUE = FUNCTION ORD(VRAW(I:1)) - 1
               MOVE CODE-VALUE TO N
               DISPLAY " " N WITH NO ADVANCING
           END-PERFORM.
           DISPLAY " ".
           CLOSE VFRAW.
           MOVE "ZZ" TO VK.
           MOVE "QQQQQQ" TO V1 V2.
           MOVE 0 TO VLEN.
           OPEN INPUT VF.
           READ VF.
           DISPLAY VK "[" V1 "][" V2 "] " VLEN.
           CLOSE VF.
           STOP RUN.
