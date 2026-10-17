*>--- conformance-case
*> id: pb337-return-into-strong-record-areas
*> rules: [SR-14.9.34.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.34.3 syntax rule 3: "If identifier-1 is a strongly-typed group item, there
*> shall be exactly one record area subordinate to the SD for file-name-1.  This record area shall
*> be a strongly-typed group item of the same type as identifier-1."
*> The RETURN twin of pb337-read-into-strong-record-areas, and the wording differs: READ's rule 2
*> says "at most one ... if specified" because 13.4.5.3 SR3 permits an FD with no record
*> description entry, while this one says "exactly one" -- 13.4.6.3 SR2 requires a record
*> description under an SD.  The SD here has TWO record areas.  COBOLNET1995.
*> 85 is not listed: TYPEDEF and the STRONG phrase are 2002 introductions.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB337N4.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SF ASSIGN TO "pb337n4s.tmp".
DATA DIVISION.
FILE SECTION.
SD SF.
01 S-REC.
   05 S-K PIC X(6).
01 PLAIN.
   05 P-K PIC X(6).
WORKING-STORAGE SECTION.
01 TA IS TYPEDEF STRONG.
   05 TA-K PIC X(6).
01 WS-RECV TYPE TA.
PROCEDURE DIVISION.
MAIN.
    SORT SF ASCENDING S-K
        INPUT PROCEDURE IS FEED
        OUTPUT PROCEDURE IS DRAIN
    STOP RUN.
FEED.
    MOVE "1ONE  " TO S-REC
    RELEASE S-REC.
DRAIN.
    RETURN SF INTO WS-RECV
        AT END CONTINUE
    END-RETURN
    DISPLAY "INTO=" TA-K OF WS-RECV.
