*>--- conformance-case
*> id: pb450-set-false-in-second-group-below-2002
*> rules: [GR-14.9.39.4-8]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB450 - the COBOL-2002 introduction gate on SET condition-name TO FALSE, asked at the position a
*> STATEMENT-level gate cannot see. ISO 14.9.39.2 Format 4 repeats the whole
*> `{ condition-name-1 } ... TO { TRUE | FALSE }` unit, so the FALSE arm here is written in the SECOND group
*> and the first group is a plain COBOL-85 TRUE. The gate reads every group's own keyword
*> (VersionConformancePass.ParseArm.VisitSetBooleanStatement over setConditionPhrase), so it fires; a gate
*> that asked the statement node for its FALSE token after the phrase rule landed would find none and let a
*> COBOL-2002 construct through unmeasured at --std 85.
*> The FALSE arm's COBOL-2002 date is constructs.json set-condition-false-2002 - "COBOL-85's Format 4 has the
*> TRUE arm only" - on the same derived authority as its VALUE-clause half (value-false-phrase-2002); the
*> 2023 Annex E carries no 85->2002 SET row (the A1 authority-sufficiency gap D18).
*> 85 ONLY: at 2002 and above this is conforming source and is the positive
*> conformance-suite/cases/14.9.39-SET/pb450_set_condition_mixed_groups.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB450G2.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-P PIC X VALUE "N".
   88 P-YES VALUE "Y".
01 WS-Q PIC X VALUE "N".
   88 Q-YES VALUE "Y" WHEN SET TO FALSE "N".
PROCEDURE DIVISION.
MAIN-P.
    SET P-YES TO TRUE Q-YES TO FALSE.
    DISPLAY WS-P WS-Q.
    STOP RUN.
