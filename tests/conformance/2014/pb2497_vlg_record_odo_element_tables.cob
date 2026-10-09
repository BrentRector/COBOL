      *> kb/Work PB2497 - A FILE OR SORT RECORD THAT HOLDS AN OCCURS
      *> DEPENDING TABLE WHOSE ELEMENTS ARE VARIABLE-LENGTH GROUPS is
      *> written and read back (determination D-FRA (viii),
      *> docs/CONFORMANCE.md section 3).
      *>
      *> 8.5.1.12.1 makes every occurrence of TE a variable-length group
      *> (DD is a dynamic-length item), and 8.5.1.11.2 makes the record
      *> "behave in all respects as though it were in fact contiguous with
      *> its neighbors": WRITE sends K, then each of the first K
      *> occurrences' current images (13.18.38.4 GR8 b, the sending
      *> operand; GR9 has the program set K first). The record's LENGTH
      *> cannot say how many occurrences it holds, because the elements
      *> have no fixed width, so a READ / RETURN takes the count from:
      *>   (v)   the record's extent table, when it travelled with the
      *>         record (sequential frames, the sort store, indexed
      *>         frames): the number of components it describes;
      *>   (vi)  the fixed form of a file of fixed-length records: the
      *>         maximum - every occurrence sits at its fixed-form place;
      *>   else  data-name-1: the record is decomposed at the maximum
      *>         (GR8 b, the receiving record area), which places the K
      *>         the record holds (13.18.38.3 SR20 puts it before the
      *>         table), then again at K's value; the D-FRA take step
      *>         then gives each component the characters beyond the
      *>         fixed material still to come.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   A  sequential, two records (K=2: ab/1 cde/2; K=1: z/9), read
      *>      back by their extent tables; K is preset to 3 and DD(3) to
      *>      "q" before the READ, and the record's own count wins
      *>                                          = 2 [ab][1][cde][2], 1 [z][9]
      *>   B  written through the file's CHARACTER record (no table
      *>      travels): "1ab1" and "2ab12"; K read from the record, then
      *>      the take step at K
      *>                                          = 1 [ab][1], 2 [ab][1][][2]
      *>   C  RECORD CONTAINS 30 CHARACTERS: the fixed form, each DD padded
      *>      to 5, the absent third occurrence spaces = 2 [ab][1][cde][2]
      *>   D  DEPENDING ON outside the record (WS-N): the record states 2
      *>      by its extent table                 = [ab][1][c][2]
      *>   E  RELEASE / RETURN in KY order        = a1 q 1 [x][7];
      *>                                            b2 pp 2 [ab][1] [cde][2]
      *>   F  indexed, READ by key                = k2 p 2 [ab][1][c][2]
      *>   G  an EXTERNAL record (a storage cell) = pp 2 [ab][1][cde][2]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2497REC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT QF ASSIGN TO "pb2497.seq" ORGANIZATION SEQUENTIAL.
           SELECT LF ASSIGN TO "pb2497.chr" ORGANIZATION SEQUENTIAL.
           SELECT FF ASSIGN TO "pb2497.fix" ORGANIZATION SEQUENTIAL.
           SELECT OF2 ASSIGN TO "pb2497.out" ORGANIZATION SEQUENTIAL.
           SELECT SF ASSIGN TO "pb2497.srt".
           SELECT XF ASSIGN TO "pb2497.idx" ORGANIZATION INDEXED
               ACCESS DYNAMIC RECORD KEY XKY.
           SELECT EF ASSIGN TO "pb2497.ext" ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD QF.
       01 QREC.
          05 QK PIC 9.
          05 QE OCCURS 1 TO 3 DEPENDING ON QK.
             10 QD PIC X DYNAMIC LENGTH LIMIT 5.
             10 QX PIC X.
       FD LF.
       01 LREC.
          05 LK PIC 9.
          05 LE OCCURS 1 TO 3 DEPENDING ON LK.
             10 LD PIC X DYNAMIC LENGTH LIMIT 5.
             10 LX PIC X.
       01 LC4 PIC X(4).
       01 LC5 PIC X(5).
       FD FF RECORD CONTAINS 30 CHARACTERS.
       01 FREC.
          05 FK PIC 9.
          05 FE OCCURS 1 TO 3 DEPENDING ON FK.
             10 FD1 PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX PIC X.
       FD OF2.
       01 OREC.
          05 OE OCCURS 1 TO 3 DEPENDING ON WS-N.
             10 OD PIC X DYNAMIC LENGTH LIMIT 5.
             10 OX PIC X.
       SD SF.
       01 SREC.
          05 KY PIC X(2).
          05 PD PIC X DYNAMIC LENGTH LIMIT 4.
          05 SK PIC 9.
          05 SE OCCURS 1 TO 3 DEPENDING ON SK.
             10 SD1 PIC X DYNAMIC LENGTH LIMIT 5.
             10 SX PIC X.
       FD XF.
       01 XREC.
          05 XKY PIC X(2).
          05 XPD PIC X DYNAMIC LENGTH LIMIT 4.
          05 XK PIC 9.
          05 XE OCCURS 1 TO 3 DEPENDING ON XK.
             10 XD PIC X DYNAMIC LENGTH LIMIT 5.
             10 XX PIC X.
       FD EF IS EXTERNAL.
       01 EREC.
          05 EPD PIC X DYNAMIC LENGTH LIMIT 4.
          05 EK PIC 9.
          05 EE OCCURS 1 TO 3 DEPENDING ON EK.
             10 ED PIC X DYNAMIC LENGTH LIMIT 5.
             10 EX PIC X.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9.
       01 WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
      *> A: sequential, the extent table states the count.
           OPEN OUTPUT QF
           MOVE 2 TO QK
           MOVE "ab" TO QD(1) MOVE "1" TO QX(1)
           MOVE "cde" TO QD(2) MOVE "2" TO QX(2)
           WRITE QREC
           MOVE 1 TO QK
           MOVE "z" TO QD(1) MOVE "9" TO QX(1)
           WRITE QREC
           CLOSE QF
           MOVE 3 TO QK MOVE "q" TO QD(3)
           OPEN INPUT QF
           READ QF
           DISPLAY "A " QK " [" QD(1) "][" QX(1) "][" QD(2) "]["
               QX(2) "]"
           READ QF
           DISPLAY "A " QK " [" QD(1) "][" QX(1) "]"
           CLOSE QF
      *> B: no extent table, data-name-1 read from the record.
           OPEN OUTPUT LF
           WRITE LC4 FROM "1ab1"
           WRITE LC5 FROM "2ab12"
           CLOSE LF
           OPEN INPUT LF
           READ LF
           DISPLAY "B " LK " [" LD(1) "][" LX(1) "]"
           READ LF
           DISPLAY "B " LK " [" LD(1) "][" LX(1) "][" LD(2) "]["
               LX(2) "]"
           CLOSE LF
      *> C: the fixed form of a file of fixed-length records.
           OPEN OUTPUT FF
           MOVE 2 TO FK
           MOVE "ab" TO FD1(1) MOVE "1" TO FX(1)
           MOVE "cde" TO FD1(2) MOVE "2" TO FX(2)
           WRITE FREC
           CLOSE FF
           MOVE 3 TO FK MOVE "zz" TO FD1(3)
           OPEN INPUT FF
           READ FF
           DISPLAY "C " FK " [" FD1(1) "][" FX(1) "][" FD1(2) "]["
               FX(2) "]"
           CLOSE FF
      *> D: data-name-1 outside the record.
           OPEN OUTPUT OF2
           MOVE 2 TO WS-N
           MOVE "ab" TO OD(1) MOVE "1" TO OX(1)
           MOVE "c" TO OD(2) MOVE "2" TO OX(2)
           WRITE OREC
           CLOSE OF2
           MOVE 1 TO WS-N
           OPEN INPUT OF2
           READ OF2
           MOVE 2 TO WS-N
           DISPLAY "D [" OD(1) "][" OX(1) "][" OD(2) "][" OX(2) "]"
           CLOSE OF2
      *> E: the sort store.
           SORT SF ON ASCENDING KEY KY
               INPUT PROCEDURE IS E-IN
               OUTPUT PROCEDURE IS E-OUT
      *> F: an indexed file read by its key.
           OPEN OUTPUT XF
           MOVE "k2" TO XKY MOVE "p" TO XPD MOVE 2 TO XK
           MOVE "ab" TO XD(1) MOVE "1" TO XX(1)
           MOVE "c" TO XD(2) MOVE "2" TO XX(2)
           WRITE XREC
           MOVE "k1" TO XKY MOVE "qqq" TO XPD MOVE 1 TO XK
           MOVE "z" TO XD(1) MOVE "9" TO XX(1)
           WRITE XREC
           CLOSE XF
           OPEN INPUT XF
           MOVE "k2" TO XKY
           READ XF KEY IS XKY
           DISPLAY "F " XKY " " XPD " " XK " [" XD(1) "][" XX(1) "]["
               XD(2) "][" XX(2) "]"
           CLOSE XF
      *> G: an EXTERNAL record, held in a storage cell.
           OPEN OUTPUT EF
           MOVE "pp" TO EPD MOVE 2 TO EK
           MOVE "ab" TO ED(1) MOVE "1" TO EX(1)
           MOVE "cde" TO ED(2) MOVE "2" TO EX(2)
           WRITE EREC
           CLOSE EF
           MOVE "x" TO EPD MOVE 1 TO EK
           OPEN INPUT EF
           READ EF
           DISPLAY "G " EPD " " EK " [" ED(1) "][" EX(1) "][" ED(2) "]["
               EX(2) "]"
           CLOSE EF
           STOP RUN.
       E-IN.
           MOVE "b2" TO KY MOVE "pp" TO PD MOVE 2 TO SK
           MOVE "ab" TO SD1(1) MOVE "1" TO SX(1)
           MOVE "cde" TO SD1(2) MOVE "2" TO SX(2)
           RELEASE SREC
           MOVE "a1" TO KY MOVE "q" TO PD MOVE 1 TO SK
           MOVE "x" TO SD1(1) MOVE "7" TO SX(1)
           RELEASE SREC.
       E-OUT.
           PERFORM UNTIL WS-EOF = "Y"
               RETURN SF AT END MOVE "Y" TO WS-EOF
               NOT AT END
                   DISPLAY "E " KY " " PD " " SK " [" SD1(1) "]["
                       SX(1) "]"
                   IF SK = 2
                       DISPLAY "E [" SD1(2) "][" SX(2) "]"
                   END-IF
               END-RETURN
           END-PERFORM.
