*>--- conformance-case
*> id: ca17_indexed_rewrite_collating
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> CA17 (CONFORMANCE-FIX-QUEUE): a sequential-access INDEXED REWRITE checks that the replaced record's prime
      *> key EQUALS the last record read (ISO §14.9.35 GR22, '21' if not). That equality is COLLATING-SEQUENCE-based
      *> per §12.4.5.12.4 GR1 ("according to the rules for a relation condition"), NOT a byte compare. Under ALPHABET
      *> ALPHA (A ALSO a) the keys "A123" and "a123" collate EQUAL, so a REWRITE that reads "A123" then rewrites with
      *> "a123" is a legal same-key replacement -> FS='00' and the record is replaced. Pre-fix the connector compared
      *> the prime key with C# ordinal '!=', so "a123" != "A123" wrongly yielded '21' and the REWRITE failed while
      *> WRITE/DELETE/target-lookup already used the weight-aware KeyEq — the lone ordinal outlier this fixes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA17.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ALPHA IS "A" ALSO "a".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "ca17-ixrewrite-collating.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS K
               FILE STATUS IS FS
               COLLATING SEQUENCE IS ALPHA.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 REC.
          05 K PIC X(4).
          05 D PIC X(7).
       WORKING-STORAGE SECTION.
       01 FS PIC X(2) VALUE "00".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "A123" TO K.
           MOVE "ORIGVAL" TO D.
           WRITE REC.
           CLOSE F.
           OPEN I-O F.
           READ F NEXT.
           MOVE "a123" TO K.
           MOVE "UPDATED" TO D.
           REWRITE REC.
           DISPLAY "FS=" FS.
           CLOSE F.
           OPEN INPUT F.
           READ F NEXT.
           DISPLAY "K=" K " D=" D.
           CLOSE F.
           STOP RUN.
