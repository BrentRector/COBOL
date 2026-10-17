*>--- conformance-case
*> id: apos_class
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> Audit regression (PHASE-05 Step-1 sweep completion): a SPECIAL-NAMES CLASS defined with APOSTROPHE-delimited
      *> literals (ISO §8.3.3.1 — apostrophe and quotation-mark forms are equal-standing). Before the fix, DataBinder
      *> LiteralChars guarded on '"' only, so '0' THRU '9' populated the class with APOSTROPHE characters instead of
      *> the digit range — a silent miscompile of a valid CLASS clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APOS-CLASS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS MY-DIGIT IS '0' THRU '9'.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS  PIC X(3) VALUE '123'.
       01 WS2 PIC X(3) VALUE 'A2C'.
       PROCEDURE DIVISION.
       MAIN.
           IF WS  IS MY-DIGIT DISPLAY "WS-ALLDIGIT"  ELSE DISPLAY "WS-NO".
           IF WS2 IS MY-DIGIT DISPLAY "WS2-ALLDIGIT" ELSE DISPLAY "WS2-NO".
           STOP RUN.
