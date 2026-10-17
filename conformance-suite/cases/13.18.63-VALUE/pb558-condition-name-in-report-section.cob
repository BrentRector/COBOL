*>--- conformance-case
*> id: pb558-condition-name-in-report-section
*> rules: [SR-13.18.63.3-30]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR30 - "Condition-name and content-validation formats shall not be specified in
*> the report section." This is the WITNESS for that rule, and the rule is enforced as a CONSEQUENCE of two
*> others rather than by a screen of its own: SR33 confines formats 3 and 5 to a level-number of 88, and
*> 13.18.33.3 SR4 - "Report group description entries that are subordinate to an RD entry shall have
*> level-numbers with the values 1 through 49" - refuses 88 in this section (COBOLNET1746, LevelNumberPass).
*> The conjunction is exact, so writing a third screen would report the same line twice.
*> kb/Work PB558 measured the hole this closes: the report-entry walk had no level-88 arm, so the entry was
*> neither rejected nor registered but DROPPED, and a program that went on to reference the condition-name
*> compiled and then aborted at RUN time with a not-implemented exception. PB485 (f37da577b, 2026-09-05) gave
*> the level-number a domain and closed it; this test is what keeps it closed.
*> No edition changes the rule - Annex E carries no 2023 change to SR30 or to SR4, and neither screen has a
*> version predicate - so all four editions reject.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB558RPT88.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT PRT ASSIGN TO "pb558rpt88.txt".
DATA DIVISION.
FILE SECTION.
FD PRT REPORT IS R1.
WORKING-STORAGE SECTION.
01 W PIC 9 VALUE 3.
REPORT SECTION.
RD R1.
01 DT TYPE DETAIL.
   88 D-ON VALUE "X".
   05 LINE 1.
      10 PIC 9 SOURCE W.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT PRT
    INITIATE R1
    GENERATE DT
    TERMINATE R1
    CLOSE PRT
    STOP RUN.
