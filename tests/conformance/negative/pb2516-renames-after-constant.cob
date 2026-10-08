      *> reject-at: 2002 2014 2023
      *> kb/Work PB2516 - a constant entry ENDS the record description before it (ISO 13.5.2, 13.11.1), so a level-66
      *> entry written after one does not follow the record it renames. ISO 13.18.45.3 SR2 (cite.py OK): "All RENAMES
      *> entries referring to data items within a given record shall immediately follow the last data description
      *> entry of the associated record description entry." Before the fix R silently attached to D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2516RENCONST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D.
           05  A PIC X VALUE "A".
           05  B PIC X VALUE "B".
       01  K CONSTANT AS 3.
       66  R RENAMES A THRU B.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY D.
           STOP RUN.
