*>--- conformance-case
*> id: pb348-release-from-zero-length-literal
*> rules: [SR-14.9.32.3-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.32.3 syntax rule 4: "Literal-1 shall not be a zero-length literal."  It is
*> RELEASE's rule ALONE: the FROM phrases of WRITE (14.9.51.3) and REWRITE (14.9.35.3) state no such
*> sentence, so kb/Work PB348 carries it on the RELEASE row of the per-verb rules table rather than
*> applying it to every FROM phrase.
*> 85 is not listed because the FROM phrase admits only identifier-1 at X3.23-1985, so a literal
*> sender there is refused by the edition gate COBOLNET0871 before SR4 is reached -- a different
*> rule, and a .err file names ONE diagnostic for every edition it lists.
*> This was a missing APPLICATION, not a missing capability: the compiler already owns the
*> zero-length-literal predicate and applies it in ControlFlowBinder, IntrinsicBinder,
*> DataBinder.Switches and CallBinder.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB348N4.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SRTF ASSIGN TO "pb348n4s.tmp".
DATA DIVISION.
FILE SECTION.
SD SRTF.
01 SRT-REC PIC X(8).
WORKING-STORAGE SECTION.
01 EOF-FLAG PIC X VALUE "N".
PROCEDURE DIVISION.
MAIN.
    SORT SRTF ON ASCENDING KEY SRT-REC
        INPUT PROCEDURE IS FEED
        OUTPUT PROCEDURE IS DRAIN
    STOP RUN.
FEED.
    RELEASE SRT-REC FROM "".
DRAIN.
    PERFORM UNTIL EOF-FLAG = "Y"
        RETURN SRTF RECORD
            AT END MOVE "Y" TO EOF-FLAG
            NOT AT END DISPLAY "R=[" SRT-REC "]"
        END-RETURN
    END-PERFORM.
