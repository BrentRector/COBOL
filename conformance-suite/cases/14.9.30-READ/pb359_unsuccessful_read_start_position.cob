      *>--- conformance-case
      *> id: pb359_unsuccessful_read_start_position
      *> rules: [GR-14.9.30.4-18, GR-14.9.41.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB359 - every unsuccessful READ and START leaves the
      *> file position indicator invalid, whatever the kind of failure,
      *> on all three organizations.
      *>
      *> "Following the unsuccessful execution of a START statement,
      *> the file position indicator is set to indicate that no valid
      *> record position has been established."
      *>   cite.py: OK  §14.9.41.4 7)  (General rules)
      *> "Unless otherwise specified, at the completion of any
      *> unsuccessful execution of a READ statement, ... the file
      *> position indicator is set to indicate that no valid record
      *> position has been established."
      *>   cite.py: OK  §14.9.30.4 18)  (General rules)
      *> "For a sequential READ statement, if the previous READ or
      *> START statement for the file connector was unsuccessful, then
      *> the READ statement is unsuccessful and the I-O status is set
      *> to '46'"
      *>   cite.py: OK  §14.9.30.4 21)  (General rules)
      *> "The open mode of the file connector referenced by file-name-1
      *> shall be input or I-O." - otherwise '47'.
      *>   cite.py: OK  §14.9.41.4 1) ; §14.9.30.4 2) ; §9.1.13.7 7)
      *>
      *> DERIVATION (each leg fails if an unsuccessful exit skips the
      *> invalidation, or if the invalidation outlives a reposition):
      *>   REL, ACCESS DYNAMIC, records 1 and 2 written OPEN OUTPUT.
      *>   R1 START in OUTPUT mode -> 47 (GR1). R2 READ NEXT in OUTPUT
      *>   mode -> 47 (READ GR2). CLOSE, OPEN INPUT: OPEN establishes
      *>   the position, so R3 READ NEXT -> 00 0001. R4 random READ of
      *>   key 9 -> 23 (GR29); R5 READ NEXT -> 46 (GR18 + GR21); R6
      *>   READ NEXT -> 46 again (an unsuccessful READ). R7 START KEY
      *>   = 2 -> 00, a reposition; R8 READ NEXT -> 00 0002; R9 READ
      *>   NEXT -> 10, the at end condition, so the READ is
      *>   unsuccessful (GR24); R10 READ NEXT -> 46.
      *>   IDX, ACCESS DYNAMIC, keys AAAA and BBBB. X1 START in OUTPUT
      *>   mode -> 47. CLOSE, OPEN INPUT. X2 random READ of ZZZZ -> 23
      *>   (GR32); X3 READ NEXT -> 46; X4 START KEY NOT < AAAA -> 00;
      *>   X5 READ NEXT -> 00 AAAA; X6 START KEY > ZZZZ -> 23 (no
      *>   record satisfies it); X7 READ NEXT -> 46 (START GR7).
      *>   SEQ, records S1 and S2. Q1 READ in OUTPUT mode -> 47. CLOSE,
      *>   OPEN INPUT. Q2 READ -> 00 S1; Q3 -> 00 S2; Q4 -> 10; Q5 ->
      *>   46.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB359FPI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "p359rel.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK FILE STATUS IS RS.
           SELECT IDXF ASSIGN TO "p359idx.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS XK FILE STATUS IS XS.
           SELECT SEQF ASSIGN TO "p359seq.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS QS.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RR PIC X(4).
       FD IDXF.
       01 XR.
          05 XK PIC X(4).
       FD SEQF.
       01 QR PIC X(2).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 RS PIC XX.
       01 XS PIC XX.
       01 QS PIC XX.
       PROCEDURE DIVISION.
       P-REL.
           OPEN OUTPUT RELF
           MOVE 1 TO RK
           MOVE "0001" TO RR
           WRITE RR INVALID KEY DISPLAY "W1 INVALID"
           END-WRITE
           MOVE 2 TO RK
           MOVE "0002" TO RR
           WRITE RR INVALID KEY DISPLAY "W2 INVALID"
           END-WRITE
           MOVE 1 TO RK
           START RELF KEY = RK INVALID KEY CONTINUE
           END-START
           DISPLAY "R1=" RS
           READ RELF NEXT AT END CONTINUE
           END-READ
           DISPLAY "R2=" RS
           CLOSE RELF
           OPEN INPUT RELF
           READ RELF NEXT AT END CONTINUE
           END-READ
           DISPLAY "R3=" RS " " RR
           MOVE 9 TO RK
           READ RELF INVALID KEY CONTINUE
           END-READ
           DISPLAY "R4=" RS
           READ RELF NEXT AT END CONTINUE
           END-READ
           DISPLAY "R5=" RS
           READ RELF NEXT AT END CONTINUE
           END-READ
           DISPLAY "R6=" RS
           MOVE 2 TO RK
           START RELF KEY = RK INVALID KEY CONTINUE
           END-START
           DISPLAY "R7=" RS
           READ RELF NEXT AT END CONTINUE
           END-READ
           DISPLAY "R8=" RS " " RR
           READ RELF NEXT AT END CONTINUE
           END-READ
           DISPLAY "R9=" RS
           READ RELF NEXT AT END CONTINUE
           END-READ
           DISPLAY "R10=" RS
           CLOSE RELF.
       P-IDX.
           OPEN OUTPUT IDXF
           MOVE "AAAA" TO XK
           WRITE XR INVALID KEY DISPLAY "V1 INVALID"
           END-WRITE
           MOVE "BBBB" TO XK
           WRITE XR INVALID KEY DISPLAY "V2 INVALID"
           END-WRITE
           START IDXF KEY NOT < XK INVALID KEY CONTINUE
           END-START
           DISPLAY "X1=" XS
           CLOSE IDXF
           OPEN INPUT IDXF
           MOVE "ZZZZ" TO XK
           READ IDXF INVALID KEY CONTINUE
           END-READ
           DISPLAY "X2=" XS
           READ IDXF NEXT AT END CONTINUE
           END-READ
           DISPLAY "X3=" XS
           MOVE "AAAA" TO XK
           START IDXF KEY NOT < XK INVALID KEY CONTINUE
           END-START
           DISPLAY "X4=" XS
           READ IDXF NEXT AT END CONTINUE
           END-READ
           DISPLAY "X5=" XS " " XK
           MOVE "ZZZZ" TO XK
           START IDXF KEY > XK INVALID KEY CONTINUE
           END-START
           DISPLAY "X6=" XS
           READ IDXF NEXT AT END CONTINUE
           END-READ
           DISPLAY "X7=" XS
           CLOSE IDXF.
       P-SEQ.
           OPEN OUTPUT SEQF
           MOVE "S1" TO QR
           WRITE QR
           MOVE "S2" TO QR
           WRITE QR
           READ SEQF AT END CONTINUE
           END-READ
           DISPLAY "Q1=" QS
           CLOSE SEQF
           OPEN INPUT SEQF
           READ SEQF AT END CONTINUE
           END-READ
           DISPLAY "Q2=" QS " " QR
           READ SEQF AT END CONTINUE
           END-READ
           DISPLAY "Q3=" QS " " QR
           READ SEQF AT END CONTINUE
           END-READ
           DISPLAY "Q4=" QS
           READ SEQF AT END CONTINUE
           END-READ
           DISPLAY "Q5=" QS
           CLOSE SEQF
           STOP RUN.
