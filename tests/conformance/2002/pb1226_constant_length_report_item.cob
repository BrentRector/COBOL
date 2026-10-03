      *> kb/Work PB1226 - 13.10.3 SR11: "Data-name-1 and data-name-2, if defined in the report section, shall
      *> reference elementary report items" (ISO 13.10, 2002) - so a constant's LENGTH OF / BYTE-LENGTH OF may
      *> measure an ELEMENTARY report item, as the LENGTH / BYTE-LENGTH functions do (13.10.4 GR5/GR6):
      *>   KL = 5   LENGTH OF EL IN RP, EL is PIC X(5) (qualified by its report-name, 8.4.2.2.3 SR4)
      *>   KW = 5   the same item measured from WORKING-STORAGE, which precedes the REPORT SECTION
      *>   KN = 6   BYTE-LENGTH OF EN OF DL, EN is PIC N(3): 2 bytes per national position (the 15.14 value)
      *>   KF = 5   LENGTH OF EF, written BEFORE the report group that describes EF, whose PICTURE is X(KL) -
      *>            KL itself written after EF (13.10.3 SR2 repetition; SR4 forbids only a circular dependence)
      *> The negative half: negative/pb1226-constant-length-report-group (a report group is not elementary).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1226KLR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1226KLR.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       WORKING-STORAGE SECTION.
       01  KW CONSTANT AS LENGTH OF EL.
       01  KN CONSTANT AS BYTE-LENGTH OF EN OF DL.
       REPORT SECTION.
       RD  RP PAGE LIMIT IS 10 LINES.
       01  KF CONSTANT AS LENGTH OF EF.
       01  DL TYPE DE LINE PLUS 1.
           03  EL COLUMN 1 PIC X(5) VALUE "ABCDE".
           03  EN COLUMN 7 PIC N(3) VALUE N"XYZ".
           03  EF COLUMN 11 PIC X(KL) VALUE "QQQQQ".
       01  KL CONSTANT AS LENGTH OF EL IN RP.
       PROCEDURE DIVISION.
           DISPLAY "KL=" KL " KW=" KW " KN=" KN " KF=" KF
           STOP RUN.
