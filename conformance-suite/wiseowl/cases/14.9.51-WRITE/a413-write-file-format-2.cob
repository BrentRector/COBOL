*>--- conformance-case
*> id: a413-write-file-format-2
*> rules: [SR-14.9.51.3-7, SR-14.9.51.3-8, SR-14.9.51.3-9,
*>    SR-14.9.51.3-10, GR-14.9.51.4-7, GR-14.9.51.4-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 Annex A.4.13 item 2), FORMAT 2. 14.9.51.2 prints TWO general formats and BOTH carry the
*> same `{ record-name-1 | FILE file-name-1 }` choice (RENDERED, PDF p785 format 1 and p786 format 2);
*> 14.9.51.3 rule 3 requires format 2 for an indexed or relative write file. The sequential witness alone
*> would not have covered this: the binder reroutes keyed organizations to a DIFFERENT method
*> (KeyedIoBinder.BindWrite), so a refusal placed after that reroute would leave the indexed arm silently
*> accepting the unclaimed phrase. The check therefore sits BEFORE the reroute, and this fixture is what
*> proves it (measure the selector's complement: one witness per arm, not one per statement).
IDENTIFICATION DIVISION.
PROGRAM-ID. A413WF29AL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-IX ASSIGN TO "a413wf29al.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS RANDOM
        RECORD KEY IS F-KEY.
DATA DIVISION.
FILE SECTION.
FD F-IX.
01 F-REC.
   05 F-KEY PIC X(4).
   05 F-VAL PIC X(16).
WORKING-STORAGE SECTION.
01 WS-REC.
   05 W-KEY PIC X(4) VALUE "K001".
   05 W-VAL PIC X(16) VALUE "HELLO".
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F-IX.
    WRITE FILE F-IX FROM WS-REC.
    CLOSE F-IX.
    STOP RUN.
