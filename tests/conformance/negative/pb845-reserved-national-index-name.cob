      *> reject-at: 2002 2014 2023
      *> kb/Work PB845. ISO 8.3.2.1 1): "Reserved words shall not be used
      *> as user-defined words or system-names", and 8.9 reserves
      *> NATIONAL from COBOL-2002. An index-name is a user-defined word,
      *> so INDEXED BY NATIONAL is COBOLNET0901. Before the fix NATIONAL
      *> was exempt from the reservation gate as a supposed function
      *> name and this compiled at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB845NI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 3 INDEXED BY NATIONAL.
       PROCEDURE DIVISION.
           SET NATIONAL TO 1
           STOP RUN.
