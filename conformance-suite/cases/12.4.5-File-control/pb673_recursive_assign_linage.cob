*>--- conformance-case
*> id: pb673_recursive_assign_linage
*> rules: [GR-12.4.5.3-3, GR-14.9.27.4-26]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §12.4.5.3 GR3 b) and §13.18.34.4 GR6 b) - THE OPERANDS BELONG TO THE ACTIVATION THAT
      *> EXECUTES THE STATEMENT, not to the last one that ran its prologue. The connector of a RECURSIVE,
      *> non-INITIAL program is UNIT-SCOPED last-used state (§8.6.4 "one copy of the item is available in a run
      *> unit"; §14.6.2.3.3), so ONE connector serves every activation while LOCAL-STORAGE is allocated afresh
      *> for each (§8.6.4) - which makes "whose data item?" observable.
      *>
      *> §13.18.34.4 GR6 b): "If a data-name is specified, the value is the content of the data item referenced
      *> by the associated data-name at the following times when the indicated statement references the
      *> associated file: 1. At the completion of an OPEN statement with the OUTPUT phrase." That statement is
      *> executed by depth 1, AFTER depth 2 has returned.
      *>
      *> EXPECTED, derived from the rules (depth 1's LOCAL-STORAGE: page size 4, footing start 3, file
      *> "pb673o.dat"; depth 2's: page size 9, footing start 9, file "pb673i.dat"):
      *>   OPEN=00   the association is made with depth 1's LS-NAME.
      *>   §13.18.34.4 GR7 d) sets LINAGE-COUNTER to one at the OPEN OUTPUT; each WRITE AFTER ADVANCING 1 LINE
      *>   increments it by one (GR7 c) 2.), so the counters observed after the two writes are 02 and 03.
      *>   EOP=N LC=02  counter 2 is below the footing start 3 - no end-of-page (§14.9.51.4 GR26).
      *>   EOP=Y LC=03  §14.9.51.4 GR26 b): "the execution of the WRITE statement causes printing or spacing
      *>                within the footing area of a page body ... when the associated LINAGE-COUNTER is equal
      *>                to or exceeds the current value of the footing start and is less than the page size" -
      *>                3 >= 3 and 3 < 4.  (Neither write reaches the page size, so GR26 a) never applies and
      *>                this golden does not depend on the GR26 a)/b) boundary at counter = page size, which
      *>                LinageConformanceTests pins.)
      *>   CHKI=05   "pb673i.dat" - depth 2's name - was never associated, so it is absent.
      *>   CHKO=00   "pb673o.dat" - depth 1's name - holds the output.
      *> Before kb/Work PB673 the connector held one installed source closure and one installed LINAGE evaluator
      *> closure, both re-installed unguarded at every activation, so depth 2's returned activation answered
      *> depth 1's OPEN: the wrong file, and a page model with no footing area in reach (EOP=N twice).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB673RC RECURSIVE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN USING LS-NAME
               FILE STATUS IS FS.
           SELECT OPTIONAL CHKI ASSIGN TO "pb673i.dat"
               FILE STATUS IS CK.
           SELECT OPTIONAL CHKO ASSIGN TO "pb673o.dat"
               FILE STATUS IS CK.
       DATA DIVISION.
       FILE SECTION.
      *> §13.18.34.3 SR2 - the LINAGE operands are elementary unsigned numeric integer items; nothing confines
      *> them to WORKING-STORAGE, and LOCAL-STORAGE is what makes the per-activation reading observable.
       FD  PRT LINAGE IS LS-BODY LINES WITH FOOTING AT LS-FOOT.
       01  PRT-REC PIC X(2).
       FD  CHKI.
       01  CHKI-REC PIC X(2).
       FD  CHKO.
       01  CHKO-REC PIC X(2).
       WORKING-STORAGE SECTION.
      *> A RECURSIVE program's WORKING-STORAGE is static (§13.5.4 GR1), so DEPTH counts activations.
       01  FS    PIC XX.
       01  CK    PIC XX.
       01  DEPTH PIC 9 VALUE 0.
       01  EOPF  PIC X.
       01  LCD   PIC 99.
       LOCAL-STORAGE SECTION.
       01  LS-NAME PIC X(20) VALUE "pb673i.dat".
       01  LS-BODY PIC 99 VALUE 09.
       01  LS-FOOT PIC 99 VALUE 09.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO DEPTH
           IF DEPTH = 1
               MOVE "pb673o.dat" TO LS-NAME
               MOVE 4 TO LS-BODY
               MOVE 3 TO LS-FOOT
      *>       Depth 2 runs its prologue with the LOCAL-STORAGE VALUE clauses above and returns; its storage is
      *>       gone, and nothing it did may reach depth 1's OPEN.
               CALL "PB673RC"
               OPEN OUTPUT PRT
               DISPLAY "OPEN=" FS
               PERFORM 2 TIMES
                   MOVE "XX" TO PRT-REC
                   MOVE "N" TO EOPF
                   WRITE PRT-REC AFTER ADVANCING 1 LINE
                       AT END-OF-PAGE MOVE "Y" TO EOPF
                   END-WRITE
                   MOVE LINAGE-COUNTER OF PRT TO LCD
                   DISPLAY "EOP=" EOPF " LC=" LCD
               END-PERFORM
               CLOSE PRT
               OPEN INPUT CHKI
               DISPLAY "CHKI=" CK
               CLOSE CHKI
               OPEN INPUT CHKO
               DISPLAY "CHKO=" CK
               CLOSE CHKO
           END-IF
           GOBACK.
       END PROGRAM PB673RC.
