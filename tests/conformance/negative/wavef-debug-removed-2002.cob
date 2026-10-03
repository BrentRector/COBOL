*> reject-at: 2002 2014 2023
*> X3.23-1985 USE FOR DEBUGGING + SOURCE-COMPUTER WITH DEBUGGING MODE — the '85 debug module was deleted by
*> ISO/IEC 1989:2002 and is absent from ISO/IEC 1989:2023 (§8.9 reserved-word table absence @10407-10408; the
*> DEBUG-* register spellings are gone). At --std >=2002 the version-conformance pass rejects the USE FOR
*> DEBUGGING declarative with COBOLNET0902 (registry row use-for-debugging-removed-2002; VCR Table 7 row 7.17); the
*> WITH DEBUGGING MODE clause beside it is obsolete at 2002 and removed at 2014 (row debugging-mode-removed-2014,
*> kb/Work R61). Accepted-and-ACTIVE at --std 85 (the wavef_dbg_proc.cob positive golden).
IDENTIFICATION DIVISION.
PROGRAM-ID. WAVEF-DBG-REMOVED.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 NM PIC X(30).
PROCEDURE DIVISION.
DECLARATIVES.
DBG SECTION.
    USE FOR DEBUGGING ON ALL PROCEDURES.
DBG-BODY.
    MOVE DEBUG-NAME TO NM.
    DISPLAY NM.
END DECLARATIVES.
MAIN SECTION.
P1.
    STOP RUN.
