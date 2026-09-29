      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1394 - ISO 8.3.5 rule 2: "The COBOL characters comma
      *>  and semicolon, immediately followed by a space, are
      *>  separators". N,M and N;M therefore hold no separator. Both
      *>  used to compile as two receivers and print 001001.
      *>  COBOLNET2631 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGW73ACM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(3).
       01 M PIC 9(3).
       PROCEDURE DIVISION.
           MOVE 1 TO N,M.
           MOVE 1 TO N;M.
           DISPLAY N M.
           STOP RUN.
