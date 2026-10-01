      *> reject-at: 2002 2014 2023
      *> The AUTHOR comment paragraph is an obsolete COBOL-85 element
      *> removed at COBOL-2002 (kb/Work R61: accepted at 85, rejected
      *> from 2002; ISO 2023 11.2.1's general format has no such
      *> paragraph). The fixed-form conversion keeps the paragraph
      *> header so the ONE removal gate sees it as it does in free form
      *> (kb/Work PB1494, PB1758).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1758AU.
000300 AUTHOR. SOMEONE.
000400 PROCEDURE DIVISION.
000500     DISPLAY "OK".
000600     STOP RUN.
