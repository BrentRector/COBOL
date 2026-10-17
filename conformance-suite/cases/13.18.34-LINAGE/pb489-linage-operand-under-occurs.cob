*>--- conformance-case
*> id: pb489-linage-operand-under-occurs
*> rules: [SR-13.18.34.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO §13.18.34.3 SR1 — "Data-name-1, data-name-2, data-name-3, and data-name-4 shall not be subject to any
*> OCCURS clauses." T-LINES is an element of a table, so it IS subject to an OCCURS clause however it is
*> written; this fixture writes it WITHOUT a subscript so the rule is tested on the RESOLVED item rather than
*> on the written shape (its subscripted twin is pb489-linage-operand-subscripted).
*> ⛔ THE RULE HAD NO SITE AT ALL until kb/Work PB489 — a grep for a §13.18.34.3 citation over the whole tree
*> returned nothing. This source compiled clean and then died at OPEN OUTPUT with an unhandled runtime
*> "LINAGE operand 'T-LINES' is not resolvable to storage" throw, which is a process kill where §4.2.2
*> requires the implementation to indicate the violation; the message also cited SR2 for what is SR1's
*> obligation. The rule is present in all four supported editions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB489N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb489n4.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS T-LINES LINES.
       01 P-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 T-TAB.
          05 T-LINES PIC 99 OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           CLOSE LPF.
           STOP RUN.
