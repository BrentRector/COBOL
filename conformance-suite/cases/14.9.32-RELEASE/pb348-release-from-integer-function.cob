*>--- conformance-case
*> id: pb348-release-from-integer-function
*> rules: [SR-14.9.32.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.32.3 syntax rule 2: "If identifier-1 is a function-identifier, it shall
*> reference an alphanumeric or national function."  FUNCTION LENGTH is an INTEGER function
*> (14.9.32.3 SR2 names the admitted set; 15.2 item 5 defines the integer functions), so it is
*> outside the admitted set at EVERY edition -- the rule is version-invariant and carries no
*> edition qualifier.
*> Until kb/Work PB348 the FROM phrase applied no rule of its own: SequentialIoBinder.WriteSource
*> bound the operand for WRITE, REWRITE and RELEASE alike and INSPECTED NOTHING, and the implicit
*> MOVE 14.9.32.4 GR4 a) makes of the phrase was then constructed in the EMITTER, downstream of
*> every bind-time screen.  This program compiled clean and printed R=[8       ] -- the value moves,
*> which is exactly why an under-reject in this position is invisible in a run.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB348N1.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SRTF ASSIGN TO "pb348n1s.tmp".
DATA DIVISION.
FILE SECTION.
SD SRTF.
01 SRT-REC PIC X(8).
WORKING-STORAGE SECTION.
01 WS-A PIC X(8) VALUE "ABCDEFGH".
01 EOF-FLAG PIC X VALUE "N".
PROCEDURE DIVISION.
MAIN.
    SORT SRTF ON ASCENDING KEY SRT-REC
        INPUT PROCEDURE IS FEED
        OUTPUT PROCEDURE IS DRAIN
    STOP RUN.
FEED.
    RELEASE SRT-REC FROM FUNCTION LENGTH(WS-A).
DRAIN.
    PERFORM UNTIL EOF-FLAG = "Y"
        RETURN SRTF RECORD
            AT END MOVE "Y" TO EOF-FLAG
            NOT AT END DISPLAY "R=[" SRT-REC "]"
        END-RETURN
    END-PERFORM.
