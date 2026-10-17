*>--- conformance-case
*> id: declined-validate-val-status
*> rules: [SR-13.18.62.3-9]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.62.3 SR9: "The words VAL-STATUS and VALIDATE-STATUS are equivalent." The ABBREVIATED spelling
*> is a separate witness because it is a separate lexer token and a separate grammar alternative - a fix
*> that reached only VALIDATE-STATUS would leave this one a generic parse error (feedback_two_arm_dispatch).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLVALST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC.
          05 WS-A PIC X(4).
       01 WS-MSG PIC X(30) VAL-STATUS IS "ERR" WHEN ERROR FOR WS-A.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
