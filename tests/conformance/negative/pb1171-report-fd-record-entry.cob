      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1171 - a record description entry under the file
      *> description entry of a REPORT file, and a WRITE of it.
      *> RULE (13.4.5.3 SR8): "Format 3 is the file description entry
      *> for a report file. No record description entries or constant
      *> entries shall be associated with the file description entry for
      *> a report file."
      *> RULE (14.9.51.3 SR12): "The file description entry associated
      *> with the write file shall not contain the REPORT clause"
      *> cite.py --check 13.4.5.3 "No record description entries or
      *>   constant entries shall be associated with the file description
      *>   entry for a report file." -> OK 8)
      *> cite.py --check 14.9.51.3 "The file description entry
      *>   associated with the write file shall not contain the REPORT
      *>   clause" -> OK 12)
      *> Before the fix this compiled and the WRITE injected INJECTED
      *> between the report's HELLO lines. The entry is COBOLNET2578;
      *> the WRITE is COBOLNET2577 as well.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1171N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1171N3.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS RPT.
       01 PR PIC X(10).
       REPORT SECTION.
       RD RPT PAGE LIMIT 60.
       01 DL TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
           OPEN OUTPUT RPF
           INITIATE RPT
           GENERATE DL
           MOVE "INJECTED" TO PR
           WRITE PR
           GENERATE DL
           TERMINATE RPT
           CLOSE RPF
           STOP RUN.
