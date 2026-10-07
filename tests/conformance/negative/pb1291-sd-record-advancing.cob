      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1291 - ISO 13.4.6.3 SR4: "A record description entry associated with file-name-1 shall not
      *> be specified in an input-output statement other than following the word FROM or the word INTO."
      *> cite.py: OK 13.4.6.3 4). SRT-N belongs to a record of the SD SRT and is the WRITE's ADVANCING count;
      *> the rule used to be asked only of the record-name slot, so this compiled clean and advanced by
      *> whatever the sort record area held.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1031GPB1291NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT1 ASSIGN TO "w1031gpb1291n.dat".
           SELECT SRT ASSIGN TO "w1031gpb1291ns.dat".
       DATA DIVISION.
       FILE SECTION.
       FD OUT1.
       01 OUT-REC PIC X(4).
       SD SRT.
       01 SRT-REC.
          05 SRT-N PIC 9(2).
          05 SRT-D PIC X(2).
       PROCEDURE DIVISION.
           OPEN OUTPUT OUT1.
           MOVE "WXYZ" TO OUT-REC.
           WRITE OUT-REC AFTER ADVANCING SRT-N LINES.
           CLOSE OUT1.
           DISPLAY "DONE".
           STOP RUN.
