*>--- conformance-case
*> id: pb348_from_into_move_rules_2014
*> rules: [SR-14.9.32.3-3, GR-14.9.32.4-4]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.9.32.4 GR4 a), 14.9.51.4 GR5 a), 14.9.35.4 GR7 a) -- each makes a ... FROM
      *> phrase EXACTLY "MOVE identifier-1 TO record-name-1 according to the rules specified for the
      *> MOVE statement", followed by the same statement without the phrase; 14.9.30.4 GR4 b) and
      *> 14.9.34.4 GR5 b) make ... INTO exactly the reverse move, "according to the rules for the
      *> MOVE statement without the CORRESPONDING phrase".  This program writes each phrase beside the
      *> explicit statement pair it is equivalent to, so the equivalence is what is measured.
      *> DERIVATION -- every expected line follows from the rules above, nothing from the compiler.
      *>  . INTO1 / INTO2: the two records were placed by `WRITE F-REC FROM WS-SEND` and by the
      *>    explicit `MOVE WS-SEND2 TO F-REC` + `WRITE F-REC`.  GR5 a) makes the first pair identical
      *>    to the second, so the file holds WS-SEND then WS-SEND2 in that order, and 14.9.30.4
      *>    GR4 b) hands each back unchanged: BBBbbbbb then AAAaaaaa.
      *>  . RWFROM: `REWRITE F-REC FROM WS-RW` is 14.9.35.4 GR7 a)'s MOVE followed by the REWRITE, and
      *>    14.9.35.4 GR2 replaces the record last read -- the first -- so re-reading the first record
      *>    yields WS-RW: RRRrrrrr.
      *>  . RET lines: `RELEASE SRT-REC FROM WS-SEND` (GR4 a)) and the explicit MOVE+RELEASE pair put
      *>    BBBbbbbb and AAAaaaaa into the sort file, and 14.9.40.4 GR8 a) returns the lower key
      *>    first, so ASCENDING SRT-REC gives AAAaaaaa then BBBbbbbb -- read out through
      *>    `RETURN ... INTO`, 14.9.34.4 GR5 b).
      *> The whole receiving area is displayed inside [ ] so a trailing-blank difference cannot be
      *> trimmed away by the corpus runner's per-line trailing-space normalisation.
      *> kb/Work PB348: every one of these five phrases used to build its implicit MOVE in the
      *> EMITTER, downstream of the bind-time MOVE screens and of the storage facts codegen consumes.
      *> The 2014 leg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB348P3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb348p3f.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SRTF ASSIGN TO "pb348p3s.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(8).
       SD SRTF.
       01 SRT-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-SEND PIC X(8) VALUE "BBBbbbbb".
       01 WS-SEND2 PIC X(8) VALUE "AAAaaaaa".
       01 WS-RW   PIC X(8) VALUE "RRRrrrrr".
       01 WS-RECV PIC X(8).
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           WRITE F-REC FROM WS-SEND
           MOVE WS-SEND2 TO F-REC
           WRITE F-REC
           CLOSE F
           OPEN INPUT F
           READ F INTO WS-RECV AT END CONTINUE END-READ
           DISPLAY "INTO1=[" WS-RECV "]"
           READ F INTO WS-RECV AT END CONTINUE END-READ
           DISPLAY "INTO2=[" WS-RECV "]"
           CLOSE F
           OPEN I-O F
           READ F AT END CONTINUE END-READ
           REWRITE F-REC FROM WS-RW
           CLOSE F
           OPEN INPUT F
           READ F INTO WS-RECV AT END CONTINUE END-READ
           DISPLAY "RWFROM=[" WS-RECV "]"
           CLOSE F
           SORT SRTF ON ASCENDING KEY SRT-REC
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN
           DISPLAY "DONE"
           STOP RUN.
       FEED.
           RELEASE SRT-REC FROM WS-SEND
           MOVE WS-SEND2 TO SRT-REC
           RELEASE SRT-REC.
       DRAIN.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SRTF RECORD INTO WS-RECV
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY "RET=[" WS-RECV "]"
               END-RETURN
           END-PERFORM.
