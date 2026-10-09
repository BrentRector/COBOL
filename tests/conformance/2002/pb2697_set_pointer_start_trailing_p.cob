      *> kb/Work PB2697 - a TRAILING-P amount counts by its VALUE in the two COBOL-2002 consumers of
      *> the SET-family amount landing: SET pointer UP/DOWN BY and START ... WITH LENGTH.
      *> ISO 13.18.40.4 GR14: "The symbol 'P' specifies the location of an assumed decimal point when
      *> that point is not within the number that appears in the data item." N10, N20 and N40 (PIC 9P)
      *> store the digits 1, 2 and 4; their values are 10, 20 and 40. The runtime used to hand back the
      *> stored digits for every scale <= 0.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES AND NOT FROM A RUN:
      *> UP=[U]     14.9.39.4 GR20: the address is incremented by 20 character positions - from BUF
      *>            position 1 to position 21, which holds "U".
      *> DOWN=[K]   decremented by 10 - position 11, "K".
      *> L40=INVALID 23
      *>            14.9.41.4 GR14: "If arithmetic-expression-1 does not evaluate to a positive nonzero
      *>            integer that is less than or equal to the length of the associated key, the I-O status
      *>            value ... is set to '23', the invalid key condition exists". 40 > the key's 30.
      *> L20=OK 00  20 <= 30: the temporary area is the key's first 20 characters, "AB01" followed by 16
      *>            spaces, which the record AB01 matches.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2697PS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb2697ps.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY PIC X(30).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       01 BUF PIC X(30) VALUE "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123".
       01 P   USAGE POINTER.
       01 N10 PIC 9P VALUE 10.
       01 N20 PIC 9P VALUE 20.
       01 N40 PIC 9P VALUE 40.
       LINKAGE SECTION.
       01 LW PIC X(1) BASED.
       PROCEDURE DIVISION.
       MAIN-P.
           SET P TO ADDRESS OF BUF.
           SET P UP BY N20.
           SET ADDRESS OF LW TO P.
           DISPLAY "UP=[" LW "]".
           SET P DOWN BY N10.
           SET ADDRESS OF LW TO P.
           DISPLAY "DOWN=[" LW "]".
           OPEN OUTPUT IXF.
           MOVE "AB01" TO IX-KEY.
           WRITE IX-REC.
           CLOSE IXF.
           OPEN INPUT IXF.
           MOVE "AB01" TO IX-KEY.
           START IXF KEY IS EQUAL TO IX-KEY WITH LENGTH N40
               INVALID KEY DISPLAY "L40=INVALID " ST1
               NOT INVALID KEY DISPLAY "L40=OK " ST1
           END-START.
           START IXF KEY IS EQUAL TO IX-KEY WITH LENGTH N20
               INVALID KEY DISPLAY "L20=INVALID " ST1
               NOT INVALID KEY DISPLAY "L20=OK " ST1
           END-START.
           CLOSE IXF.
           STOP RUN.
