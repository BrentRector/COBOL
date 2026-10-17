*>--- conformance-case
*> id: pb416-initialize-target-renames
*> rules: [SR-14.9.20.3-5]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB416 — ISO 14.9.20.3 SR5: "The data description entry for the data item referenced by identifier-1
*> shall not contain a RENAMES clause." A level-66 entry (13.18.45) is COBOL-85, and so is this rule.
*> The check, its citation and its diagnostic code all EXISTED before PB416 and none of them could fire: the
*> only call site sat inside the arm taken when the reference does NOT resolve, and a 66 entry resolves
*> perfectly well. It fell through to the unsupported-place-kind arm and shipped as an unhandled run-time
*> NotImplementedCobolFeatureException naming a missing COMPILER feature — pointing the reader at this
*> compiler instead of at their own illegal program. The check now asks the RESOLVED item.
*> The positive control is tests/conformance/85/pb416_initialize_operand_screens_85 T5: a 66 entry BESIDE
*> identifier-1 is not a RENAMES clause in identifier-1's own entry, and INITIALIZE of that group is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB416NRN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC.
          05 N PIC 9(4) VALUE 1.
          05 A PIC X(4) VALUE "abcd".
       66 R66 RENAMES N THRU A.
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE R66.
           STOP RUN.
