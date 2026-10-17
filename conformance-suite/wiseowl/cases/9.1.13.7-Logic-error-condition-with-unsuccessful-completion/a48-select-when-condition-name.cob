*>--- conformance-case
*> id: a48-select-when-condition-name
*> rules: [GR-9.1.13.7-5, GR-13.18.13.4-4, SR-13.18.51.3-1, SR-13.18.51.3-2, SR-13.18.51.3-3, SR-13.18.51.3-4, SR-13.18.51.3-5, SR-13.18.51.3-6, GR-13.18.51.4-1, GR-13.18.51.4-2, GR-13.18.51.4-3, GR-13.18.51.4-4, GR-13.18.51.4-5, FMT-13.18.51.2, DOC-A.1-84, DOC-A.1-85, DOC-A.1-173]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 Annex A.4.8 item 2) "SELECT WHEN clause (13.18.51)" - the module's second item, DECLINED
*> (docs/CONFORMANCE.md section 5, row A.4.8), refused by name with COBOLNET1705. 13.18.51.2's printed
*> general format (RENDERED, PDF p481) is `SELECT WHEN { condition-name-1 | OTHER }` - plain braces, NO
*> choice indicators, so exactly one of the two. This witness is the condition-name-1 arm.
*> Refused rather than accepted inert because 13.18.51.4 GR1/GR2 make the clause decide WHICH record
*> description entry is selected: compiled inert it would silently select the wrong one, and 9.1.13.7
*> rule 5 (I-O status 45, record identification failure) is the path that then cannot be reached.
*> 13.18.51.3 rule 1 admits the clause at the 01 level of the file, linkage, local-storage OR
*> working-storage section; the witness uses working-storage so the refusal is provably NOT keyed to an FD.
IDENTIFICATION DIVISION.
PROGRAM-ID. A48SWC9AL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 REC-A SELECT WHEN COND-A.
   05 R-KIND PIC X.
      88 COND-A VALUE "A".
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
