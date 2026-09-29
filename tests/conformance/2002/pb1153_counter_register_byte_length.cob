      *> kb/Work PB1153 - FUNCTION BYTE-LENGTH (a COBOL-2002 function) of a
      *> counter register. ISO 15.14.3 r1 admits "a data item of any class or
      *> category", and LINAGE-COUNTER / PAGE-COUNTER are data items
      *> (8.4.3.14.4 GR1, 8.4.3.15.4 GR1). Their implicit description is
      *> PIC 9(d) USAGE DISPLAY (docs/CONFORMANCE.md, the counter registers'
      *> declared capacity), d bytes: 2 for LINAGE IS 20 LINES, 18 for the
      *> report counters. The COBOL-85 witness of the same description is
      *> conformance:85/pb1153_counter_register_character_image.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1153CBL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb1153blpf.txt".
           SELECT RPTF ASSIGN TO "pb1153brpt.txt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 20 LINES.
       01 P-REC PIC X(10).
       FD RPTF REPORT IS R1.
       REPORT SECTION.
       RD R1 PAGE LIMIT 10 LINES.
       01 D1 TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(3) VALUE "DET".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "LNC-BYTES=" FUNCTION BYTE-LENGTH(LINAGE-COUNTER).
           DISPLAY "PC-BYTES=" FUNCTION BYTE-LENGTH(PAGE-COUNTER).
           STOP RUN.
