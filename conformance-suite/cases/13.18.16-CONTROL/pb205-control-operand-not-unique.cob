*>--- conformance-case
*> id: pb205-control-operand-not-unique
*> rules: [SR-13.18.16.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.16.3 SR6: "Data-name-1 shall be unique in any given CONTROL
      *> clause." Uniqueness is of the WRITTEN reference - SR6's own second
      *> sentence permits two operands to "refer to the same physical data item
      *> or to overlapping data items", so the referenced item cannot be what
      *> distinguishes them, and 13.18.57.3 SR10 could not name a level
      *> unambiguously if it were. CX(1:3) CX(4:3) in the SAME clause is legal
      *> for that reason and is the positive witness (2023/pb205_control_ref_mod);
      *> the same reference twice is not.
      *> MEASURED BEFORE kb/Work PB205: accepted, building two control levels
      *> over one item, so every break at the major level also broke the minor.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB205N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb205n2.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 CX     PIC X(6) VALUE SPACES.
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1 CONTROLS ARE CX CX.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
