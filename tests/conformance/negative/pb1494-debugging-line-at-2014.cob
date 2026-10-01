      *> reject-at: 2014 2023
      *> ISO 6.2.2 lists no D fixed indicator at COBOL-2023; kb/Work
      *> R61: the debugging line is accepted at 85, obsolete at 2002
      *> and removed at 2014 (kb/Work PB1494).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1494D14.
000300 PROCEDURE DIVISION.
000400     DISPLAY "A".
000500D    DISPLAY "DEBUG".
000600     STOP RUN.
