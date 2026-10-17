*>--- conformance-case
*> id: declined-ec-validate-raise
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> Annex A.4.14 item 10, the RAISE arm - a LEVEL-3 name (EC-VALIDATE-CONTENT), so it passes the 14.9.29.3
*> SR1 level check and reaches the declined-module refusal rather than COBOLNET0710.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLECVR.
       PROCEDURE DIVISION.
           RAISE EXCEPTION EC-VALIDATE-CONTENT.
           STOP RUN.
