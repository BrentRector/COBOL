      *>--- conformance-case
      *> id: l1_open_mixed_org_access
      *> rules: [SR-14.9.27.3-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.27.3 SR4 — "The files referenced in the OPEN
      *> statement need not all have the same organization or access."
      *> A PERMISSIVE rule: conformance means the compiler imposes no
      *> agreement it was not asked for. ONE OPEN statement names a
      *> SEQUENTIAL/ACCESS SEQUENTIAL file, a RELATIVE/ACCESS RANDOM
      *> file and an INDEXED/ACCESS DYNAMIC file — three organizations
      *> and three access modes in one list — and it is written twice,
      *> once OUTPUT and once INPUT.
      *> Expected values, derived only from the rule text:
      *>  - SR4 makes the mixed list legal, so the program COMPILES.
      *>  - §14.9.27.4 GR20: the result "is the same as if a separate
      *>    OPEN statement had been written for each file-name in the
      *>    same order", so each file-name gets its OWN I-O status.
      *>  - Table 18 (§14.9.27.4 GR4): OUTPUT on an unavailable file =
      *>    "Open causes the file to be created"; INPUT on an available
      *>    file = "Normal open". Successful, no further information ⇒
      *>    §9.1.13.2 item 1, '00', on every leg.
      *>  - The three records read back prove each connector really was
      *>    associated with its OWN file, not with a shared one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OPN03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FQ ASSIGN TO "l1opn03q.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-Q.
           SELECT FR ASSIGN TO "l1opn03r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS ST-R.
           SELECT FX ASSIGN TO "l1opn03x.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS X-KEY
               FILE STATUS IS ST-X.
       DATA DIVISION.
       FILE SECTION.
       FD FQ.
       01 Q-REC PIC X(6).
       FD FR.
       01 R-REC PIC X(6).
       FD FX.
       01 X-REC.
          05 X-KEY PIC X(3).
          05 X-VAL PIC X(3).
       WORKING-STORAGE SECTION.
       01 RK   PIC 9(4).
       01 ST-Q PIC XX.
       01 ST-R PIC XX.
       01 ST-X PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> ONE OPEN statement over three organizations / three accesses.
           OPEN OUTPUT FQ FR FX
           DISPLAY "OUTQ=" ST-Q
           DISPLAY "OUTR=" ST-R
           DISPLAY "OUTX=" ST-X
           MOVE "QQQQQQ" TO Q-REC
           WRITE Q-REC
           MOVE 2 TO RK
           MOVE "RRRRRR" TO R-REC
           WRITE R-REC
           MOVE "X01" TO X-KEY
           MOVE "XXX" TO X-VAL
           WRITE X-REC
           CLOSE FQ FR FX
           DISPLAY "CLSQ=" ST-Q
           DISPLAY "CLSR=" ST-R
           DISPLAY "CLSX=" ST-X
      *> The same mixed list again, this time INPUT.
           OPEN INPUT FQ FR FX
           DISPLAY "INQ=" ST-Q
           DISPLAY "INR=" ST-R
           DISPLAY "INX=" ST-X
           READ FQ AT END CONTINUE END-READ
           DISPLAY "QREC=" Q-REC
           MOVE 2 TO RK
           READ FR INVALID KEY CONTINUE END-READ
           DISPLAY "RREC=" R-REC
           READ FX NEXT AT END CONTINUE END-READ
           DISPLAY "XREC=" X-KEY X-VAL
           CLOSE FQ FR FX
           STOP RUN.
