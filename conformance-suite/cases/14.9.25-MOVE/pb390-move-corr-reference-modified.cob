*>--- conformance-case
*> id: pb390-move-corr-reference-modified
*> rules: [SR-14.9.25.3-12]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB390 - ISO 14.9.25.3 SR12's SECOND half: "Identifier-3 and identifier-4 shall specify group
*> data items AND SHALL NOT BE REFERENCE-MODIFIED." Only the first half was ever checked. The second was
*> assumed away by a code comment claiming a group "cannot" be reference-modified - false: a group item is
*> reference-modifiable and has its own golden (tests/conformance/2023/pb70_group_reference_modification).
*> 8.4.3.3.4 GR6 makes the result "an elementary data item", which is why the arithmetic spellings
*> (14.9.2.3 SR6 / 14.9.44.3 SR6, whose operands shall be group items) refuse it too.
*> Measured before the fix: this program COMPILED and RAN, moving the WHOLE group with the reference
*> modifier silently discarded - a wrong answer, not a loud one.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB390CORRRM.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G1.
   05 A PIC X(3) VALUE "ABC".
   05 B PIC X(3) VALUE "DEF".
01 G2.
   05 A PIC X(3) VALUE SPACES.
   05 B PIC X(3) VALUE SPACES.
PROCEDURE DIVISION.
MAIN.
    MOVE CORRESPONDING G1(1:3) TO G2.
    STOP RUN.
