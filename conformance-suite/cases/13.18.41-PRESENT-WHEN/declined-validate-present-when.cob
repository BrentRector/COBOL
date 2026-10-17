*>--- conformance-case
*> id: declined-validate-present-when
*> rules: [GR-13.18.41.1-2, SR-13.18.41.3-1, GR-13.18.41.4-1,
*>    GR-13.18.41.4-4, GR-13.18.41.4-5, GR-13.18.41.4-6, FMT-13.18.41.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.41 PRESENT WHEN, FORMAT 2 (validation) - Annex A.4.14 item 5. The SHARED-CLAUSE half of the
*> pair: format 1 (report-writer) has the SAME spelling and IS supported (A.4.11 item 14; CONFORMANCE.md 5
*> records report writer as Partial with it implemented). The two are told apart by WHERE the clause is
*> written, and this entry is in WORKING-STORAGE. The other half is pinned by the POSITIVE control
*> conformance-suite/cases/_unassigned/declined_rw_present_varying_control.cob, which must keep compiling and running.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLPRES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-F PIC 9.
       01 WS-REC.
          05 WS-A PIC X(4) PRESENT WHEN WS-F = 1.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
