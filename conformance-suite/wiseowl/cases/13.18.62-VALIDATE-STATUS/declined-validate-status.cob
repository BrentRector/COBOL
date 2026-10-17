*>--- conformance-case
*> id: declined-validate-status
*> rules: [SR-13.18.62.3-1, SR-13.18.62.3-2, SR-13.18.62.3-3, SR-13.18.62.3-4, SR-13.18.62.3-5, SR-13.18.62.3-6, SR-13.18.62.3-7, SR-13.18.62.3-8, GR-13.18.62.4-1, GR-13.18.62.4-2, GR-13.18.62.4-3, GR-13.18.62.4-4, GR-13.18.62.4-5, GR-13.18.62.4-6, GR-13.18.62.4-7, GR-13.18.62.4-8, FMT-13.18.62.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.62 VALIDATE-STATUS clause - Annex A.4.14 item 6, written with the FULL general format: the ON
*> phrase's CHOICE INDICATORS (5.2.6.4 - one or more of FORMAT / CONTENT / RELATION, in any order) and the
*> FOR list. FORMAT and RELATION arrive as cobolWord (FORMAT is 8.9-reserved from 2002 but has no lexer
*> token; RELATION is 8.10 context-sensitive "VALIDATE-STATUS clause"), and the 8.9 funnel used to print a
*> FALSE "'FORMAT' is a reserved word" beside the true diagnostic - the .err below is the only code emitted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLVSTAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC.
          05 WS-A PIC X(4).
       01 WS-MSG PIC X(30) VALIDATE-STATUS IS "ERR" WHEN NO ERROR
          ON FORMAT CONTENT RELATION FOR WS-A.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
