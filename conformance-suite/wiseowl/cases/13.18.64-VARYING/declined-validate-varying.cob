*>--- conformance-case
*> id: declined-validate-varying
*> rules: [GR-13.18.64.4-2, GR-13.18.64.4-6]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.64 VARYING, the VALIDATION leg - Annex A.4.14 item 8. The second SHARED clause: the
*> report-writer leg (A.4.11 item 20) is implemented and stays so - see the positive control
*> tests/conformance/2023/declined_rw_present_varying_control.cob. 13.18.64.3 SR1 requires an OCCURS clause
*> outside a report group, which is why this entry carries one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLVARY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC.
          05 WS-A PIC X(4) OCCURS 3 TIMES VARYING WS-I FROM 1 BY 1.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
