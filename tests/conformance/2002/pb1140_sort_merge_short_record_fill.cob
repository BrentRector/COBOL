      *> kb/Work PB1140 - WHICH SPACE FILLS A SHORT RECORD OF A SORT/MERGE
      *> TRANSFER. 14.9.40.4 GR7 (USING) and GR16 (GIVING), and the
      *> MERGE twins 14.9.24.4 GR2 and GR13, space-fill a record that has
      *> fewer character positions than the fixed length of the file it
      *> moves to, "as follows: a) If there is only one record
      *> description entry associated with the file ... and that record
      *> is described as a national data item or as an elementary data
      *> item of usage national and of category numeric, numeric-edited,
      *> or boolean, the record is filled with national space
      *> characters. ... c) Otherwise, the record is filled with
      *> alphanumeric space characters."  (b) needs SELECT WHEN, which
      *> WiseOwl COBOL declines by name - Annex A.4.8 - so it cannot arise.
      *> A national space is the two bytes 00 20 (D-N1, UTF-16BE) and an
      *> alphanumeric space the one byte 20, so every leg below is read
      *> BYTE BY BYTE through an alphanumeric record that shares the area
      *> (9.1.2) or through an X(6) readback of the written file:
      *>   U1 USING FN, one record N(2), into a 6-byte SD: a) -> the two
      *>      bytes after the 4-byte record are 00 20  -> ORD 001 033
      *>   U2 USING FA, one record X(4): c) -> 20 20   -> ORD 033 033
      *>   G1 GIVING GNUM, one record PIC 9(3) USAGE NATIONAL: a) (an
      *>      elementary usage-national numeric item) -> 00 31 00 20 00 20
      *>   G2 GIVING GNAT, one record N(3): a)             -> same bytes
      *>   G3 GIVING GMUL, two records (N(3) and X(6)), no SELECT WHEN:
      *>      c) -> 00 31 20 20 20 20 (the national record is NOT the
      *>      only description, so the alphanumeric space applies)
      *>   G4 GIVING GALN, one record X(6): c)             -> 00 31 20 20 20 20
      *>   M1 MERGE ... GIVING GNUM2 / GMUL2 from a VARYING 4-TO-6 SD: the
      *>      4-byte records stay 4 bytes (a varying SD has no fixed length
      *>      to fill) and the MERGE GR13 a) / c) fill happens on the way
      *>      out: 00 58 00 58 00 20 versus 00 58 00 58 20 20
      *> The SD of G1-G4 (SG) is VARYING 2 TO 6 so that its 2-byte record STAYS 2 bytes for the fill to act on: an SD
      *> with no RECORD clause is the implied Format 1, whose every record is the largest description's size, so a
      *> RELEASE of the short description would send the whole 6-byte area and nothing would be short (kb/Work PB322 F,
      *> docs/CONFORMANCE.md DOC-A.1-147). The range 2..6 sits inside the GIVING files' fixed 6 bytes (SR11), and the
      *> key is SG-SHORT, the 2 bytes every record has (§14.9.40.3 SR6 g: a key lies within the SD's minimum size).
      *> A leg fails if the fill is the alphanumeric space everywhere
      *> (U1, G1, M1 wrong), the national space everywhere (G3, M1 wrong),
      *> or taken from the connector's any-national-record flag (G3 wrong).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1140FILL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FN ASSIGN TO "pb1140fn.dat".
           SELECT FN2 ASSIGN TO "pb1140fn2.dat".
           SELECT FA ASSIGN TO "pb1140fa.dat".
           SELECT GNUM ASSIGN TO "pb1140gn.dat".
           SELECT GNAT ASSIGN TO "pb1140gt.dat".
           SELECT GMUL ASSIGN TO "pb1140gm.dat".
           SELECT GALN ASSIGN TO "pb1140ga.dat".
           SELECT GNUM2 ASSIGN TO "pb1140hn.dat".
           SELECT GMUL2 ASSIGN TO "pb1140hm.dat".
           SELECT BACK ASSIGN TO "pb1140gn.dat".
           SELECT BACKT ASSIGN TO "pb1140gt.dat".
           SELECT BACKM ASSIGN TO "pb1140gm.dat".
           SELECT BACKA ASSIGN TO "pb1140ga.dat".
           SELECT BACKH ASSIGN TO "pb1140hn.dat".
           SELECT BACKI ASSIGN TO "pb1140hm.dat".
           SELECT SN ASSIGN TO "pb1140sn.tmp".
           SELECT SG ASSIGN TO "pb1140sg.tmp".
           SELECT SM ASSIGN TO "pb1140sm.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD FN.
       01 FN-REC PIC N(2).
       FD FN2.
       01 FN2-REC PIC N(2).
       FD FA.
       01 FA-REC PIC X(4).
       FD GNUM.
       01 GNUM-REC PIC 9(3) USAGE NATIONAL.
       FD GNAT.
       01 GNAT-REC PIC N(3).
       FD GMUL.
       01 GMUL-N PIC N(3).
       01 GMUL-X PIC X(6).
       FD GALN.
       01 GALN-REC PIC X(6).
       FD GNUM2.
       01 GNUM2-REC PIC 9(3) USAGE NATIONAL.
       FD GMUL2.
       01 GMUL2-N PIC N(3).
       01 GMUL2-X PIC X(6).
       FD BACK.
       01 BACK-REC PIC X(6).
       FD BACKT.
       01 BACKT-REC PIC X(6).
       FD BACKM.
       01 BACKM-REC PIC X(6).
       FD BACKA.
       01 BACKA-REC PIC X(6).
       FD BACKH.
       01 BACKH-REC PIC X(6).
       FD BACKI.
       01 BACKI-REC PIC X(6).
       SD SN.
       01 SN-REC.
          05 SN-K PIC N(2).
          05 SN-T PIC N(1).
       01 SN-X PIC X(6).
       SD SG
          RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS.
       01 SG-LONG PIC N(3).
       01 SG-SHORT PIC N(1).
       SD SM
          RECORD IS VARYING IN SIZE FROM 4 TO 6 CHARACTERS.
       01 SM-REC.
          05 SM-K PIC N(2).
          05 SM-T PIC N(1).
       WORKING-STORAGE SECTION.
       01 W-I PIC 99.
       01 W-O PIC 9(3).
       01 W-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FN.
           MOVE N"YY" TO FN-REC. WRITE FN-REC.
           MOVE N"XX" TO FN-REC. WRITE FN-REC.
           CLOSE FN.
           SORT SN ON ASCENDING KEY SN-K USING FN
               OUTPUT PROCEDURE SHOW-N.
           OPEN OUTPUT FA.
           MOVE "YYYY" TO FA-REC. WRITE FA-REC.
           CLOSE FA.
           SORT SN ON ASCENDING KEY SN-K USING FA
               OUTPUT PROCEDURE SHOW-N.
           SORT SG ON ASCENDING KEY SG-SHORT
               INPUT PROCEDURE REL-SHORT
               GIVING GNUM GNAT GMUL GALN.
           OPEN OUTPUT FN.
           MOVE N"XX" TO FN-REC. WRITE FN-REC.
           CLOSE FN.
           OPEN OUTPUT FN2.
           MOVE N"YY" TO FN2-REC. WRITE FN2-REC.
           CLOSE FN2.
           MERGE SM ON ASCENDING KEY SM-K USING FN FN2
               GIVING GNUM2 GMUL2.
           PERFORM DUMP-ALL.
           STOP RUN.
       SHOW-N.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               RETURN SN AT END MOVE "Y" TO W-EOF
               NOT AT END
                   MOVE FUNCTION ORD(SN-X(5:1)) TO W-O
                   DISPLAY "T5=" W-O
                   MOVE FUNCTION ORD(SN-X(6:1)) TO W-O
                   DISPLAY "T6=" W-O
               END-RETURN
           END-PERFORM.
       REL-SHORT.
           MOVE N"1" TO SG-SHORT. RELEASE SG-SHORT.
           MOVE N"9" TO SG-SHORT. RELEASE SG-SHORT.
       DUMP-ALL.
           OPEN INPUT BACK.
           READ BACK AT END DISPLAY "EOF".
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 6
               MOVE FUNCTION ORD(BACK-REC(W-I:1)) TO W-O
               DISPLAY "G1 B" W-I "=" W-O
           END-PERFORM.
           CLOSE BACK.
           OPEN INPUT BACKT.
           READ BACKT AT END DISPLAY "EOF".
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 6
               MOVE FUNCTION ORD(BACKT-REC(W-I:1)) TO W-O
               DISPLAY "G2 B" W-I "=" W-O
           END-PERFORM.
           CLOSE BACKT.
           OPEN INPUT BACKM.
           READ BACKM AT END DISPLAY "EOF".
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 6
               MOVE FUNCTION ORD(BACKM-REC(W-I:1)) TO W-O
               DISPLAY "G3 B" W-I "=" W-O
           END-PERFORM.
           CLOSE BACKM.
           OPEN INPUT BACKA.
           READ BACKA AT END DISPLAY "EOF".
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 6
               MOVE FUNCTION ORD(BACKA-REC(W-I:1)) TO W-O
               DISPLAY "G4 B" W-I "=" W-O
           END-PERFORM.
           CLOSE BACKA.
           OPEN INPUT BACKH.
           READ BACKH AT END DISPLAY "EOF".
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 6
               MOVE FUNCTION ORD(BACKH-REC(W-I:1)) TO W-O
               DISPLAY "M1N B" W-I "=" W-O
           END-PERFORM.
           CLOSE BACKH.
           OPEN INPUT BACKI.
           READ BACKI AT END DISPLAY "EOF".
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 6
               MOVE FUNCTION ORD(BACKI-REC(W-I:1)) TO W-O
               DISPLAY "M1A B" W-I "=" W-O
           END-PERFORM.
           CLOSE BACKI.
