*>--- conformance-case
*> id: pb208-table-value-alphanumeric-on-numeric
*> rules: [SR-13.18.63.3-2, SR-13.18.63.3-16]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB208 half 1 - THE FORMAT 2 (TABLE) VALUE LITERAL IS SCREENED BY SR2 LIKE ANY OTHER.
      *> ISO 13.18.63.3 SR2 is an ALL FORMATS rule: "If the category of the subject of the entry is numeric,
      *> all literals in the VALUE clause shall be numeric and shall be permissible values within the range
      *> indicated by the PICTURE clause or the USAGE clause".  SR16 carries SRs 10-15 into format 2 as well
      *> ("Syntax rules 10, 11,12,13,14,and 15 above apply"), so a table VALUE literal is governed exactly as
      *> a format 1 one.  It was not: DataBinder.BuildTableValueSpecs' per-occurrence literals never reached
      *> ValidateValueCategory, so THIS program compiled CLEAN at strict 2023 (and seeded zeros) while its
      *> format 1 twin - tests/conformance/negative/pb94-value-alphanumeric-on-numeric.cob - was COBOLNET1657.
      *> One funnel now serves both formats (DataBinder.ScreenValueLiteral).
      *>
      *> ALL FOUR EDITIONS reject, and COBOLNET1657 is present at every one of them: SR2 is edition-
      *> independent, so it is the .err substring here.  At COBOL-85 a SECOND diagnostic also fires - the
      *> Format 2 (table) VALUE clause is a COBOL-2002 addition, so the introduction gate reports COBOLNET0900
      *> as well (that arm is pinned on its own by ValueFormat2Tests.BelowIntroduction_Rejected0900).
      *>
      *> A digits-only alphanumeric literal on a numeric item is the CCVS vendor leniency: an error at strict
      *> (this file) and a warning plus the numeric store under --permissive, exactly as on format 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB208N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC X(2) VALUE "AA".
          05 B PIC 9(4) COMP OCCURS 2 VALUE "0012" FROM (1) TO (2).
          05 C PIC X(2) VALUE "CC".
       01 R REDEFINES G PIC X(8).
       PROCEDURE DIVISION.
           DISPLAY B(1) B(2) C.
           STOP RUN.
