*>--- conformance-case
*> id: pb450-set-address-second-operand-below-2002
*> rules: [FMT-14.9.39.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB450 - the COBOL-2002 introduction gate on SET Format 7, asked at the position an arity-one
*> production could not reach. ISO 14.9.39.2 Format 7 repeats the whole
*> `{ ADDRESS OF data-name-1 | identifier-5 }` brace, so the ADDRESS phrase here is written in the SECOND
*> receiving operand and the first is a plain identifier-5. The gate is a parse-tree override on the WHOLE
*> setAddressStatement rule (VersionConformancePass.ParseArm.VisitSetAddressStatement), so it fires wherever
*> in the receiving list the ADDRESS phrase stands.
*> !! THE PROGRAM CARRIES EXACTLY ONE setAddressStatement, AND IT IS THE MIXED ONE, ON PURPOSE. A companion
*> `SET P1 TO ADDRESS OF REC-A` would draw the SAME COBOLNET0900 from a shape the OLD grammar could express,
*> so this case would stay green even if the mixed receiving list regressed to COBOL0001 - which is exactly
*> what it was before kb/Work PB450 half 2 (measured: `SET ADDRESS OF B1 P1 TO P2` was
*> `error COBOL0001: unexpected 'P1'`) (feedback_green_gates_arent_evidence).
*> Format 7's COBOL-2002 date is constructs.json SetAddress2002 / usage-pointer-2002 - the data-pointer
*> family's edition.
*> 85 ONLY: at 2002 and above this is conforming source - the positive is
*> conformance-suite/cases/14.9.39-SET/pb450_set_format7_receiving_list.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB450F7.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 P1 USAGE POINTER.
01 P2 USAGE POINTER.
01 B1 BASED.
   05 B1A PIC X(4).
PROCEDURE DIVISION.
MAIN-P.
    SET P2 ADDRESS OF B1 TO P1.
    DISPLAY "UNREACHED".
    STOP RUN.
