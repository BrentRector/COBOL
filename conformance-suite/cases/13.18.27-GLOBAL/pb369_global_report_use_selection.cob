      *>--- conformance-case
      *> id: pb369_global_report_use_selection
      *> rules: [SR-13.18.27.3-1, SR-14.9.46.3-2, GR-14.9.49.4-4,
      *>    GR-14.9.49.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.27.3 SR1 e) permits GLOBAL on a report description
      *> entry; §13.18.27.4 GR1 makes the report-name and every data-name
      *> subordinate to it (its groups, its sum counter TOT-N) global
      *> names, and GR2 lets a directly OR indirectly contained program
      *> reference them without describing them again (kb/Work PB369).
      *> §14.9.49.4 GR4 (FORMATS 1 AND 2) selects the USE BEFORE REPORTING
      *> declarative per STATEMENT: a) the qualifying declarative in the
      *> source element that contains the GENERATE / TERMINATE, else b) a
      *> qualifying declarative with the GLOBAL attribute in the next
      *> inclusive directly containing source element, repeated outward.
      *>   PB369GRA (declares R-1 GLOBAL):
      *>     GLOBAL on DET-1 and TOT, NON-global on DET-2.
      *>   PB369GRB (in A): its own non-global declarative on DET-1.
      *>   PB369GRC (in B): no declaratives at all.
      *> A: GENERATE DET-1 -> a) A's own          "A-GLOBAL DET-1 N=1"
      *>    GENERATE DET-2 -> a) A's own          "A-LOCAL  DET-2 N=1"
      *> B: GENERATE DET-1 -> a) B's own beats A  "B-OWN    DET-1 N=2"
      *>    GENERATE DET-2 -> A's is not GLOBAL, so b) finds none: nothing
      *> C: GENERATE DET-1 -> B's is not GLOBAL, so b) goes on to A
      *>                                          "A-GLOBAL DET-1 N=3"
      *>    TOT-N = 1+1+2+2+3 = 09 (§13.18.54.4 GR7 c) 1. - WS-N is added
      *>    at every GENERATE for the report); PAGE-COUNTER OF R-1 = 1.
      *>    TERMINATE R-1 -> the CF FINAL group TOT: C none, B none, A's
      *>    GLOBAL one (GR4 b) twice removed)     "A-GLOBAL TOT   T=09"
      *> The FD is GLOBAL too: §14.9.16.3 SR3 and §14.9.46.3 SR2 require it
      *> of a contained GENERATE / TERMINATE (the refused shape is
      *> conformance:negative/pb369-contained-report-file-not-global).
      *> The report file is not read back: the LINE SEQUENTIAL
      *> organization that would read it is a COBOL-2023 introduction.
      *> EDITION: Report Writer, nested programs, GLOBAL and USE BEFORE
      *> REPORTING are all COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB369GRA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb369gra.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT GLOBAL REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 0 GLOBAL.
       01 WS-T PIC 99 VALUE 0 GLOBAL.
       REPORT SECTION.
       RD R-1 IS GLOBAL CONTROL IS FINAL
           PAGE LIMIT IS 30 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 25.
       01 DET-1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE IS WS-N.
       01 DET-2 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "X".
       01 TOT TYPE CF FINAL LINE PLUS 1.
          02 TOT-N COLUMN 1 PIC 99 SUM WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       A-D1 SECTION.
           USE GLOBAL BEFORE REPORTING DET-1.
       A-D1-P.
           DISPLAY "A-GLOBAL DET-1 N=" WS-N.
       A-D2 SECTION.
           USE BEFORE REPORTING DET-2.
       A-D2-P.
           DISPLAY "A-LOCAL  DET-2 N=" WS-N.
       A-D3 SECTION.
           USE GLOBAL BEFORE REPORTING TOT.
       A-D3-P.
           MOVE TOT-N TO WS-T.
           DISPLAY "A-GLOBAL TOT   T=" WS-T.
       END DECLARATIVES.
       A-MAIN SECTION.
       A-1.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           MOVE 1 TO WS-N.
           GENERATE DET-1.
           GENERATE DET-2.
           CALL "PB369GRB".
           CLOSE RPT.
           DISPLAY "A DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB369GRB.
       PROCEDURE DIVISION.
       DECLARATIVES.
       B-D1 SECTION.
           USE BEFORE REPORTING DET-1.
       B-D1-P.
           DISPLAY "B-OWN    DET-1 N=" WS-N.
       END DECLARATIVES.
       B-MAIN SECTION.
       B-1.
           MOVE 2 TO WS-N.
           GENERATE DET-1.
           GENERATE DET-2.
           DISPLAY "B GENERATED DET-2".
           CALL "PB369GRC".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB369GRC.
       PROCEDURE DIVISION.
       C-1.
           MOVE 3 TO WS-N.
           GENERATE DET-1.
           MOVE TOT-N TO WS-T.
           DISPLAY "C TOT-N=" WS-T.
           MOVE PAGE-COUNTER OF R-1 TO WS-T.
           DISPLAY "C PAGE-COUNTER=" WS-T.
           TERMINATE R-1.
           EXIT PROGRAM.
       END PROGRAM PB369GRC.
       END PROGRAM PB369GRB.
       END PROGRAM PB369GRA.
