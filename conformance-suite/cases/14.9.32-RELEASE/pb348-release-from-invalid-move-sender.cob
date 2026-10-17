*>--- conformance-case
*> id: pb348-release-from-invalid-move-sender
*> rules: [SR-14.9.32.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.32.3 syntax rule 3: "Identifier-1 or literal-1 shall be valid as a sending
*> operand in a MOVE statement specifying record-name-1 as the receiving operand."  A PIC 9(3)
*> sender into a PIC A(8) receiver is not: 14.9.25.3 SR10 and Table 16 refuse a numeric or
*> numeric-edited sending operand to an alphabetic receiver (COBOLNET0819).
*> THE DECISIVE PAIR of kb/Work PB348.  Written as `MOVE WS-NUM TO SRT-REC` the compiler drew
*> COBOLNET0819; written as `RELEASE SRT-REC FROM WS-NUM` -- which 14.9.32.4 GR4 a) makes EXACTLY
*> that MOVE -- it compiled clean and printed R=[123     ].  Same move, opposite verdict, because
*> the implicit move was constructed in the emitter, downstream of every bind-time MOVE screen.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB348N5.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SRTF ASSIGN TO "pb348n5s.tmp".
DATA DIVISION.
FILE SECTION.
SD SRTF.
01 SRT-REC PIC A(8).
WORKING-STORAGE SECTION.
01 WS-NUM PIC 9(3) VALUE 123.
01 EOF-FLAG PIC X VALUE "N".
PROCEDURE DIVISION.
MAIN.
    SORT SRTF ON ASCENDING KEY SRT-REC
        INPUT PROCEDURE IS FEED
        OUTPUT PROCEDURE IS DRAIN
    STOP RUN.
FEED.
    RELEASE SRT-REC FROM WS-NUM.
DRAIN.
    PERFORM UNTIL EOF-FLAG = "Y"
        RETURN SRTF RECORD
            AT END MOVE "Y" TO EOF-FLAG
            NOT AT END DISPLAY "R=[" SRT-REC "]"
        END-RETURN
    END-PERFORM.
