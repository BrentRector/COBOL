*>--- conformance-case
*> id: declined_rw_present_varying_control
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> THE POSITIVE CONTROL FOR THE SHARED-CLAUSE SPLIT. PRESENT WHEN (ISO 13.18.41) and VARYING
      *> (13.18.64) are listed BOTH by Annex A.4.14 (items 5 and 8 - DECLINED) and by Annex A.4.11 (items 14
      *> and 20 - report writer, which CONFORMANCE.md 5 records as Partial with both IMPLEMENTED). The two
      *> legs are told apart by WHERE the clause is written, not by how it is spelled, so a change that
      *> reached the report-group forms would silently decline a CLAIMED facility. This program writes both
      *> clauses in a REPORT GROUP at the default edition and asserts they still compile AND still produce
      *> their values; conformance:cases/13.18.41-PRESENT-WHEN/declined-validate-present-when and -varying are its complement.
      *> (tests/conformance/2002/rw_present_when.cob is the same shape at 2002 and stays the 2002 leg.)
      *> PLACEMENT (kb/Work PB484). The report's FIRST printed line is the first line of the file:
      *> a line number IS a page line number (ISO 13.18.35.4 GR6 - "the report's LINE-COUNTER is set
      *> equal to that line number and the line is now printed on the page at that vertical location"),
      *> and GR7's "Any unoccupied lines on the page result in a blank line" fixes the blanks above it.
      *> Here the only group is a body group and it is the first on the page, so GR5 b) 3 gives it the
      *> FIRST DETAIL integer, 2: line 1 is unoccupied and blank, ROW-1 prints on line 2, ROW-2 on line 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLRWCTL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "dclrwctl.rpt".
           SELECT RBACK ASSIGN TO "dclrwctl.rpt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-CTL.
       FD RBACK.
       01 RB-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01 WS-SEQ  PIC 9 VALUE 0.
       01 WS-FLAG PIC 9 VALUE 0.
       01 WS-EOF  PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-CTL PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 15.
       01 DET-A TYPE DE.
          02 LINE PLUS 1.
             03 COLUMN 1 PIC X(4) VALUE "ROW-".
             03 COLUMN 5 PIC 9 SOURCE IS WS-SEQ.
             03 COLUMN 8 PIC X(3) VALUE "OPT"
                PRESENT WHEN WS-FLAG = 1.
             03 COLUMNS ARE 12 16 20 PIC Z9 SOURCE IS RV-IDX
                VARYING RV-IDX FROM WS-SEQ BY 2.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-CTL.
           MOVE 1 TO WS-SEQ.
           MOVE 1 TO WS-FLAG.
           GENERATE DET-A.
           MOVE 2 TO WS-SEQ.
           MOVE 0 TO WS-FLAG.
           GENERATE DET-A.
           TERMINATE R-CTL.
           CLOSE RPT.
           OPEN INPUT RBACK.
           PERFORM UNTIL WS-EOF = 1
               READ RBACK
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "L=" RB-REC
               END-READ
           END-PERFORM.
           CLOSE RBACK.
           STOP RUN.
