*>--- conformance-case
*> id: pb780-code-national-constant
*> rules: [SR-13.10.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB780 - a constant-name is admitted where the CODE clause writes literal-1 (13.10.3 SR2) and then
      *> meets the rules of the literal it stands for (13.10.4 GR1): 13.18.12.3 SR1 "Literal-1 shall be an
      *> alphanumeric literal", and KC stands for the NATIONAL literal N"Q". The alphanumeric form is
      *> conformance/2002/pb780_constant_names_name_programs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780CDN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb780cdn.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-K.
       WORKING-STORAGE SECTION.
       01  KC CONSTANT AS N"Q".
       REPORT SECTION.
       RD  R-K CODE IS KC.
       01  DE-K TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT.
           INITIATE R-K.
           GENERATE DE-K.
           TERMINATE R-K.
           CLOSE RPT.
           STOP RUN.
