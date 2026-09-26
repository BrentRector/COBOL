      *> ISO §14.9.35.4 14) — an unsuccessful REWRITE updates no record,
      *> leaves the record area as it was, and sets the I-O status
      *> GR14: "If the execution of the REWRITE statement is
      *>   unsuccessful, no logical record updating takes place, the
      *>   content of the record area is unaffected, and the I-O status
      *>   in the rewrite file connector is updated as indicated in
      *>   General rules 2, 5, 11, 16, 17, 20, 21, 22, 23, and 25."
      *>   cite.py: OK  §14.9.35.4 14)  (General rules)
      *> Six unsuccessful paths, each with a distinct record-area value
      *> moved in just before the REWRITE and a read-back of the file:
      *>   L1 GR3 "If the open mode is some other value or the file is
      *>      not open, the I-O status ... is set to '49'" (OK 3)).
      *>      S is open INPUT.
      *>   L2 GR5 sequential access: "the immediately previous
      *>      input-output statement ... shall have been a successfully
      *>      executed READ statement. If this is not true, the I-O
      *>      status ... is set to '43'" (OK 5)). The previous statement
      *>      is the OPEN I-O.
      *>   L3 GR21 relative random: "If the file does not contain the
      *>      record specified by the key, the invalid key condition
      *>      exists ... '23'" (OK 21)). Key 5 was never written.
      *>   L4 GR22 indexed sequential: the prime key "shall be equal to
      *>      the value of the prime record key of the last record read
      *>      ... If it is not ... '21'" (OK 22)). K1 was read; the
      *>      area is changed to key K2, a record that DOES exist - so
      *>      a wrong update would be visible as K2ZZZZ in the file.
      *>   L5 GR23 indexed random: "If there is no existing record in
      *>      the physical file with that prime record key ... '23'"
      *>      (OK 23)). K9 was never written.
      *>   L6 GR25 c) indexed random, ALTERNATE RECORD KEY without
      *>      DUPLICATES: "When an alternate record key of the record to
      *>      be replaced does not allow duplicates and the value of that
      *>      alternate record key is equal to the value of the
      *>      corresponding alternate record key of a record in that
      *>      physical file, the I-O status ... is set to '22'" (OK
      *>      25)), and "the updating operation does not take place, and
      *>      the content of the record area is unaffected" (OK 25)).
      *>      K1's alternate key is changed from A1 to A2, K2's. This is
      *>      the one listed path where the target record EXISTS under
      *>      a matching prime key, so an update-before-validate would
      *>      show as K1A2ZZ in the file.
      *> Transfer of control: L3-L5 carry INVALID KEY, so the "I" branch
      *>   runs (OK §9.1.14 2)). L1/L2 are fatal-class '4x' statuses
      *>   (OK §9.1.13.1 "Certain classes of I-O status values indicate
      *>   fatal exception conditions"): the implementor decides whether
      *>   the run unit continues; COBOL.NET documents that it continues
      *>   at the end of the statement when a FILE STATUS clause is
      *>   present (docs/CONFORMANCE.md, A.1 item 103).
      *> The read-back after each leg re-reads every record written at
      *>   setup (and, for L3/L5, READs the missing key: '23' again),
      *>   so a leg that updated the file would show a changed value.
      *>   GR13 (OK 13)): the file position indicator is not affected,
      *>   so L2's first READ after the failed REWRITE returns record 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M8RU.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "l1m8ru-s.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS S-ST.
           SELECT R ASSIGN TO "l1m8ru-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS R-KEY
               FILE STATUS IS R-ST.
           SELECT X ASSIGN TO "l1m8ru-x.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS X-KEY
               FILE STATUS IS X-ST.
           SELECT Y ASSIGN TO "l1m8ru-y.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS Y-KEY
               FILE STATUS IS Y-ST.
           SELECT Z ASSIGN TO "l1m8ru-z.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS Z-KEY
               ALTERNATE RECORD KEY IS Z-ALT
               FILE STATUS IS Z-ST.
       DATA DIVISION.
       FILE SECTION.
       FD S.
       01 S-REC PIC X(6).
       FD R.
       01 R-REC PIC X(6).
       FD X.
       01 X-REC.
          05 X-KEY PIC XX.
          05 X-DAT PIC X(4).
       FD Y.
       01 Y-REC.
          05 Y-KEY PIC XX.
          05 Y-DAT PIC X(4).
       FD Z.
       01 Z-REC.
          05 Z-KEY PIC XX.
          05 Z-ALT PIC XX.
          05 Z-DAT PIC XX.
       WORKING-STORAGE SECTION.
       01 S-ST  PIC XX.
       01 R-ST  PIC XX.
       01 X-ST  PIC XX.
       01 Y-ST  PIC XX.
       01 Z-ST  PIC XX.
       01 R-KEY PIC 9(4).
       01 W-BR  PIC X.
       01 W1    PIC X(6).
       01 W2    PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT S.
           MOVE "AAAAAA" TO S-REC. WRITE S-REC.
           MOVE "BBBBBB" TO S-REC. WRITE S-REC.
           CLOSE S.
           OPEN OUTPUT R.
           MOVE 1 TO R-KEY. MOVE "AAAAAA" TO R-REC. WRITE R-REC.
           MOVE 2 TO R-KEY. MOVE "BBBBBB" TO R-REC. WRITE R-REC.
           CLOSE R.
           OPEN OUTPUT X.
           MOVE "K1AAAA" TO X-REC. WRITE X-REC.
           MOVE "K2BBBB" TO X-REC. WRITE X-REC.
           CLOSE X.
           OPEN OUTPUT Y.
           MOVE "K1AAAA" TO Y-REC. WRITE Y-REC.
           MOVE "K2BBBB" TO Y-REC. WRITE Y-REC.
           CLOSE Y.
           OPEN OUTPUT Z.
           MOVE "K1A1AA" TO Z-REC. WRITE Z-REC.
           MOVE "K2A2BB" TO Z-REC. WRITE Z-REC.
           CLOSE Z.
      *> L1 - open mode INPUT: '49'
           OPEN INPUT S.
           READ S.
           MOVE "ZZZZZZ" TO S-REC.
           REWRITE S-REC.
           DISPLAY "L1 " S-ST " [" S-REC "]".
           CLOSE S.
      *> L2 - no successful READ before the REWRITE: '43'
           OPEN I-O S.
           MOVE "YYYYYY" TO S-REC.
           REWRITE S-REC.
           DISPLAY "L2 " S-ST " [" S-REC "]".
           READ S. MOVE S-REC TO W1.
           READ S. MOVE S-REC TO W2.
           DISPLAY "L2 FILE " W1 " " W2.
           CLOSE S.
      *> L3 - relative random, no record 5: '23'
           OPEN I-O R.
           MOVE 5 TO R-KEY.
           MOVE "QQQQQQ" TO R-REC.
           MOVE "-" TO W-BR.
           REWRITE R-REC
               INVALID KEY MOVE "I" TO W-BR
               NOT INVALID KEY MOVE "N" TO W-BR
           END-REWRITE.
           DISPLAY "L3 " R-ST " " W-BR " [" R-REC "]".
           MOVE 1 TO R-KEY. READ R. MOVE R-REC TO W1.
           MOVE 2 TO R-KEY. READ R. MOVE R-REC TO W2.
           MOVE 5 TO R-KEY. READ R.
           DISPLAY "L3 FILE " W1 " " W2 " " R-ST.
           CLOSE R.
      *> L4 - indexed sequential, key changed K1 -> K2: '21'
           OPEN I-O X.
           READ X.
           MOVE "K2ZZZZ" TO X-REC.
           MOVE "-" TO W-BR.
           REWRITE X-REC
               INVALID KEY MOVE "I" TO W-BR
               NOT INVALID KEY MOVE "N" TO W-BR
           END-REWRITE.
           DISPLAY "L4 " X-ST " " W-BR " [" X-REC "]".
           CLOSE X.
           OPEN INPUT X.
           READ X. MOVE X-REC TO W1.
           READ X. MOVE X-REC TO W2.
           DISPLAY "L4 FILE " W1 " " W2.
           CLOSE X.
      *> L5 - indexed random, no record K9: '23'
           OPEN I-O Y.
           MOVE "K9ZZZZ" TO Y-REC.
           MOVE "-" TO W-BR.
           REWRITE Y-REC
               INVALID KEY MOVE "I" TO W-BR
               NOT INVALID KEY MOVE "N" TO W-BR
           END-REWRITE.
           DISPLAY "L5 " Y-ST " " W-BR " [" Y-REC "]".
           MOVE "K1" TO Y-KEY. READ Y. MOVE Y-REC TO W1.
           MOVE "K2" TO Y-KEY. READ Y. MOVE Y-REC TO W2.
           MOVE "K9" TO Y-KEY. READ Y.
           DISPLAY "L5 FILE " W1 " " W2 " " Y-ST.
           CLOSE Y.
      *> L6 - indexed random, duplicate no-DUPLICATES alternate key: '22'
           OPEN I-O Z.
           MOVE "K1A2ZZ" TO Z-REC.
           MOVE "-" TO W-BR.
           REWRITE Z-REC
               INVALID KEY MOVE "I" TO W-BR
               NOT INVALID KEY MOVE "N" TO W-BR
           END-REWRITE.
           DISPLAY "L6 " Z-ST " " W-BR " [" Z-REC "]".
           MOVE "K1" TO Z-KEY. READ Z. MOVE Z-REC TO W1.
           MOVE "K2" TO Z-KEY. READ Z. MOVE Z-REC TO W2.
           DISPLAY "L6 FILE " W1 " " W2.
           CLOSE Z.
           DISPLAY "END".
           STOP RUN.
