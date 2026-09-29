      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1394 - ISO 8.3.5 rule 3: "The COBOL character period,
      *>  when followed by a space, is a separator." N.DISPLAY ends no
      *>  sentence; it used to compile and print 005 then 007.
      *>  COBOLNET2632 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGW73APD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(3) VALUE 5.
       01 M PIC 9(3) VALUE 7.
       PROCEDURE DIVISION.
           DISPLAY N.DISPLAY M.
           STOP RUN.
