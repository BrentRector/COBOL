*>--- conformance-case
*> id: pb331-read-record-word-twice
*> rules: [FMT-14.9.30.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.30.2 Format 1 (PDF page 722, RENDERED) prints the optional word
      *> RECORD exactly ONCE - `READ file-name-1 { NEXT | PREVIOUS } RECORD [ INTO
      *> identifier-1 ]` - and 5.2.1 admits only the sequence the general format gives.
      *> 5.2.3 makes a non-underlined word one that "may be specified" or omitted, not
      *> one that may be repeated.
      *> It compiled until kb/Work PB331 because the one optional word was written in
      *> TWO grammar places: `readDirection : (NEXT|PREVIOUS) RECORD?` and the
      *> statement's own `RECORD?`. Neither spelling alone is wrong; having both is,
      *> and no test could see it because both single-RECORD spellings still parse.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB331RR2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb331rr2.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT SQF.
           READ SQF NEXT RECORD RECORD
               AT END CONTINUE
           END-READ.
           CLOSE SQF.
           STOP RUN.
