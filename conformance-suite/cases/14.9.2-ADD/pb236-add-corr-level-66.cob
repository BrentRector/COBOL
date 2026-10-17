*>--- conformance-case
*> id: pb236-add-corr-level-66
*> rules: [SR-14.9.2.3-6, SR-14.9.44.3-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB236 - ISO 14.9.2.3 SR6's SECOND clause: the operands "shall not be described with
*> level-number 66". RN is a RENAMES entry. It was rejected before PB236 too, but INCIDENTALLY and for the
*> WRONG REASON: DataBinder.BindRenames builds a 66 entry with no PICTURE and no children, so DataItem.IsGroup
*> is false for it and it fell into the "elementary operand" arm. SR6 excludes it BY NAME, and the diagnostic
*> now says so - a rejection for the rule the standard actually gives.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB236CORR66.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 SRC.
   05 A PIC 9(3) VALUE 1.
   05 B PIC 9(3) VALUE 2.
66 RN RENAMES A THRU B.
01 GRP.
   05 A PIC 9(3) VALUE 1.
PROCEDURE DIVISION.
MAIN.
    ADD CORRESPONDING RN TO GRP.
    STOP RUN.
