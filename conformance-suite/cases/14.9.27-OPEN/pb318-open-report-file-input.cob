*>--- conformance-case
*> id: pb318-open-report-file-input
*> rules: [SR-14.9.27.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.27.3 SR1 - "The OPEN statement for a report file shall not contain the INPUT
*> phrase or the I-O phrase." 13.18.46.3 SR3 says the same from the file's side: the subject of an
*> FD carrying a REPORT clause may be referenced in the procedure division only by USE, the WHEN
*> phrase of a PERFORM, CLOSE, or the OPEN statement with the OUTPUT or EXTEND phrase. Both the
*> INPUT and the I-O open below compiled clean until kb/Work PB318.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB318RPI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "pb318rpi.txt".
       DATA DIVISION.
       FILE SECTION.
       FD RPTF REPORT IS RP1.
       REPORT SECTION.
       RD RP1.
       01 TYPE IS DETAIL.
          05 LINE PLUS 1 COLUMN 1 PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT RPTF
           CLOSE RPTF
           OPEN I-O RPTF
           CLOSE RPTF
           STOP RUN.
