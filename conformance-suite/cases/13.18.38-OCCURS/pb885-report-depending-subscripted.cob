*>--- conformance-case
*> id: pb885-report-depending-subscripted
*> rules: [SR-13.18.38.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> A REPORT-WRITER OCCURS ... DEPENDING ON OPERAND SHALL NOT BE SUBSCRIPTED (kb/Work PB885).
      *> ISO/IEC 1989:2023 §13.18.38.3 SR2 (all formats): "Data-name-1 and data-name-2 shall not be
      *> subscripted." The grammar's shared dataReference parses WS-TE (2), and the binder used to keep the
      *> name and DROP the subscript in silence — this entry printed three repetitions where WS-TE (2) = 2
      *> asked for two. The capture is now the ONE data-name-n screen (COBOLNET2024). The report-writer
      *> OCCURS clause is COBOL-2002, so 1985 rejects it earlier, under the edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB885RDS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb885rds.txt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-TE PIC 9 OCCURS 3 TIMES.
       REPORT SECTION.
       RD RPT.
       01 DTL TYPE IS DETAIL.
          02 LINE 1.
             03 COLUMN 1 PIC X OCCURS 1 TO 3 TIMES
                DEPENDING ON WS-TE (2) STEP 2 VALUE "A".
       PROCEDURE DIVISION.
           MOVE 2 TO WS-TE (2)
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DTL
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
