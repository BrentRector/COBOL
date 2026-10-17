*>--- conformance-case
*> id: pb236-return-file-not-sd
*> rules: [SR-14.9.34.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB236 - ISO 14.9.34.3 SR1: "File-name-1 shall be described by a sort-merge file description entry
*> in the data division." F-IN has an FD. Also behind a GO TO, for the reason the RELEASE twin gives.
*> This site used to conflate TWO verdicts in one test: `!FilesByName.TryGetValue(...) || !file.IsSortMerge`
*> answered "there is no such file-name" (8.4.2.1) and "the file exists but is under an FD" (14.9.34.3 SR1)
*> with the same message. They are different diagnoses; the undeclared case is now COBOLNET1639.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB236RETNOSD.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-IN ASSIGN TO "pb236retnosd.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-IN.
01 F-REC PIC X(20).
WORKING-STORAGE SECTION.
01 WS-REC PIC X(20).
PROCEDURE DIVISION.
MAIN.
    GO TO SKIPPER.
    RETURN F-IN INTO WS-REC AT END CONTINUE END-RETURN.
SKIPPER.
    STOP RUN.
