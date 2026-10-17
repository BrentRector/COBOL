*>--- conformance-case
*> id: pb1026_external_out_of_line_records
*> rules: [SR-13.18.22.3-4, GR-13.18.22.4-4]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1026 - an EXTERNAL file whose record area holds OUT-OF-LINE records (determination
      *> D-FRA): a dynamic-length record, a variable-length group record and a pointer-class record.
      *> ISO 13.18.22.4 GR4 b): "the data contained in all record description entries subordinate to that
      *> file description entry is external and may be accessed by any runtime element in the run unit
      *> that describes the same file and records as external" - so ALL of the area is one copy per run
      *> unit, not only its character half. Three separately compiled programs describe the file under
      *> DIFFERENT record names (GR5 externalizes the FILE name), and each sees the others' values.
      *> The WORKING-STORAGE twin (ISO 13.18.22.4 GR1): an EXTERNAL group with a dynamic-length member,
      *> whose member used to be a zero-width window of the shared cell (DISPLAY showed "   ").
      *> Expected values:
      *>   B1: B sees A's stores - WG = "ABC"+"HELLO" (ISO 8.5.1.11.2, contiguous), BR2 = "DYNREC",
      *>       BR3 = "007"+"XY"+"ZZ"; B2: the pointer record A set is not NULL in B.
      *>   A1: A sees B's stores - WG2 "WORLD!", AR2 "B-SIDE", AR3B "LONGER"; A2: B set it to NULL.
      *>   A3: FUNCTION LENGTH(WG) = 3 + 6 = 9 (ISO 15.50.4 - the current length of the dynamic item).
      *>   A4: XC READs the record "0123456789" through its own descriptions; in A, AR1 and AR2 hold it
      *>       whole, and AR3 (fixed run 5) splits it by the D-FRA take step: AR3A "012", AR3B "34567"
      *>       (the 5 characters beyond the fixed run), AR3C "89".
      *>   A5: an ADDRESS-OF-taken variable-length group (the same cell-backed storage shape) keeps its
      *>       dynamic member's VALUE and its fixed members: "PQR"+"ab"+"42", LENGTH 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1026XA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "pb1026x.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD EF IS EXTERNAL.
       01 AR1 PIC X(10).
       01 AR2 PIC X DYNAMIC LENGTH LIMIT 20.
       01 AR3.
          05 AR3A PIC 9(3).
          05 AR3B PIC X DYNAMIC LENGTH LIMIT 9.
          05 AR3C PIC X(2).
       01 AP USAGE POINTER.
       WORKING-STORAGE SECTION.
       01 WG EXTERNAL.
          05 WG1 PIC X(3).
          05 WG2 PIC X DYNAMIC LENGTH LIMIT 9.
       01 LG.
          05 LG1 PIC X(3).
          05 LG2 PIC X DYNAMIC LENGTH LIMIT 9 VALUE "ab".
          05 LG3 PIC 9(2) VALUE 42.
       01 T PIC X(4).
       01 P USAGE POINTER.
       01 N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABC" TO WG1
           MOVE "HELLO" TO WG2
           MOVE "DYNREC" TO AR2
           MOVE 7 TO AR3A
           MOVE "XY" TO AR3B
           MOVE "ZZ" TO AR3C
           SET AP TO ADDRESS OF T
           CALL "PB1026XB"
           DISPLAY "A1 [" WG "] [" AR2 "] [" AR3 "]"
           IF AP = NULL
               DISPLAY "A2 NULL"
           ELSE
               DISPLAY "A2 SET"
           END-IF
           MOVE FUNCTION LENGTH(WG) TO N
           DISPLAY "A3 " N
           OPEN OUTPUT EF
           MOVE "0123456789" TO AR1
           WRITE AR1
           CLOSE EF
           CALL "PB1026XC"
           DISPLAY "A4 [" AR1 "] [" AR2 "] [" AR3A "][" AR3B "]["
               AR3C "]"
           SET P TO ADDRESS OF LG
           MOVE "PQR" TO LG1
           MOVE FUNCTION LENGTH(LG) TO N
           DISPLAY "A5 [" LG "] " N
           STOP RUN.
       END PROGRAM PB1026XA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1026XB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "pb1026x.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD EF IS EXTERNAL.
       01 BR1 PIC X(10).
       01 BR2 PIC X DYNAMIC LENGTH LIMIT 20.
       01 BR3.
          05 BR3A PIC 9(3).
          05 BR3B PIC X DYNAMIC LENGTH LIMIT 9.
          05 BR3C PIC X(2).
       01 BP USAGE POINTER.
       WORKING-STORAGE SECTION.
       01 WG EXTERNAL.
          05 WG1 PIC X(3).
          05 WG2 PIC X DYNAMIC LENGTH LIMIT 9.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "B1 [" WG "] [" BR2 "] [" BR3 "]"
           IF BP = NULL
               DISPLAY "B2 NULL"
           ELSE
               DISPLAY "B2 SET"
           END-IF
           MOVE "WORLD!" TO WG2
           MOVE "B-SIDE" TO BR2
           MOVE "LONGER" TO BR3B
           SET BP TO NULL
           GOBACK.
       END PROGRAM PB1026XB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1026XC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "pb1026x.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD EF IS EXTERNAL.
       01 CR1 PIC X(10).
       01 CR2 PIC X DYNAMIC LENGTH LIMIT 20.
       01 CR3.
          05 CR3A PIC 9(3).
          05 CR3B PIC X DYNAMIC LENGTH LIMIT 9.
          05 CR3C PIC X(2).
       01 CP USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT EF
           READ EF
           CLOSE EF
           GOBACK.
       END PROGRAM PB1026XC.
