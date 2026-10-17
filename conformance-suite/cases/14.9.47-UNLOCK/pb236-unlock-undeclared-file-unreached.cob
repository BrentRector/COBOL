*>--- conformance-case
*> id: pb236-unlock-undeclared-file-unreached
*> rules: [FMT-14.9.47.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB236 - the same 8.4.2.1 verdict as the OPEN case, on an UNREACHED path and through a DIFFERENT
*> one of the seven sites, so the fixture proves the resolution step is SHARED and not per-verb. Before the
*> fix this program compiled AND ran to normal completion printing nothing about NOSUCH at all.
*> UNLOCK is a COBOL-2002 introduction (14.9.47), so the *reject-at* list starts at 2002: at --std 85 the
*> statement draws the introduction gate instead, which is a different rule and a different fixture's job.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB236UNLKUND.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-X PIC X(5).
PROCEDURE DIVISION.
MAIN.
    GO TO SKIPPER.
    UNLOCK NOSUCH.
SKIPPER.
    STOP RUN.
