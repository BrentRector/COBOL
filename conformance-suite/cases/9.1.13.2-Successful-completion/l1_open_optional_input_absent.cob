      *>--- conformance-case
      *> id: l1_open_optional_input_absent
      *> rules: [GR-9.1.13.2-4, GR-14.9.27.4-13]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.27.4 GR13 — "If the file is not present, and the
      *> INPUT phrase is specified in the OPEN statement, and the
      *> OPTIONAL clause is specified in the file control entry for
      *> file-name-1, the file position indicator in the file connector
      *> referenced by file-name-1 is set to indicate that an optional
      *> input file is not present."
      *> The rule sets a STATE, so it is observed through the three
      *> statements the standard defines over that state, on all three
      *> organizations (the rule names none, so it holds for each):
      *>   OPEN itself — §9.1.13.2 item 4a: "For an OPEN statement, it
      *>     is successfully executed but the file is described as
      *>     optional and the physical file is not present" ⇒ '05'.
      *>   a sequential READ — §14.9.30.4 GR21: "If the file position
      *>     indicator indicates that an optional input file is not
      *>     present … the I-O status value … is set to '10', the at
      *>     end condition exists"; also §9.1.13.4 item 1c.
      *>   a START — §14.9.41.4 GR5: "If, at the time of the execution
      *>     of the START statement, the file position indicator
      *>     indicates that an optional input file is not present, the
      *>     invalid key condition exists and the execution of the
      *>     START statement is unsuccessful", and §9.1.13.5 item 3b
      *>     names the value: "a START or random READ statement is
      *>     attempted on a file described as optional and the physical
      *>     file is not present" ⇒ '23'.
      *> GR13 vs GR17: the INPUT phrase does NOT create the file — GR17
      *> gives creation to EXTEND and I-O only. Q-AGAIN re-opens the
      *> same file-name and gets '05' a second time; had the first OPEN
      *> created it, §9.1.13.2 item 4a could not apply and the value
      *> would be '00'.
      *> Each observation starts from a fresh OPEN, because §14.9.41.4
      *> GR7 and §14.9.30.4 GR24b overwrite the file position indicator
      *> once a START or READ has failed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OPN13.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL FQ ASSIGN TO "l1opn13q.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-Q.
           SELECT OPTIONAL FR ASSIGN TO "l1opn13r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS ST-R.
           SELECT OPTIONAL FX ASSIGN TO "l1opn13x.dat"
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
      *> ---- sequential ------------------------------------------
           OPEN INPUT FQ
           DISPLAY "Q-OPEN=" ST-Q
           READ FQ AT END CONTINUE END-READ
           DISPLAY "Q-READ=" ST-Q
           CLOSE FQ
           DISPLAY "Q-CLOSE=" ST-Q
           OPEN INPUT FQ
           DISPLAY "Q-AGAIN=" ST-Q
           CLOSE FQ
      *> ---- relative --------------------------------------------
           OPEN INPUT FR
           DISPLAY "R-OPEN=" ST-R
           MOVE 1 TO RK
           START FR KEY IS >= RK
               INVALID KEY DISPLAY "R-INV=YES"
               NOT INVALID KEY DISPLAY "R-INV=NO"
           END-START
           DISPLAY "R-START=" ST-R
           CLOSE FR
           OPEN INPUT FR
           DISPLAY "R-AGAIN=" ST-R
           READ FR NEXT AT END CONTINUE END-READ
           DISPLAY "R-READ=" ST-R
           CLOSE FR
      *> ---- indexed ---------------------------------------------
           OPEN INPUT FX
           DISPLAY "X-OPEN=" ST-X
           MOVE "K01" TO X-KEY
           START FX KEY IS >= X-KEY
               INVALID KEY DISPLAY "X-INV=YES"
               NOT INVALID KEY DISPLAY "X-INV=NO"
           END-START
           DISPLAY "X-START=" ST-X
           CLOSE FX
           OPEN INPUT FX
           DISPLAY "X-AGAIN=" ST-X
           READ FX NEXT AT END CONTINUE END-READ
           DISPLAY "X-READ=" ST-X
           CLOSE FX
           STOP RUN.
