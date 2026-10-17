*>--- conformance-case
*> id: oo_object_group
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §11.7 / ADR §7 — typed-native object data for GROUPS (→ a per-instance record struct) and
      *> fixed OCCURS tables (→ a per-instance array), proven PER-INSTANCE: FILL mutates R1's PERSON group + TBL
      *> table; R2 (untouched) keeps its defaults. R2 shown first (ANN/0/000), then R1 (BOB/7/123) — a shared/static
      *> group or array would show R2 already filled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOGRP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS REC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1 USAGE OBJECT REFERENCE REC.
       01 R2 USAGE OBJECT REFERENCE REC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE REC "NEW" RETURNING R1.
           INVOKE REC "NEW" RETURNING R2.
           INVOKE R1 "FILL".
           INVOKE R2 "SHOW".
           INVOKE R1 "SHOW".
           STOP RUN.
       END PROGRAM OOGRP.

       IDENTIFICATION DIVISION.
       CLASS-ID. REC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PERSON.
          05 PNAME PIC X(4) VALUE "ANN".
          05 PAGE-N PIC 9 VALUE 0.
       01 TBL.
          05 SLOT PIC 9 OCCURS 3 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. FILL.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "BOB" TO PNAME.
           MOVE 7 TO PAGE-N.
           MOVE 1 TO SLOT(1).
           MOVE 2 TO SLOT(2).
           MOVE 3 TO SLOT(3).
       END METHOD FILL.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "NAME=" PNAME.
           DISPLAY "AGE=" PAGE-N.
           DISPLAY "T=" SLOT(1) SLOT(2) SLOT(3).
       END METHOD SHOW.
       END OBJECT.
       END CLASS REC.
