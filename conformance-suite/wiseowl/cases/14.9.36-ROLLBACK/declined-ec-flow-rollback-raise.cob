*>--- conformance-case
*> id: declined-ec-flow-rollback-raise
*> rules: [GR-14.9.36.4-2]
*> source-format: free
*> portable: unverified
*>---
*> Annex A.4.3 item 3, the RAISE arm: EC-FLOW-ROLLBACK is level-3 and Fatal in the 14.6.13.1.6 table, and
*> had ZERO setting sites - a RAISE of it compiled clean against a facility that does not exist.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLECFR.
       PROCEDURE DIVISION.
           RAISE EXCEPTION EC-FLOW-ROLLBACK.
           STOP RUN.
