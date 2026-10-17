      *>--- conformance-case
      *> id: pb368-flow-use-name-below-2002
      *> rules: [GR-14.9.49.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The REJECT half of conformance:2002/pb368_flow_use_reentrancy.
      *> EC-FLOW-USE is the exception-name ISO §14.9.49.4 GR2 sets to
      *> exist, and §14.6.13.1.6 Table 13 carries it as Fatal; both
      *> belong to the exception-condition facility ISO/IEC 1989:2002
      *> introduced, so naming it in a declarative below COBOL-2002 is
      *> refused with the introduction-band diagnostic COBOLNET0878.
      *> This fixture carries NO >>TURN directive on purpose: that would
      *> be refused first (COBOLNET0900 — the §7.3 directive facility is
      *> itself 2002+) and the case would pin the directive's gate
      *> instead of the NAME's, which is the thing kb/Work PB368 made
      *> raisable.  The USE format-3 header takes COBOLNET0877 on the
      *> same compile; the .err names 0878 because the exception-name is
      *> this case's subject.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB368N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb368n1-absent.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS1.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC   PIC X(10).
       WORKING-STORAGE SECTION.
       01  FS1      PIC XX VALUE "00".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DFU SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-USE.
       DFU-P.
           DISPLAY "FLOW-USE-HANDLER".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN INPUT F1.
           DISPLAY "AFTER".
           STOP RUN.
