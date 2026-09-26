      *> ISO §13.4.5.4 GR2 e) — two programs' EXTERNAL report FDs, both with a REPORT clause, share one connector
      *> RULE (§13.4.5.4 2) e), cite.py OK): "If any of the file
      *> description entries has a REPORT clause, all shall have a REPORT
      *> clause." Main L1FDRMN and the separately described L1FDRSB each
      *> describe the external file L1FDR-F with a REPORT clause (each naming
      *> its own report, RM and RS, of equal 8-character line width, so
      *> GR2 d)'s record size also agrees). The run unit OBEYS GR2, so it
      *> must work: one external file connector (§13.18.22.4 4) a),
      *> cite.py OK), opened ONLY by the main; the sub INITIATEs,
      *> GENERATEs and TERMINATEs its own report on that already-open
      *> connector. This golden witnesses only that two agreeing
      *> REPORT-clause FDs are accepted and run. It does NOT prove the
      *> connector is shared: had it not been, the sub's INITIATE would
      *> set EC-REPORT-FILE-MODE (§14.9.21.4 3), cite.py OK), and with
      *> that checking off (the default) whether execution continues is
      *> the implementor's (§14.6.13.1.3 8)), so the same stdout could
      *> result. The report file's record structure is the implementor's
      *> (§13.4.5.4 4)), so the file content is not compared; the stdout
      *> checkpoints are:
      *>   MAIN-GEN   after the main's GENERATE
      *>   SUB-DONE   after the sub's INITIATE/GENERATE/TERMINATE
      *>   E-DONE     after the main's TERMINATE and CLOSE
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDRMN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT L1FDR-F ASSIGN TO "L1FDRR.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD L1FDR-F IS EXTERNAL
           REPORT IS RM.
       REPORT SECTION.
       RD RM.
       01 RM-D TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(8) VALUE "MAINLINE".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT L1FDR-F.
           INITIATE RM.
           GENERATE RM-D.
           DISPLAY "MAIN-GEN".
           CALL "L1FDRSB".
           TERMINATE RM.
           CLOSE L1FDR-F.
           DISPLAY "E-DONE".
           STOP RUN.
       END PROGRAM L1FDRMN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDRSB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT L1FDR-F ASSIGN TO "L1FDRR.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD L1FDR-F IS EXTERNAL
           REPORT IS RS.
       REPORT SECTION.
       RD RS.
       01 RS-D TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(8) VALUE "SUB-LINE".
       PROCEDURE DIVISION.
       SB1.
           INITIATE RS.
           GENERATE RS-D.
           TERMINATE RS.
           DISPLAY "SUB-DONE".
           GOBACK.
       END PROGRAM L1FDRSB.
