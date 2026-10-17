      *>--- conformance-case
      *> id: l1_open_extend_position
      *> rules: [GR-9.1.13.5-1, GR-14.9.27.4-15, GR-14.9.51.4-17,
      *>    GR-14.9.51.4-18, GR-14.9.51.4-30, GR-14.9.51.4-38]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.27.4 GR15 — "When the EXTEND phrase is specified,
      *> the OPEN statement positions the file immediately after the
      *> last logical record for that file. The last logical record for
      *> a sequential file is the last record written in the file. The
      *> last logical record for a relative file is the currently
      *> existing record with the highest relative record number. The
      *> last logical record for an indexed file is the currently
      *> existing record with the highest prime key value."
      *> One leg per sentence. §14.9.27.3 SR2 confines EXTEND to
      *> sequential access mode, so every EXTEND connector below is
      *> ACCESS SEQUENTIAL.
      *> SEQUENTIAL. SEQ001, SEQ002 exist; the EXTEND session writes
      *> SEQ003, and the read-back order is 001, 002, 003 — the append
      *> landed AFTER the last record written, not at the front.
      *> RELATIVE. The file is seeded SPARSE — records at relative
      *> record numbers 1 and 4 — through a DYNAMIC connector, so
      *> "highest existing relative record number" (4) and "number of
      *> records" (2) differ. §14.9.51.4 GR29a: in extend mode "the
      *> first record released after the OPEN is assigned a record
      *> number that is one greater than the highest relative record
      *> number existing in the physical file", moved into the RELATIVE
      *> KEY item — so RK must read 0005, not 0003. The read-back walks
      *> 1, 4, 5.
      *> INDEXED. K002 and K006 exist. §14.9.51.4 GR38: under extend
      *> "the first record released … shall have a prime record key
      *> whose value is greater than the highest prime record key value
      *> existing in the physical file when it was opened", else the
      *> WRITE "is unsuccessful, the invalid key condition exists, and
      *> the I-O status … is set to '21'" (§9.1.13.5 item 1). K008 is
      *> accepted; a SECOND extend session then offers K004 as its
      *> FIRST released record and is refused with '21', which is only
      *> possible if that OPEN positioned after K008 — the highest
      *> existing prime key — rather than at the beginning. The final
      *> walk shows the refused record never entered the file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OPN15.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FQ ASSIGN TO "l1opn15q.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-Q.
           SELECT FRD ASSIGN TO "l1opn15r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RKD
               FILE STATUS IS ST-D.
           SELECT FRS ASSIGN TO "l1opn15r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RKS
               FILE STATUS IS ST-S.
           SELECT FXS ASSIGN TO "l1opn15x.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS X-KEY
               FILE STATUS IS ST-X.
       DATA DIVISION.
       FILE SECTION.
       FD FQ.
       01 Q-REC PIC X(6).
       FD FRD.
       01 D-REC.
          05 D-VAL PIC X(6).
       FD FRS.
       01 S-REC.
          05 S-VAL PIC X(6).
       FD FXS.
       01 X-REC.
          05 X-KEY PIC X(4).
          05 X-VAL PIC X(3).
       WORKING-STORAGE SECTION.
       01 RKD  PIC 9(4).
       01 RKS  PIC 9(4).
       01 ST-Q PIC XX.
       01 ST-D PIC XX.
       01 ST-S PIC XX.
       01 ST-X PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> ---- sequential ------------------------------------------
           OPEN OUTPUT FQ
           MOVE "SEQ001" TO Q-REC
           WRITE Q-REC
           MOVE "SEQ002" TO Q-REC
           WRITE Q-REC
           CLOSE FQ
           OPEN EXTEND FQ
           DISPLAY "QEXT=" ST-Q
           MOVE "SEQ003" TO Q-REC
           WRITE Q-REC
           DISPLAY "QW=" ST-Q
           CLOSE FQ
           OPEN INPUT FQ
           READ FQ AT END CONTINUE END-READ
           DISPLAY "Q1=" Q-REC
           READ FQ AT END CONTINUE END-READ
           DISPLAY "Q2=" Q-REC
           READ FQ AT END CONTINUE END-READ
           DISPLAY "Q3=" Q-REC
           READ FQ AT END CONTINUE END-READ
           DISPLAY "QE=" ST-Q
           CLOSE FQ
      *> ---- relative: seed a SPARSE file, then extend ------------
           OPEN OUTPUT FRD
           MOVE 1 TO RKD
           MOVE "REL001" TO D-VAL
           WRITE D-REC
           MOVE 4 TO RKD
           MOVE "REL004" TO D-VAL
           WRITE D-REC
           CLOSE FRD
           OPEN EXTEND FRS
           DISPLAY "REXT=" ST-S
           MOVE "REL005" TO S-VAL
           WRITE S-REC
           DISPLAY "RW=" ST-S " RK=" RKS
           CLOSE FRS
           OPEN INPUT FRD
           READ FRD NEXT AT END CONTINUE END-READ
           DISPLAY "D1=" RKD " " D-VAL
           READ FRD NEXT AT END CONTINUE END-READ
           DISPLAY "D2=" RKD " " D-VAL
           READ FRD NEXT AT END CONTINUE END-READ
           DISPLAY "D3=" RKD " " D-VAL
           CLOSE FRD
      *> ---- indexed ---------------------------------------------
           OPEN OUTPUT FXS
           MOVE "K002" TO X-KEY
           MOVE "V02" TO X-VAL
           WRITE X-REC
           MOVE "K006" TO X-KEY
           MOVE "V06" TO X-VAL
           WRITE X-REC
           CLOSE FXS
           OPEN EXTEND FXS
           DISPLAY "XEXT=" ST-X
           MOVE "K008" TO X-KEY
           MOVE "V08" TO X-VAL
           WRITE X-REC
           DISPLAY "XW1=" ST-X
           CLOSE FXS
           OPEN EXTEND FXS
           DISPLAY "XEXT2=" ST-X
           MOVE "K004" TO X-KEY
           MOVE "V04" TO X-VAL
           WRITE X-REC
           DISPLAY "XW2=" ST-X
           CLOSE FXS
           OPEN INPUT FXS
           READ FXS AT END CONTINUE END-READ
           DISPLAY "X1=" X-KEY
           READ FXS AT END CONTINUE END-READ
           DISPLAY "X2=" X-KEY
           READ FXS AT END CONTINUE END-READ
           DISPLAY "X3=" X-KEY
           READ FXS AT END CONTINUE END-READ
           DISPLAY "XE=" ST-X
           CLOSE FXS
           STOP RUN.
