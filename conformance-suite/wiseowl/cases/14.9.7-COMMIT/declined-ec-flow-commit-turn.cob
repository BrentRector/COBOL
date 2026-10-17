*>--- conformance-case
*> id: declined-ec-flow-commit-turn
*> rules: [GR-14.9.7.4-2]
*> source-format: free
*> portable: unverified
*>---
*> Annex A.4.3 item 3: EC-FLOW-COMMIT in the TURN compiler directive. NOT a bare "EC-FLOW" refusal - the
*> EC-FLOW level-2 family's OTHER level-3 names (EC-FLOW-GLOBAL-EXIT, EC-FLOW-RELEASE, EC-FLOW-RETURN,
*> EC-FLOW-USE, ...) belong to facilities this compiler DOES implement, and declining them would reject
*> legal source; conformance:negative/declined-ec-flow-sibling-still-legal is that complement's witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLECFT.
       PROCEDURE DIVISION.
       >>TURN EC-FLOW-COMMIT CHECKING ON
           DISPLAY "UNREACHABLE".
           STOP RUN.
