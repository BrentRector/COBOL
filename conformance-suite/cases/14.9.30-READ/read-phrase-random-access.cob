*>--- conformance-case
*> id: read-phrase-random-access
*> rules: [SR-14.9.30.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.30.3 SR6: "None of the phrases ADVANCING, AT END, NEXT,
      *> NOT AT END, or PREVIOUS shall be specified if ACCESS MODE RANDOM
      *> is specified in the file control entry for file-name-1."
      *> Tolerated (warning, bind unchanged) under --permissive -- the
      *> CCVS-85 corpus is lenient about phrase placement, and a phrase
      *> that cannot fire ('1x' on a random read) is simply dead in the
      *> emitter's status-first branches (kb/Work PB144).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB144N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT X ASSIGN TO "n4.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS X-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD X.
       01 X-R.
          05 X-KEY PIC X(3).
          05 X-VAL PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT X
           MOVE "K01" TO X-KEY
           READ X NEXT
               AT END CONTINUE
           END-READ
           STOP RUN.
