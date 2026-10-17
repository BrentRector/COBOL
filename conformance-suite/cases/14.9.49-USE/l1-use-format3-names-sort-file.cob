*>--- conformance-case
*> id: l1-use-format3-names-sort-file
*> rules: [SR-14.9.49.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.3 SR2 — "File-name-1 or file-name-2 shall not be a sort or a merge file."
      *> This is the FILE-NAME-2 arm, the second half of the same rule: a Format 3 USE
      *> (§14.9.49.2 Format 3 — "exception-name-2 { FILE file-name-2 } …") names SRTF, an SD
      *> file. SR13 is satisfied — EC-I-O begins with 'EC-I-O' — so the only rule this program
      *> breaks is SR2. Format 3 is the exception-condition model introduced with COBOL 2002,
      *> so 1985 is not a rejecting edition for THIS arm (it is covered by the file-name-1 arm
      *> in l1-use-format1-names-sort-file).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1USE2B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "l1use2b-sort.dat".
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
           USE AFTER EXCEPTION CONDITION EC-I-O FILE SRTF.
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
