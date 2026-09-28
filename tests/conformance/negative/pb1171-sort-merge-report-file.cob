      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1171 - a REPORT file named as a SORT GIVING file and
      *> as a MERGE USING file.
      *> RULE (14.9.40.3 SR8): "File-name-2 and file-name-3 shall be
      *> described in a file description entry that is not for a report
      *> file and is not a sort-merge file description entry."
      *> RULE (14.9.24.3 SR9): "File-name-2, file-name-3, and file-name-4
      *> shall be described in a file description entry that is not for
      *> a report file and is not a sort-merge file description entry."
      *> cite.py --check 14.9.40.3 "File-name-2 and file-name-3 shall be
      *>   described in a file description entry that is not for a
      *>   report file" -> OK 8)
      *> cite.py --check 14.9.24.3 "File-name-2, file-name-3, and
      *>   file-name-4 shall be described in a file description entry
      *>   that is not for a report file" -> OK 9)
      *> Before the fix only the sort-merge half was asked; both
      *> statements compiled clean. Each is COBOLNET2577.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1171N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1171N2.RPT".
           SELECT IN1 ASSIGN TO "PB1171N2.IN1".
           SELECT IN2 ASSIGN TO "PB1171N2.IN2".
           SELECT SW ASSIGN TO "PB1171N2.SW".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS RPT.
       FD IN1.
       01 IN1-REC PIC X(3).
       FD IN2.
       01 IN2-REC PIC X(3).
       SD SW.
       01 SW-REC.
          05 K1 PIC X(3).
       REPORT SECTION.
       RD RPT PAGE LIMIT 60.
       01 DL TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
           SORT SW ASCENDING K1 USING IN1 GIVING RPF
           MERGE SW ASCENDING K1 USING IN1 RPF GIVING IN2
           STOP RUN.
