*>--- conformance-case
*> id: pb137-commit-in-sort-proc
*> rules: [SR-14.9.7.3-2, GR-14.9.7.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.7.3 SR2: COMMIT shall not be specified in the input or output procedure of a MERGE or
      *> file SORT statement - the cross-pass that implemented exactly this ban carried a MERGE-only
      *> predicate; the identity-bearing bound node makes the sibling reachable (kb/Work PB137).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB137S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SWK ASSIGN TO "pb137-sort.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SWK.
       01 SW-REC.
          05 SW-KEY PIC S9(4) COMP.
       WORKING-STORAGE SECTION.
       01 DONE-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           SORT SWK ON ASCENDING KEY SW-KEY
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN
           STOP RUN.
       FEED.
           MOVE 1 TO SW-KEY
           RELEASE SW-REC
           COMMIT.
       DRAIN.
           PERFORM UNTIL DONE-FLAG = "Y"
               RETURN SWK RECORD
                   AT END MOVE "Y" TO DONE-FLAG
               END-RETURN
           END-PERFORM.
