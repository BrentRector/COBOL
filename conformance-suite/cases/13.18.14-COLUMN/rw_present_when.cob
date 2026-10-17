*>--- conformance-case
*> id: rw_present_when
*> rules: [GR-13.18.14.4-12, GR-13.18.41.1-1, GR-13.18.41.4-1, GR-13.18.41.4-2, FMT-13.18.41.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.41 Format 1 (report-group PRESENT WHEN, 2002) +
      *> §13.18.64 (report-group VARYING, 2002) + the §13.18.14 multiple
      *> COLUMN clause. Conditions are evaluated once per presentation,
      *> BEFORE any LINE processing (§13.18.41.4 GR2); a false condition
      *> processes the entry "as though [it] were omitted" (GR2b):
      *> an absent LINE prints nothing and the NEXT relative line
      *> re-anchors on LINE-COUNTER (the collapse — presentation 2's
      *> TAIL lands directly under ROW-2); an absent printable item
      *> leaves its columns as spaces. The VARYING counter takes FROM
      *> at the first COLUMN repetition (§13.18.64.4 GR3a — re-evaluated
      *> per presentation: FROM WS-SEQ), then += BY per repetition
      *> (GR3b), and is the SOURCE item (GR4 NOTE). Placement:
      *> §13.18.35.4 GR5b3 (first body line on the page → FIRST
      *> DETAIL) / GR7 (subsequent relative lines → LINE-COUNTER + n).
      *> PLACEMENT (kb/Work PB484). The report's FIRST printed line is the first line of the file:
      *> a line number IS a page line number (ISO 13.18.35.4 GR6 - "the report's LINE-COUNTER is set
      *> equal to that line number and the line is now printed on the page at that vertical location"),
      *> and GR7's "Any unoccupied lines on the page result in a blank line" fixes the blanks above it.
      *> Here the page heading's LINE 1 is absolute, so GR5 a) prints PRESENT-WHEN on line 1 - the first
      *> line of the file, with nothing above it - and the first body group takes FIRST DETAIL 2 (GR5 b) 3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWPWP10RP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "rwpw-p10rp.rpt".
           SELECT RBACK ASSIGN TO "rwpw-p10rp.rpt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-PW.
       FD RBACK.
       01 RB-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01 WS-SEQ  PIC 9 VALUE 0.
       01 WS-FLAG PIC 9 VALUE 0.
       01 WS-EOF  PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-PW PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 15.
       01 TYPE PH.
          02 LINE 1.
             03 COLUMN 1 PIC X(12) VALUE "PRESENT-WHEN".
       01 DET-A TYPE DE.
          02 LINE PLUS 1.
             03 COLUMN 1 PIC X(4) VALUE "ROW-".
             03 COLUMN 5 PIC 9 SOURCE IS WS-SEQ.
             03 COLUMN 8 PIC X(3) VALUE "OPT"
                PRESENT WHEN WS-FLAG = 1.
             03 COLUMNS ARE 12 16 20 PIC Z9 SOURCE IS RV-IDX
                VARYING RV-IDX FROM WS-SEQ BY 2.
          02 LINE PLUS 1 PRESENT WHEN WS-FLAG = 1.
             03 COLUMN 1 PIC X(9) VALUE "EXTRA-ROW".
          02 LINE PLUS 1.
             03 COLUMN 1 PIC X(4) VALUE "TAIL".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-PW.
           MOVE 1 TO WS-SEQ.
           MOVE 1 TO WS-FLAG.
           GENERATE DET-A.
           MOVE 2 TO WS-SEQ.
           MOVE 0 TO WS-FLAG.
           GENERATE DET-A.
           TERMINATE R-PW.
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
