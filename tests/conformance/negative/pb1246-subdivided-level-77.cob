      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1246 - ISO 8.5.1.3.2 3) (cite.py OK): noncontiguous data items "are not subdivisions of other items, and are not themselves subdivided", and have the level-number 77;
      *> a level 2-49 entry after a level-77 item therefore has no record to belong to and opens none (13.11.1: the first entry of a record description is level 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1246SUBDIV77.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  X PIC X VALUE "X".
           05  Y PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY X Y.
           STOP RUN.
