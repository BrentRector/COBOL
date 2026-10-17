*>--- conformance-case
*> id: concat_below_2002
*> rules: [FMT-8.8.3.1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 8.8.3: the concatenation expression (the & operator joining literals) is a COBOL-2002
*> introduction (concat-operator-2002; roadmap D6). Below 2002 the VersionConformancePass parse arm
*> rejects it on RECOGNITION with the 0900 introduction diagnostic (P10 Step 14 - the audit's
*> concat-at-85 witness).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGCBP10CC.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC X(6).
PROCEDURE DIVISION.
MAIN.
    MOVE "ABC" & "DEF" TO W.
    DISPLAY W.
    STOP RUN.
