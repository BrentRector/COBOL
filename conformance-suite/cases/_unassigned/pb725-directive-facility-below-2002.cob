*>--- conformance-case
*> id: pb725-directive-facility-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The ISO 7.3 compiler-directive FACILITY is a COBOL-2002 introduction - COBOL-85
      *> has no compiler directives at all, so a '>>' line cannot occur in a conforming
      *> COBOL-85 source and every directive word below is the introduction diagnostic
      *> COBOLNET0900, never a silent consume (kb/Work PB725: >>LISTING, >>PAGE, >>DEFINE,
      *> >>CALL-CONVENTION and the >>IF family all compiled CLEAN at --std 85 because the
      *> preprocessor held them in a flat name set with no edition column).
      *>
      *> The 85 edge is DERIVED, not quoted (VCR Table 7's discipline): the repo holds no
      *> 2002 text, so the derivation is the M2 post-85 feature catalog in
      *> docs/ISO2023_CONFORMANCE_PLAN.md plus the landed leap-second-directive-2002 row.
      *> Annex E covers only 2014->2023 and lists none of these, so they predate 2023.
      *>
      *> Directives sit at COLUMN 8 - column 7 is the fixed-form indicator area.
       >>LISTING ON
       >>PAGE PB725 THE 2002 DIRECTIVE FACILITY
       >>CALL-CONVENTION COBOL
       >>DEFINE PB725-N AS 3
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB725N02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
       >>IF PB725-N DEFINED
           MOVE 3 TO W-N
       >>END-IF
           DISPLAY "N=" W-N
           STOP RUN.
