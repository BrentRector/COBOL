      *> kb/Work PB2516 - constant entries BETWEEN records are legal and end the record before them without
      *> touching it. ISO 13.5.2 (cite.py OK): the working-storage section is a repeated choice of 77-level entry,
      *> constant-entry, record-description-entry and type-declaration-entry. ISO 13.18.45.3 SR2 (cite.py OK): the
      *> RENAMES entry R immediately follows D's last entry, so it belongs to D. ISO 13.11.1 (cite.py OK): E begins at
      *> level 1. Expected, from the rules: LENGTH OF D is 2 (A and B only), R is "AB", E is "CCC" and its 88 is true,
      *> S takes K's value 3 and K2 is 4. A walk that leaves D open across K would still print these values, so the
      *> negatives pb2516-constant-ends-record and pb2516-renames-after-constant pin the boundary itself.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2516CONSTOK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D.
           05  A PIC X VALUE "A".
           05  B PIC X VALUE "B".
       66  R RENAMES A THRU B.
       01  K CONSTANT AS 3.
       01  E.
           05  C PIC X(3) VALUE "CCC".
               88  C-SET VALUE "CCC".
       01  K2 CONSTANT AS 4.
       77  S PIC 9 VALUE K.
       77  L PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION LENGTH(D) TO L.
           DISPLAY L " " R " " E " " S " " K2.
           IF C-SET DISPLAY "SET" END-IF.
           STOP RUN.
