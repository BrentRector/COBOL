      *> reject-at: 2002 2014 2023
      *> The REMARKS paragraph is a COBOL-74 carryover absent from
      *> COBOL-2002 and later (kb/Work R61, VCR row 7.10); fixed form
      *> must enforce it as free form does (kb/Work PB1494, PB1758).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1758RM.
000300 REMARKS. THE OLD COMMENT ENTRY.
000400 PROCEDURE DIVISION.
000500     DISPLAY "OK".
000600     STOP RUN.
