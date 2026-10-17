*>--- conformance-case
*> id: pb331-rewrite-ignoring-lock
*> rules: [FMT-14.9.30.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.35.2 (PDF page 740, RENDERED) gives the REWRITE statement
      *> [ retry-phrase ] and [ WITH LOCK | WITH NO LOCK ] and no IGNORING LOCK
      *> alternative; 5.2.1 admits only what the general format prints. Its WRITE twin
      *> is negative/pb331-write-ignoring-lock. Both were accepted until kb/Work PB331,
      *> because the merged record-lock grammar rule they share with READ carried
      *> READ's IGNORING LOCK into statements that never print it.
      *> ⚠ THE EXPECTED CODE MOVED COBOL0001 → COBOL0307 WITH kb/Work PB428, AND THE OLD ONE WAS AN
      *> ARTEFACT. Until then the grammar carried a non-ISO `inlineMethodInvocationStatement :
      *> dataReference LPAREN argumentList? RPAREN` as a STATEMENT alternative, so after the WRITE/REWRITE
      *> statement the parser still had a viable alternative beginning with a bare word: at --std 2023 it
      *> consumed IGNORING as a data reference and failed one token later on LOCK, and below 2023 its
      *> {is2023()}? predicate failed MID-ALTERNATIVE and produced a no-viable-alternative COBOL0001. The
      *> rule is deleted (§14.9's roster has no such statement), so the parser now says the true thing at
      *> every edition, positioned on IGNORING itself: the sentence must have ended — COBOL0307. The rule
      *> this fixture exists for is unchanged and still enforced; only the parse-error code it pins moved.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB331RIG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "pb331rig.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD RLF.
       01 RL-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O RLF.
           MOVE 1 TO RL-KEY.
           MOVE "AAAA" TO RL-REC.
           REWRITE RL-REC IGNORING LOCK
               INVALID KEY CONTINUE
           END-REWRITE.
           CLOSE RLF.
           STOP RUN.
