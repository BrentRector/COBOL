*>--- conformance-case
*> id: declined_ec_flow_sibling_still_legal
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> THE COMPLEMENT OF THE COBOLNET1710 TABLE (feedback_measure_the_selectors_complement): the refusal
      *> is keyed to EC-FLOW-APPLY-COMMIT / EC-FLOW-COMMIT / EC-FLOW-ROLLBACK, the three names Annex A.4.3
      *> item 3 lists - NOT to the EC-FLOW level-2 family, whose other level-3 names belong to facilities
      *> this compiler DOES implement. A bare "EC-FLOW" prefix in the table would reject this legal program.
      *> EC-FLOW-RELEASE is the sibling chosen because it sits alphabetically between -COMMIT and -ROLLBACK,
      *> so a table that mis-ordered or over-matched its prefixes would take it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLECSIB.
       PROCEDURE DIVISION.
       >>TURN EC-FLOW-RELEASE CHECKING ON
       MAIN.
           DISPLAY "SIBLING-LEGAL".
           STOP RUN.
