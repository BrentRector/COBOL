*>--- conformance-case
*> id: ec_external_file_mismatch
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> EC-EXTERNAL-FILE-MISMATCH (ISO §14.8.4.4 / §12.4.5.3 GR1(e); raise point
      *> §14.9.4.4 GR3e; VCR 15). MAIN's file control entry for external file XFM-F is
      *> ORGANIZATION SEQUENTIAL; the CALLed sub's corresponding entry is ORGANIZATION
      *> RELATIVE — GR1(e) requires the same organization for every file control entry
      *> referencing an external file connector in the run unit. Checking enabled in both
      *> elements; at the CALL the activated element's registration detects the entry
      *> conflict, the Fatal condition is set, the call is not successful (GR3h #1 — the
      *> ON EXCEPTION phrase), and the sub never runs. No OPEN is needed: the §14.8.4.4
      *> check is an activation-time conformance check, not an I-O operation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XFMMAIN.
       >>TURN EC-EXTERNAL-FILE-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XFM-F ASSIGN "xfm-data".
       DATA DIVISION.
       FILE SECTION.
       FD XFM-F IS EXTERNAL.
       01 XFM-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "XFMSUB"
               ON EXCEPTION
                   DISPLAY "SUB=[" FUNCTION EXCEPTION-STATUS "]"
               NOT ON EXCEPTION
                   DISPLAY "NO-CONDITION"
           END-CALL
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM XFMMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XFMSUB.
       >>TURN EC-EXTERNAL-FILE-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XFM-F ASSIGN "xfm-data" ORGANIZATION RELATIVE.
       DATA DIVISION.
       FILE SECTION.
       FD XFM-F IS EXTERNAL.
       01 XFM-REC PIC X(10).
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY "SUB-RAN"
           GOBACK.
       END PROGRAM XFMSUB.
