      *> reject-at: 2002 2014 2023
      *> kb/Work PB2516 - a constant entry ENDS the record description before it. ISO 13.5.2 (cite.py OK) lists
      *> constant-entry and record-description-entry as separate alternatives of the section's entries, and ISO 13.11.1
      *> (cite.py OK): "A record description entry consists of a set of data description entries, the first of which
      *> shall have level-number 1". So K ends D, and 05 B begins a record description that does not start at level 1.
      *> Before the fix B joined D's storage: LENGTH OF D was 2 and DISPLAY D printed AB. (85 has no constant entry.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2516CONSTEND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D.
           05  A PIC X VALUE "A".
       01  K CONSTANT AS 3.
           05  B PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY D.
           STOP RUN.
