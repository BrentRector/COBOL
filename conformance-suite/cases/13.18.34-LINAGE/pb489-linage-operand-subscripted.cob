*>--- conformance-case
*> id: pb489-linage-operand-subscripted
*> rules: [SR-13.18.34.3-1, FMT-13.18.34.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO §13.18.34.2 prints data-name-1, and a data-name in a general format is a QUALIFIED-DATA-NAME —
*> §8.4.2.2.2 Format 1, `data-name-1 [ data-qualifier ] … [ file-report-qualifier ]` — which carries no
*> subscript. Subscripting belongs to §8.4.2.3's qualified-data-name-WITH-subscripts, an identifier form, and
*> §13.18.34.3 SR1 forecloses the only reason to write one: "Data-name-1, data-name-2, data-name-3, and
*> data-name-4 shall not be subject to any OCCURS clauses." A subscripted operand is therefore not a spelling
*> of this clause in any edition.
*> ⛔ THE SUBSCRIPT USED TO BE DISCARDED SILENTLY (kb/Work PB489): the capture kept the base word alone, so
*> this source compiled and then killed the process at OPEN OUTPUT with a runtime "not resolvable to storage"
*> throw — a crash where §4.2.2 requires a compile-time indication.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB489N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb489n3.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS T-LINES (2) LINES.
       01 P-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 T-TAB.
          05 T-LINES PIC 99 OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           CLOSE LPF.
           STOP RUN.
