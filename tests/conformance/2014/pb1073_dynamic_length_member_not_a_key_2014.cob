      *> ISO 1989:2023 12.4.5.12.3 SR3 and 12.4.5.6.3 SR3 - the LEGAL twin of the pb1073-*
      *> dynamic-length negatives (kb/Work PB1073): "Data-name-1 and data-name-2 shall not reference
      *> a variable-length data item" forbids a dynamic-length item AS A KEY and says nothing about
      *> one elsewhere in the record, and 8.5.1.10.3 admits a dynamic-length elementary item
      *> "within the record they are subordinate to". This program is the over-rejection guard - the screen must read the
      *> KEY operand's own attribute and not the record's: the prime key IX-KEY (bytes 1-4) and the
      *> alternate key IX-ALT (bytes 5-6) both PRECEDE the dynamic-length member IX-NOTE, so they
      *> keep a fixed position and a fixed length, and the file binds and runs.
      *> DERIVATION - the expected lines follow from the rules, nothing from the compiler:
      *>  - 9.1.8.2: a sequential READ NEXT on an indexed file is ascending on the key of reference,
      *>    the prime key by default (14.9.30.4 GR31): the records written ZZZZ then AAAA are read
      *>    AAAA, ZZZZ.
      *>  - 14.9.30.4 GR30 and GR32: READ KEY IS IX-ALT returns the record whose IX-ALT equals the
      *>    value moved in; "QQ" was written to the record whose prime key is ZZZZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1073DYNMEMBER2014.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb1073dyn-ix.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-ALT.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY PIC X(4).
          05 IX-ALT PIC X(2).
          05 IX-NOTE PIC X DYNAMIC LENGTH.
       WORKING-STORAGE SECTION.
       01 WS-EOF PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF.
           MOVE "ZZZZ" TO IX-KEY.
           MOVE "QQ" TO IX-ALT.
           MOVE "LAST" TO IX-NOTE.
           WRITE IX-REC.
           MOVE "AAAA" TO IX-KEY.
           MOVE "RR" TO IX-ALT.
           MOVE "FIRST" TO IX-NOTE.
           WRITE IX-REC.
           CLOSE IXF.
           OPEN INPUT IXF.
           PERFORM UNTIL WS-EOF = 1
               READ IXF NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY IX-KEY " " IX-ALT " " IX-NOTE
               END-READ
           END-PERFORM.
           MOVE "QQ" TO IX-ALT.
           READ IXF KEY IS IX-ALT
               INVALID KEY DISPLAY "NOT FOUND"
               NOT INVALID KEY DISPLAY "ALT QQ=" IX-KEY
           END-READ.
           CLOSE IXF.
           STOP RUN.
