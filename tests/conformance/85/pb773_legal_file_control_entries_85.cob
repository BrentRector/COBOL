      *> ISO 1989:2023 12.4.5.2 SR11 / SR13 and 12.4.5.6.3 SR3 / SR4 - the LEGAL twin of the pb773-*
      *> and pb1073-* negatives, and the over-rejection guard on the rows they add (kb/Work PB773,
      *> PB1073). Every shape those rows could get wrong is here and must BIND AND RUN:
      *>   - a SEQUENTIAL file whose entry writes the clauses 12.4.5.1 prints in Formats 1 to 3
      *>     together - ACCESS MODE IS SEQUENTIAL and FILE STATUS. SR13 refuses them ON AN SD only,
      *>     so on an FD they are Format 3 and must draw nothing.
      *>   - an INDEXED file with a FILE STATUS clause and TWO alternate record keys, none of which
      *>     has the leftmost byte position of the prime key or of the other alternate (SR4): the
      *>     prime key is the group IX-KEY (bytes 1-4), IX-A1 is its SECOND half (byte 3, inside the
      *>     prime key but not its leftmost byte) and IX-A2 is the item after the group (byte 5).
      *>   - a sort-merge file whose SD entry is exactly 12.4.5.1 Format 4, ASSIGN and nothing else.
      *> DERIVATION - every expected line follows from the rules, nothing from the compiler:
      *>  - 9.1.8.2: the order of a sequential READ NEXT on an indexed file is ascending on the
      *>    key of reference, which defaults to the prime key (14.9.30.4 GR31), so the records
      *>    written in the order BBDD, CCAA, AABB are read AABB, BBDD, CCAA.
      *>  - 14.9.30.4 GR30 and GR32: a READ with the KEY phrase makes IX-A1 the key of reference and
      *>    returns the record whose IX-A1 value equals the one moved in. IX-A1 is bytes 3-4 of the
      *>    record: AA (CCAA), BB (AABB), DD (BBDD), so "BB" returns the record whose prime key is
      *>    AABB.
      *>  - 14.9.40.4 GR8 a): SORT's ASCENDING key returns the lower key value first, so the drain
      *>    prints S=1 then S=2 from records released in the order 2, 1.
      *>  - The sequential file is written with one record and read back verbatim: SEQ=HELLO, and
      *>    both FILE STATUS data items read "00" after a successful statement (9.1.13.2 item 1,
      *>    I-O status = 00).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB773LEGALENTRIES85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb773legal-sq.dat"
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-FS1.
           SELECT IXF ASSIGN TO "pb773legal-ix.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-A1
               ALTERNATE RECORD KEY IS IX-A2 WITH DUPLICATES
               FILE STATUS IS WS-FS2.
           SELECT SRT ASSIGN TO "pb773legal-srt.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC PIC X(5).
       FD IXF.
       01 IX-REC.
          05 IX-KEY.
             10 IX-K1 PIC X(2).
             10 IX-A1 PIC X(2).
          05 IX-A2 PIC X(2).
          05 IX-DATA PIC X(2).
       SD SRT.
       01 SR-REC.
          05 SR-K PIC 9.
          05 SR-T PIC X(3).
       WORKING-STORAGE SECTION.
       01 WS-FS1 PIC XX.
       01 WS-FS2 PIC XX.
       01 WS-EOF PIC 9 VALUE 0.
       01 WS-SEOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SQF.
           MOVE "HELLO" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE SQF.
           OPEN INPUT SQF.
           READ SQF AT END CONTINUE END-READ.
           DISPLAY "SEQ=" SQ-REC " FS=" WS-FS1.
           CLOSE SQF.
           OPEN OUTPUT IXF.
           MOVE "BBDD" TO IX-KEY.
           MOVE "Y1" TO IX-A2.
           MOVE "TW" TO IX-DATA.
           WRITE IX-REC.
           MOVE "CCAA" TO IX-KEY.
           MOVE "Y2" TO IX-A2.
           MOVE "TR" TO IX-DATA.
           WRITE IX-REC.
           MOVE "AABB" TO IX-KEY.
           MOVE "Y3" TO IX-A2.
           MOVE "ON" TO IX-DATA.
           WRITE IX-REC.
           CLOSE IXF.
           OPEN INPUT IXF.
           DISPLAY "PRIME WALK:".
           PERFORM UNTIL WS-EOF = 1
               READ IXF NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "  " IX-KEY " " IX-A2 " " IX-DATA
               END-READ
           END-PERFORM.
           MOVE "BB" TO IX-A1.
           READ IXF KEY IS IX-A1
               INVALID KEY DISPLAY "NOT FOUND"
               NOT INVALID KEY DISPLAY "ALT BB=" IX-KEY " FS=" WS-FS2
           END-READ.
           CLOSE IXF.
           SORT SRT ON ASCENDING KEY SR-K
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           DISPLAY "DONE".
           STOP RUN.
       FEED.
           MOVE 2 TO SR-K MOVE "BBB" TO SR-T RELEASE SR-REC.
           MOVE 1 TO SR-K MOVE "AAA" TO SR-T RELEASE SR-REC.
       DRAIN.
           PERFORM UNTIL WS-SEOF = "Y"
               RETURN SRT RECORD
                   AT END MOVE "Y" TO WS-SEOF
                   NOT AT END DISPLAY "S=" SR-K " " SR-T
               END-RETURN
           END-PERFORM.
