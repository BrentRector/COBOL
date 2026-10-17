*>--- conformance-case
*> id: pb725_directive_facility
*> rules: [GR-7.3.18.3-1, GR-7.3.19.4-3, DOC-A.1-117, DOC-A.1-200]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB725P02.
      *> kb/Work PB725 - the ISO 7.3 compiler-directive facility AT ITS INTRODUCING
      *> EDITION. The negative twin is negative/pb725-directive-facility-below-2002.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC:
      *>   N=3      7.3.11 defines PB725-N as 3; 7.3.16's ">>IF PB725-N DEFINED" is
      *>            therefore true, so text-1 (MOVE 3) is compiled and the >>ELSE branch
      *>            is omitted. W-N is PIC 9, so DISPLAY renders one digit.
      *>   THREE    7.3.13 Format 1 - selection subject PB725-N (3) against selection
      *>            object 3 matches, so that >>WHEN's lines are compiled and >>WHEN OTHER
      *>            is omitted.
      *> >>LISTING (7.3.18), >>PAGE (7.3.19) and >>CALL-CONVENTION (7.3.9) contribute
      *> NOTHING to the run: 7.3.18.3 GR1 directs that LISTING be ignored when no source
      *> listing is produced, 7.3.19.4 GR3 gives PAGE no effect in the same case, and no
      *> call convention varies here. Their presence is the point - they must COMPILE.
      *>
      *> Directives sit at COLUMN 8 (column 7 is the fixed-form indicator area).
       >>LISTING ON
       >>PAGE PB725 DIRECTIVE FACILITY AT THE COBOL-2002 FLOOR
       >>CALL-CONVENTION COBOL
       >>DEFINE PB725-N AS 3
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
       >>IF PB725-N DEFINED
           MOVE 3 TO W-N
       >>ELSE
           MOVE 9 TO W-N
       >>END-IF
           DISPLAY "N=" W-N
       >>EVALUATE PB725-N
       >>WHEN 3
           DISPLAY "THREE"
       >>WHEN OTHER
           DISPLAY "OTHER"
       >>END-EVALUATE
           STOP RUN.
