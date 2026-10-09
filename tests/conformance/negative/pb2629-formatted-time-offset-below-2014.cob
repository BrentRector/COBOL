      *> reject-at: 85 2002
      *> kb/Work PB2629 - the negative twin of the 2014 golden
      *> pb2629_leap_second_utc_offset: FORMATTED-TIME (ISO 15.41) and its
      *> UTC offset argument are a COBOL-2014 introduction, so below 2014
      *> the function is the edition-introduction diagnostic (COBOLNET1502),
      *> never compiled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2629NFT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC 9(5) VALUE 3600.
       PROCEDURE DIVISION.
           DISPLAY FUNCTION FORMATTED-TIME("hhmmssZ", S, 0).
           STOP RUN.
