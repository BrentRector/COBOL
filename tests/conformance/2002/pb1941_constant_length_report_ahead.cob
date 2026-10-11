      *> kb/Work PB1941 - a constant's LENGTH OF / BYTE-LENGTH OF may measure an elementary report item (13.10.3
      *> SR11: "Data-name-1 and data-name-2, if defined in the report section, shall reference elementary report
      *> items") wherever the constant is evaluated - before the REPORT SECTION is reached, in an earlier report
      *> group, earlier in the same group, or by the OCCURS clause of the entry the item is subordinate to. 13.10.3
      *> SR4 forbids only a length that depends on the constant. Expected values (13.10.4 GR6 "determined as
      *> specified in the LENGTH intrinsic function", GR5 the BYTE-LENGTH one), each item's description as written:
      *>   KR = 3   LENGTH OF R, PIC X(3) - demanded by the FILE SECTION record OREC, so OREC is 3 characters
      *>   KD = 4   LENGTH OF D1, PIC X(4) - demanded by H1 in the page heading group before DL
      *>   KB = 6   LENGTH OF B, PIC X(6) - demanded by A, earlier in the same line
      *>   KO = 2   LENGTH OF E, PIC X(2) - demanded by G's OCCURS clause, E subordinate to G
      *>   KS = 4   LENGTH OF SG, PIC S9(3) SIGN LEADING SEPARATE - the separate sign is a character position
      *>   KN = 6   BYTE-LENGTH OF EN OF GN, PIC N(3) in a group written USAGE NATIONAL: 2 bytes per position
      *> The negative half: negative/pb1941-constant-length-report-cycle (SR4's circular shape in a report entry).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1941RPA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1941RPA.TXT".
           SELECT OTF ASSIGN TO "PB1941RPA.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       FD  OTF.
       01  OREC PIC X(KR).
       WORKING-STORAGE SECTION.
       01  WN PIC 9(3) VALUE 7.
       REPORT SECTION.
       RD  RP PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3.
       01  PH-G TYPE PH LINE 1.
           03  H1 COLUMN 1 PIC X(KD) VALUE "HEAD".
       01  DL TYPE DE LINE PLUS 1.
           03  A COLUMN 1 PIC X(KB) VALUE "AAAAAA".
           03  B COLUMN 10 PIC X(6) VALUE "BBBBBB".
           03  R COLUMN 20 PIC X(3) VALUE "RRR".
           03  D1 COLUMN 25 PIC X(4) VALUE "DATA".
           03  X COLUMN 30 PIC X(KS) VALUE "XXXX".
           03  SG COLUMN 35 PIC S9(3) SIGN LEADING SEPARATE SOURCE WN.
       01  DO TYPE DE LINE PLUS 1.
           03  G OCCURS KO STEP 5.
               05  E COLUMN 1 PIC X(2) VALUE "EE".
       01  DN TYPE DE LINE PLUS 1.
           03  GN USAGE NATIONAL.
               05  EN COLUMN 1 PIC N(3) VALUE N"XYZ".
       01  KD CONSTANT AS LENGTH OF D1.
       01  KB CONSTANT AS LENGTH OF B.
       01  KR CONSTANT AS LENGTH OF R.
       01  KO CONSTANT AS LENGTH OF E.
       01  KS CONSTANT AS LENGTH OF SG.
       01  KN CONSTANT AS BYTE-LENGTH OF EN OF GN.
       PROCEDURE DIVISION.
           DISPLAY "KR=" KR " KD=" KD " KB=" KB " KO=" KO " KS=" KS
               " KN=" KN " OREC=" FUNCTION LENGTH(OREC)
           STOP RUN.
