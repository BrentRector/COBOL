*>--- conformance-case
*> id: pb348-release-from-quote-numeric-removed-2023
*> rules: [GR-14.9.32.4-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.32.4 general rule 4 a): RELEASE record-name-1 FROM literal-1 is "MOVE literal-1
*> TO record-name-1 according to the rules specified for the MOVE statement".  The rules specified
*> for the MOVE statement include the 14.9.25.3 SR5 restriction on an alphanumeric figurative sender
*> to a numeric receiver, which Annex F.2 records as REMOVED at COBOL-2023 -- so the implicit move
*> is removed exactly as the explicit one is, and the program is refused (COBOLNET0902).
*> The EDITION half of kb/Work PB348.  At 2023 the explicit `MOVE QUOTE TO SRT-NUM` errored while
*> `RELEASE SRT-NUM FROM QUOTE` compiled with NO DIAGNOSTIC AT ALL: "according to the rules
*> specified for the MOVE statement" was applied at no edition, because the implicit move was
*> constructed after binding and no version-conformance pass could see it.  Its positive twin is
*> conformance:2002/pb348_release_from_figurative_image, where the same statement is valid and runs.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB348N9.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SRTF ASSIGN TO "pb348n9s.tmp".
DATA DIVISION.
FILE SECTION.
SD SRTF.
01 SRT-NUM PIC 9(3).
WORKING-STORAGE SECTION.
01 EOF-FLAG PIC X VALUE "N".
PROCEDURE DIVISION.
MAIN.
    SORT SRTF ON ASCENDING KEY SRT-NUM
        INPUT PROCEDURE IS FEED
        OUTPUT PROCEDURE IS DRAIN
    STOP RUN.
FEED.
    RELEASE SRT-NUM FROM QUOTE.
DRAIN.
    PERFORM UNTIL EOF-FLAG = "Y"
        RETURN SRTF RECORD
            AT END MOVE "Y" TO EOF-FLAG
            NOT AT END DISPLAY "R=[" SRT-NUM "]"
        END-RETURN
    END-PERFORM.
