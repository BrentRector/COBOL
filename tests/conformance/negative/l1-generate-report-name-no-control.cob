*> reject-at: 85 2002 2014 2023
*> ISO §14.9.16.3 SR2 (cite.py OK): "Report-name-1 may be used only if the referenced report description entry
*> contains a CONTROL clause." RD L1GNR has NO CONTROL clause, so the report-name form GENERATE L1GNR (summary
*> reporting, §14.9.16.4) is a syntax-rule violation and must be rejected at compile time. The data-name form
*> GENERATE L1GNR-D on the same report is legal (SR1) and is included so the ONLY violation is SR2's.
*> Report Writer and both GENERATE forms exist in every edition, so every edition rejects.
*> The .err pins the clause citation the diagnostic carries, not its code: the code is COBOLNET0899 today and
*> kb/Work PB1028 owes it a syntax-rule code of its own.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1GNRNC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "L1GNRNC.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS L1GNR.
       REPORT SECTION.
       RD  L1GNR PAGE LIMIT 20 LINES.
       01  L1GNR-D TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "DLINE".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE L1GNR.
           GENERATE L1GNR-D.
           GENERATE L1GNR.
           TERMINATE L1GNR.
           CLOSE PRT.
           STOP RUN.
