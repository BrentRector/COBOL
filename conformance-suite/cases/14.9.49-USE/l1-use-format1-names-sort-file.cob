*>--- conformance-case
*> id: l1-use-format1-names-sort-file
*> rules: [SR-14.9.49.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.3 SR2 — "File-name-1 or file-name-2 shall not be a sort or a merge file."
      *> This is the FILE-NAME-1 arm: a Format 1 USE (§14.9.49.2 Format 1, whose operand brace
      *> holds "{ file-name-1 } …") names SRTF, which §13.4.6 describes with a sort-merge file
      *> description entry (SD). SR2 is an ALL FORMATS rule, so every edition that has Format 1
      *> — 1985 onwards — shall reject it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1USE2A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "l1use2a-sort.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SRTF.
       01 SRT-REC.
          05 SRT-KEY PIC X(3).
       WORKING-STORAGE SECTION.
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       BAD-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON SRTF.
       BAD-PARA.
           CONTINUE.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           SORT SRTF ON ASCENDING KEY SRT-KEY
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN
           STOP RUN.
       FEED.
           MOVE "AAA" TO SRT-KEY
           RELEASE SRT-REC.
       DRAIN.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SRTF RECORD
                   AT END MOVE "Y" TO EOF-FLAG
               END-RETURN
           END-PERFORM.
