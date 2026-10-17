*>--- conformance-case
*> id: declined-ec-flow-apply-commit-use
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> Annex A.4.3 item 3, the USE arm, on the THIRD name of the family - EC-FLOW-APPLY-COMMIT. Its own witness
*> because the table entry that refuses it is a LONGER prefix than EC-FLOW-COMMIT's: a prefix table whose
*> rows shadowed one another would still fire here, but with the wrong facility text, so the message the
*> harness matches is the code and the reasoning is recorded in the table.
*> NOTE the standard writes EC-FLOW-APPLY (no -COMMIT suffix) at 9.1.18.7 and 12.4.6.3.4 GR3/GR4, a name
*> that has NO row in the 14.6.13.1.6 table at all - a defect in the standard, reported not worked around.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLECFA.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-APPLY-COMMIT.
       D-PARA.
           DISPLAY "HANDLER".
       END DECLARATIVES.
       MAIN SECTION.
       M-PARA.
           DISPLAY "UNREACHABLE".
           STOP RUN.
