      *> reject-at: 2002 2014 2023
      *> kb/Work PB1226 - 13.10.3 SR11: "Data-name-1 and data-name-2, if defined in the report section, shall
      *> reference elementary report items". DL is a report group description entry with a subordinate entry,
      *> so LENGTH OF DL is refused by that rule (it used to be refused as an undefined data-name).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1226NKG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1226NKG.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       REPORT SECTION.
       RD  RP PAGE LIMIT IS 10 LINES.
       01  DL TYPE DE LINE PLUS 1.
           03  EL COLUMN 1 PIC X(5) VALUE "ABCDE".
       01  KG CONSTANT AS LENGTH OF DL.
       PROCEDURE DIVISION.
           DISPLAY KG
           STOP RUN.
