*>--- conformance-case
*> id: pb352-start-sort-merge-file
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 9.1.19 - "The only statements that may reference a sort file are the RELEASE,
*> RETURN, and SORT statements" (13.4.6.3 SR3/SR4 say it as a syntax rule). START was the ONE keyed
*> verb that did not run the shared sort-merge screen, so an SD whose SELECT carried ORGANIZATION
*> INDEXED accepted a START and then ran against a connector that was never registered.
IDENTIFICATION DIVISION.
PROGRAM-ID. P352SDST.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SDF ASSIGN TO "p352sdst.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS SD-KEY.
DATA DIVISION.
FILE SECTION.
SD SDF.
01 SD-REC.
   05 SD-KEY  PIC X(4).
   05 SD-DATA PIC X(6).
PROCEDURE DIVISION.
MAIN.
    START SDF KEY IS = SD-KEY END-START
    STOP RUN.
