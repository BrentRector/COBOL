*>--- conformance-case
*> id: pb348-return-into-strong-group
*> rules: [SR-14.9.34.3-3, GR-14.9.34.4-5]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.34.4 general rule 5 b): "The current record is moved from the record area to
*> the area specified by identifier-1 according to the rules for the MOVE statement without the
*> CORRESPONDING phrase."  Those rules include 14.9.25.3 syntax rule 2, which with 8.5.3.3 requires
*> the sending operand of a MOVE whose receiver is a strongly-typed group to be a group item of the
*> SAME type.  The SD record area here is an ordinary group, so the move is invalid and the RETURN
*> is refused (COBOLNET1533).
*> Before kb/Work PB348 this program compiled and ran silently while the identical MOVE written out
*> explicitly was rejected -- the INTO move was built in the emitter and no bind pass ever saw it.
*> 85 is not listed: TYPEDEF and the STRONG phrase are 2002 introductions, so the 1985 leg is
*> refused by the edition gate rather than by GR5 b).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB348N7.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SRTF ASSIGN TO "pb348n7s.tmp".
DATA DIVISION.
FILE SECTION.
SD SRTF.
01 SRT-REC.
   05 SRT-A PIC X(4).
WORKING-STORAGE SECTION.
01 TA IS TYPEDEF STRONG.
   05 TA-A PIC X(4).
01 WS-RECV TYPE TA.
01 EOF-FLAG PIC X VALUE "N".
PROCEDURE DIVISION.
MAIN.
    SORT SRTF ON ASCENDING KEY SRT-A
        INPUT PROCEDURE IS FEED
        OUTPUT PROCEDURE IS DRAIN
    STOP RUN.
FEED.
    MOVE "ABCD" TO SRT-A
    RELEASE SRT-REC.
DRAIN.
    PERFORM UNTIL EOF-FLAG = "Y"
        RETURN SRTF RECORD INTO WS-RECV
            AT END MOVE "Y" TO EOF-FLAG
            NOT AT END DISPLAY "R=[" WS-RECV "]"
        END-RETURN
    END-PERFORM.
