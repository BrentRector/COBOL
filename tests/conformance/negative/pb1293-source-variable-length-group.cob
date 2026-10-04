      *> reject-at: 2014 2023
      *> kb/Work PB1293 - ISO 13.18.53.3 SR8: "Identifier-1 shall not reference a variable-length group."   cite.py: OK  13.18.53.3 8)  (Syntax rules)
      *> 8.5.1.12.1: a group with a DYNAMIC LENGTH member is a variable-length group.  Before the screen this reached GENERATE and aborted with a
      *> NotImplementedCobolFeatureException (no whole-group image of a dynamic-length group).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1293SRCVARGROUP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W13PPB1293SRCVARGROUP.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  VG.
           05  VD PIC X DYNAMIC LENGTH.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) SOURCE VG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
