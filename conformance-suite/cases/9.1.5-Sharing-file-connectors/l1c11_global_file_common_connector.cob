      *>--- conformance-case
      *> id: l1c11_global_file_common_connector
      *> rules: [GR-9.1.5-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.1.5 2) — a global file-name of a containing (or
      *>   indirectly containing) program names ONE common connector
      *> Rule: "If a program is contained within another program, both
      *>   programs may refer to a common file connector by referring
      *>   to an associated global file-name either in the containing
      *>   program or in any program that directly or indirectly
      *>   contains the containing program."
      *>   cite.py --check 9.1.5 "both programs may refer to a common
      *>   file connector by referring to an associated global
      *>   file-name either in the containing program or in any
      *>   program that directly or indirectly contains the containing
      *>   program" -> OK  §9.1.5 2)  (Sharing file connectors)
      *> L1C11H contains L1C11HB, which contains L1C11HC. GF is GLOBAL
      *> in L1C11H (indirectly contains L1C11HC); GB is GLOBAL in
      *> L1C11HB (the program containing L1C11HC). Only the declaring
      *> program OPENs and CLOSEs each file; the others WRITE through
      *> the connector it opened, which succeeds only if it is common.
      *> Derivation:
      *>   B-W=00     L1C11HB writes BBBB to GF (containing program's).
      *>   C-GF=00    L1C11HC writes CCCC to GF (indirect container's).
      *>   C-GB=00    L1C11HC writes C-01 to GB (its container's).
      *>   GB=B-01    L1C11HB closes GB, reopens it INPUT and reads
      *>   GB=C-01    its own record, then L1C11HC's, then end of file
      *>   GB-EOF=10  (status 10).
      *>   GF=AAAA    L1C11H closes GF, reopens it INPUT: the three
      *>   GF=BBBB    records in write order from all three programs,
      *>   GF=CCCC    then end of file (status 10).
      *>   GF-EOF=10
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11H.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT GF ASSIGN TO "L1C11HF.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS GF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD GF IS GLOBAL.
       01 GF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 GF-ST IS GLOBAL PIC XX.
       01 W-EOF PIC X.
       PROCEDURE DIVISION.
       A-P.
           OPEN OUTPUT GF.
           MOVE "AAAA" TO GF-REC.
           WRITE GF-REC.
           CALL "L1C11HB".
           CLOSE GF.
           OPEN INPUT GF.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               READ GF
                   AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY "GF=" GF-REC
               END-READ
           END-PERFORM.
           DISPLAY "GF-EOF=" GF-ST.
           CLOSE GF.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11HB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT GB ASSIGN TO "L1C11HB.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS GB-ST.
       DATA DIVISION.
       FILE SECTION.
       FD GB IS GLOBAL.
       01 GB-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 GB-ST IS GLOBAL PIC XX.
       01 B-EOF PIC X.
       PROCEDURE DIVISION.
       B-P.
           OPEN OUTPUT GB.
           MOVE "BBBB" TO GF-REC.
           WRITE GF-REC.
           DISPLAY "B-W=" GF-ST.
           MOVE "B-01" TO GB-REC.
           WRITE GB-REC.
           CALL "L1C11HC".
           CLOSE GB.
           OPEN INPUT GB.
           MOVE "N" TO B-EOF.
           PERFORM UNTIL B-EOF = "Y"
               READ GB
                   AT END MOVE "Y" TO B-EOF
                   NOT AT END DISPLAY "GB=" GB-REC
               END-READ
           END-PERFORM.
           DISPLAY "GB-EOF=" GB-ST.
           CLOSE GB.
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11HC.
       PROCEDURE DIVISION.
       C-P.
           MOVE "CCCC" TO GF-REC.
           WRITE GF-REC.
           DISPLAY "C-GF=" GF-ST.
           MOVE "C-01" TO GB-REC.
           WRITE GB-REC.
           DISPLAY "C-GB=" GB-ST.
           EXIT PROGRAM.
       END PROGRAM L1C11HC.
       END PROGRAM L1C11HB.
       END PROGRAM L1C11H.
