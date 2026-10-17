      *>--- conformance-case
      *> id: l1_start_indexed_invalid_key
      *> rules: [GR-9.1.13.5-3, GR-14.9.41.4-17]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.41.4 GR17 e) 2. (INDEXED FILES) — "If the comparison
      *> is not satisfied by any record in the file, the invalid key
      *> condition exists and the execution of the START statement is
      *> unsuccessful." (The identical sentence is GR9 c) for RELATIVE
      *> files; this program exercises the INDEXED arm.)
      *> Two shapes of "not satisfied by any record", plus a control.
      *>   S1  CONTROL — the comparison IS satisfiable, so the START
      *>       succeeds, the NOT INVALID KEY imperative runs (§9.1.14),
      *>       the status is '00', and the following READ NEXT delivers
      *>       the record the START positioned at (§14.9.30.4 GR21 d1).
      *>       Without this line the two invalid-key legs could pass by
      *>       a START that always fails.
      *>   S2  A relation no record can satisfy: KEY > "ZZZ999" over
      *>       AAA100 / BBB200 / CCC300. The INVALID KEY imperative is
      *>       taken. The value: §9.1.13.5 lists the whole invalid-key
      *>       family — '21' is an indexed WRITE/REWRITE sequence
      *>       error, '22' a duplicate key created by a WRITE or
      *>       REWRITE, '24' a WRITE outside the file's boundaries —
      *>       and none of the three can arise from a START, so '23' is
      *>       the only applicable value and §9.1.13.1's "if more than
      *>       one value applies" tie-break is never reached. The
      *>       standard states '23' outright for every START
      *>       invalid-key leg it spells out (GR14, GR18, GR19, GR20,
      *>       GR21).
      *>   S2R "the execution of the START statement is unsuccessful"
      *>       has a consequence in GR7 — "the file position indicator
      *>       is set to indicate that no valid record position has
      *>       been established" — so the next sequential READ is '46'
      *>       (§9.1.13.7 item 6a, "The preceding START statement …
      *>       was unsuccessful"), NOT the at end '10'.
      *>   S3  The other shape: an EMPTY indexed file, where no record
      *>       exists to satisfy any comparison. Same condition, '23'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1STR17.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FX ASSIGN TO "l1str17x.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS X-KEY
               FILE STATUS IS ST-X.
           SELECT FE ASSIGN TO "l1str17e.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS E-KEY
               FILE STATUS IS ST-E.
       DATA DIVISION.
       FILE SECTION.
       FD FX.
       01 X-REC.
          05 X-KEY PIC X(6).
          05 X-VAL PIC X(4).
       FD FE.
       01 E-REC.
          05 E-KEY PIC X(2).
          05 E-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST-X PIC XX.
       01 ST-E PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FX
           MOVE "AAA100" TO X-KEY
           MOVE "VAAA" TO X-VAL
           WRITE X-REC
           MOVE "BBB200" TO X-KEY
           MOVE "VBBB" TO X-VAL
           WRITE X-REC
           MOVE "CCC300" TO X-KEY
           MOVE "VCCC" TO X-VAL
           WRITE X-REC
           CLOSE FX
      *> An indexed file with no records at all.
           OPEN OUTPUT FE
           CLOSE FE
      *> ---- control: a satisfiable comparison -------------------
           OPEN INPUT FX
           MOVE "AAA100" TO X-KEY
           START FX KEY IS >= X-KEY
               INVALID KEY DISPLAY "S1INV=YES"
               NOT INVALID KEY DISPLAY "S1INV=NO"
           END-START
           DISPLAY "S1=" ST-X
           READ FX NEXT AT END CONTINUE END-READ
           DISPLAY "S1K=" X-KEY
      *> ---- no record satisfies the relation --------------------
           MOVE "ZZZ999" TO X-KEY
           START FX KEY IS > X-KEY
               INVALID KEY DISPLAY "S2INV=YES"
               NOT INVALID KEY DISPLAY "S2INV=NO"
           END-START
           DISPLAY "S2=" ST-X
           READ FX NEXT AT END CONTINUE END-READ
           DISPLAY "S2R=" ST-X
           CLOSE FX
      *> ---- no record exists at all -----------------------------
           OPEN INPUT FE
           DISPLAY "EOPEN=" ST-E
           MOVE "AA" TO E-KEY
           START FE KEY IS >= E-KEY
               INVALID KEY DISPLAY "S3INV=YES"
               NOT INVALID KEY DISPLAY "S3INV=NO"
           END-START
           DISPLAY "S3=" ST-E
           CLOSE FE
           STOP RUN.
