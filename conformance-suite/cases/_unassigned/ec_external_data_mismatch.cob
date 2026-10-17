*>--- conformance-case
*> id: ec_external_data_mismatch
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> EC-EXTERNAL-DATA-MISMATCH (ISO §14.8.4.2; raise point §14.9.4.4 GR3e; VCR 15).
      *> §14.8.4.2: for each external file connector, the file status, LINAGE and
      *> relative key data items shall be external data items referring to the same
      *> corresponding storage in each runtime element. MAIN's FD for external file
      *> XDM-F carries LINAGE IS XDM-LN (an external data item); the CALLed sub's FD
      *> carries LINAGE IS 10 (the literal form — no data item at all) — the linage
      *> references are not the same corresponding external item. (The FILE STATUS and
      *> RELATIVE KEY faces of §14.8.4.2 are compile-blocked in one compilation group by
      *> the ≥2023 COBOLNET1573/1575 cross-SELECT checks — VCR 18/31 — so LINAGE is the
      *> in-group-reachable runtime vector.) Checking enabled in both elements; the
      *> Fatal condition is set at the CALL, the call is not successful (GR3h #1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XDMMAIN.
       >>TURN EC-EXTERNAL-DATA-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XDM-F ASSIGN "xdm-data".
       DATA DIVISION.
       FILE SECTION.
       FD XDM-F IS EXTERNAL
           LINAGE IS XDM-LN LINES.
       01 XDM-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 XDM-LN IS EXTERNAL PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "XDMSUB"
               ON EXCEPTION
                   DISPLAY "SUB=[" FUNCTION EXCEPTION-STATUS "]"
               NOT ON EXCEPTION
                   DISPLAY "NO-CONDITION"
           END-CALL
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM XDMMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XDMSUB.
       >>TURN EC-EXTERNAL-DATA-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XDM-F ASSIGN "xdm-data".
       DATA DIVISION.
       FILE SECTION.
       FD XDM-F IS EXTERNAL
           LINAGE IS 10 LINES.
       01 XDM-REC PIC X(10).
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY "SUB-RAN"
           GOBACK.
       END PROGRAM XDMSUB.
