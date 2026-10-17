*>--- conformance-case
*> id: pb337-return-into-not-admissible
*> rules: [SR-14.9.34.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.34.3 syntax rule 2: "The INTO phrase may be specified in a RETURN statement:
*> a) If only one record description is subordinate to the sort-merge file description entry, or
*> b) If all record-names associated with file-name-1 and the data item referenced by identifier-1
*> describe an alphanumeric group item or an elementary item of category alphanumeric or category
*> national."
*> The RETURN twin of pb337-read-into-not-admissible, and the SD wording differs from the FD's:
*> READ's arm a) also admits "no record description entry" because 13.4.5.3 SR3 permits a
*> record-less FD, while 13.4.6.3 SR2 requires a record description under an SD.  Here the SD has
*> TWO -- an alphanumeric group and an elementary category-NUMERIC item -- so arm a) fails and
*> S-NUM fails arm b).  COBOLNET1994.
*> Before kb/Work PB337 this compiled and ran, printing INTO=1ONE / INTO=2TWO.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB337N2.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SF ASSIGN TO "pb337n2s.tmp".
DATA DIVISION.
FILE SECTION.
SD SF.
01 S-REC.
   05 S-K PIC X(6).
01 S-NUM PIC 9(6).
WORKING-STORAGE SECTION.
01 WS-RECV PIC X(6).
PROCEDURE DIVISION.
MAIN.
    SORT SF ASCENDING S-K
        INPUT PROCEDURE IS FEED
        OUTPUT PROCEDURE IS DRAIN
    STOP RUN.
FEED.
    MOVE "1ONE  " TO S-REC
    RELEASE S-REC
    MOVE "2TWO  " TO S-REC
    RELEASE S-REC.
DRAIN.
    RETURN SF INTO WS-RECV
        AT END CONTINUE
    END-RETURN
    DISPLAY "INTO=" WS-RECV.
