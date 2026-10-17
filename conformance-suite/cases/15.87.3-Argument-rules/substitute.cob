*>--- conformance-case
*> id: substitute
*> rules: [AR-15.87.3-1, RV-15.87.4-2, RV-15.87.4-3, RV-15.87.4-4,
*>    RV-15.87.4-5, FMT-15.87.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.87 SUBSTITUTE — argument-1 with each occurrence of argument-2 replaced by argument-3. Default is
      *> ALL occurrences; FIRST (rule 3.a) / LAST (rule 3.b) restrict to one; ANYCASE (rule 5) folds case; multiple
      *> argument-2/argument-3 pairs apply in one left-to-right pass (rules 3/4). S1 = "ABABAB", S2 = "CAT DOG".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INTRSUBS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S1   PIC X(6)  VALUE "ABABAB".
       01 WS-S2   PIC X(7)  VALUE "CAT DOG".
       01 WS-CASE PIC X(4)  VALUE "aAaA".
       01 WS-R    PIC X(12).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION SUBSTITUTE(WS-S1 "A" "X") TO WS-R.
           DISPLAY "ALL=" WS-R.
           MOVE FUNCTION SUBSTITUTE(WS-S1 FIRST "A" "X") TO WS-R.
           DISPLAY "FIRST=" WS-R.
           MOVE FUNCTION SUBSTITUTE(WS-S1 LAST "A" "X") TO WS-R.
           DISPLAY "LAST=" WS-R.
           MOVE FUNCTION SUBSTITUTE(WS-CASE ANYCASE "a" "-") TO WS-R.
           DISPLAY "ANY=" WS-R.
           MOVE FUNCTION SUBSTITUTE(WS-S2 "CAT" "FISH" "DOG" "BIRD") TO WS-R.
           DISPLAY "MULTI=" WS-R.
           MOVE FUNCTION SUBSTITUTE(WS-S1 "AB" "WXYZ") TO WS-R.
           DISPLAY "GROW=" WS-R.
           STOP RUN.
       END PROGRAM INTRSUBS.
