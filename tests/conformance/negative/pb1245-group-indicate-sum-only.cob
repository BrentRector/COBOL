      *> reject-at: 85 2002 2014 2023
      *> GROUP INDICATE ON A SUM ENTRY WITH NO SOURCE OR VALUE (kb/Work PB1245 -- COBOLNET2519).
      *> ISO/IEC 1989:2023 §13.18.28.3 SR1: "The GROUP INDICATE clause may be specified only within a
      *> detail report group description, in an elementary entry that also contains a COLUMN clause and a
      *> SOURCE or VALUE clause."
      *>   cite.py --check 13.18.28.3 "in an elementary entry that also contains a COLUMN clause and a
      *>     SOURCE or VALUE clause"  -> OK  §13.18.28.3 1)  (Syntax rule)
      *> The printable item below has a COLUMN clause and a SUM clause, which satisfies §13.15.3 SR10, but
      *> neither a SOURCE nor a VALUE clause, which §13.18.28.3 SR1 requires of an indicated item. It is
      *> the only diagnostic the program draws at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1245NS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB1245NS.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE REPORT IS R.
       WORKING-STORAGE SECTION.
       01  WN PIC 9(3) VALUE 1.
       REPORT SECTION.
       RD  R.
       01  DA TYPE DETAIL LINE PLUS 1.
           03 COLUMN 1 PIC ZZ9 SUM WN GROUP INDICATE.
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT-FILE. INITIATE R.
           GENERATE DA.
           TERMINATE R. CLOSE RPT-FILE. STOP RUN.
