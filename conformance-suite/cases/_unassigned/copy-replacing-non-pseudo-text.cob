*>--- conformance-case
*> id: copy-replacing-non-pseudo-text
*> rules: []
*> source-format: free
*> portable: unverified
*>---
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGCR01.
DATA DIVISION.
WORKING-STORAGE SECTION.
COPY NOSUCHBOOK REPLACING KWRD BY K-NEW.
01 W PIC 9 VALUE 5.
PROCEDURE DIVISION.
MAIN.
    DISPLAY W.
    STOP RUN.
