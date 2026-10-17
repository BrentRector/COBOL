*>--- conformance-case
*> id: pb334-read-previous-line-sequential
*> rules: [SR-14.9.30.3-7, GR-14.9.30.4-19]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB334 — ISO 14.9.30.3 SR7: "The phrase PREVIOUS shall not be specified if FILE
      *> ORGANIZATION LINE SEQUENTIAL is specified in the file control entry for file-name-1."
      *> SR7 had NO ENFORCEMENT ANYWHERE, and could not have had one: the rule pairs an organization
      *> with a read DIRECTION, and the direction did not exist below the parse tree on the one binder
      *> arm every LINE SEQUENTIAL file takes. The phrase was discarded and the read ran FORWARD.
      *> Not listed at 85: LINE SEQUENTIAL is not a COBOL-85 organization, so the 85 rejection is a
      *> different diagnostic about the file control entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB334NLS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LSF ASSIGN TO "pb334nls.txt"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  LSF.
       01  LS-REC        PIC X(3).
       WORKING-STORAGE SECTION.
       01  WS-ST         PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT LSF
           READ LSF PREVIOUS RECORD
               AT END CONTINUE
           END-READ
           CLOSE LSF
           STOP RUN.
