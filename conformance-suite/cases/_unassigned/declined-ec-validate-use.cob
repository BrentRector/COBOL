*>--- conformance-case
*> id: declined-ec-validate-use
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> Annex A.4.14 item 10, the USE arm - the THIRD of the six sites the item names, and the one that proves
*> the refusal lives in the ONE written-exception-name funnel (EcNameResolution) rather than in one caller:
*> >>TURN, RAISE and the Format-3 USE declarative reach it by three different paths.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLECVU.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-VALIDATE-FORMAT.
       D-PARA.
           DISPLAY "HANDLER".
       END DECLARATIVES.
       MAIN SECTION.
       M-PARA.
           DISPLAY "UNREACHABLE".
           STOP RUN.
