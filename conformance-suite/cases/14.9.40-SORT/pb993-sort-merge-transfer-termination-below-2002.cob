*>--- conformance-case
*> id: pb993-sort-merge-transfer-termination-below-2002
*> rules: [GR-14.9.40.4-12]
*> source-format: free
*> portable: unverified
*>---
      *> The REJECT half of conformance:cases/14.6.13.1.3-Fatal-exception-conditions/pb993_sort_merge_transfer_termination
      *> (kb/Work PB993). The golden's nonfatal-OPEN legs (14.9.24.4 GR7 a),
      *> GR12 a)/b)) are reachable only through a '61' sharing refusal, and the
      *> SHARING clause that produces it (12.4.5.15; Table 19) is an ISO/IEC
      *> 1989:2002 introduction. At COBOL-85 this source does not describe a
      *> program, so the compiler refuses the clause with COBOLNET0900 rather
      *> than merging over a sharing mode the edition does not have.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB993N85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SH-N ASSIGN TO "pb993n85s.dat"
               ORGANIZATION IS SEQUENTIAL SHARING WITH ALL OTHER.
           SELECT SRC-B ASSIGN TO "pb993n85b.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT OUT-G ASSIGN TO "pb993n85g.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SRT-FILE ASSIGN TO "pb993n85w.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD SH-N.
       01 SN-REC   PIC X(3).
       FD SRC-B.
       01 SB-REC   PIC X(3).
       FD OUT-G.
       01 OG-REC   PIC X(3).
       SD SRT-FILE.
       01 SRT-REC  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           MERGE SRT-FILE ON ASCENDING KEY SRT-REC
               USING SH-N SRC-B GIVING OUT-G.
           STOP RUN.
