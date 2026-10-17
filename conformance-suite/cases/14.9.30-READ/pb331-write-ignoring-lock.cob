*>--- conformance-case
*> id: pb331-write-ignoring-lock
*> rules: [FMT-14.9.30.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.51.2 (PDF pages 815/816, RENDERED) gives the WRITE statement exactly
      *> two lock-related brackets - [ retry-phrase ] and [ WITH LOCK | WITH NO LOCK ] -
      *> in BOTH formats. There is no IGNORING LOCK alternative anywhere in WRITE, and
      *> 5.2.1 admits only what the general format prints.
      *> It was accepted until kb/Work PB331 because WRITE and REWRITE shared READ's
      *> merged record-lock rule, which carried READ's IGNORING LOCK with it. The
      *> REWRITE twin is negative/pb331-rewrite-ignoring-lock (14.9.35.2, page 740).
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
       PROGRAM-ID. PB331WIG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb331wig.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SQF.
           MOVE "AAAA" TO SQ-REC.
           WRITE SQ-REC IGNORING LOCK.
           CLOSE SQF.
           STOP RUN.
