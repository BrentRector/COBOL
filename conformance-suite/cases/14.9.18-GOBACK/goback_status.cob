*>--- conformance-case
*> id: goback_status
*> rules: [GR-14.9.18.4-10]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.18.2 / §14.9.18.4 GR3 — GOBACK … WITH {ERROR|NORMAL} STATUS [id|lit] (COBOL-2023: annex
      *> item 32 adds to GOBACK the same status phrase STOP allows). In a main program a GOBACK operates as a
      *> STOP statement carrying the status phrase (GR3), so the run unit terminates normally here. The phrase is
      *> parsed, 2023-introduction-gated, and its VALUE wired to the process exit code (§14.9.18.4 GR10 →
      *> RunUnit.ExitStatus → Environment.ExitCode). This stdout golden keeps to NORMAL STATUS 0 (exit 0, which the
      *> golden harness's ExitCode==0 check requires); the nonzero-status exit codes are asserted by
      *> StopGobackExitCodeTests.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GBSTAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  RC  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "STATUS PHRASE OK".
           GOBACK WITH NORMAL STATUS RC.
