*>--- conformance-case
*> id: l1_cancel_external_unchanged
*> rules: [GR-13.18.22.4-1, GR-14.9.5.4-8]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.5.4 8 — "The contents of data items in external data records described by a program are not
      *> changed when that program is canceled."  DERIVED FROM THE RULE TEXT, and corroborated by a second,
      *> independent clause: §14.6.2.3.3 says "Static and external data are the only data that are in the
      *> last-used state.  External data is always in the last-used state except when the run unit is
      *> activated." — a CANCEL is not a run-unit activation, so external data keeps its contents across one.
      *> THE DISCRIMINATOR.  The same CANCEL that must LEAVE the external record alone must RESET the canceled
      *> program's own working-storage: §13.5.4 1) makes "Data items in the working-storage section of a
      *> program that does not have the initial attribute ... static data", and §14.6.2.3.2 3) places static
      *> data in the initial state "The first time the program in which it is described is activated after the
      *> execution of a CANCEL statement referencing the program", where step 2 re-applies the VALUE clause.
      *> So the two storages must PART COMPANY at the CANCEL, and both halves are measured here in one run.
      *> Without the counter half this golden would be vacuous: an implementation that ignored the CANCEL
      *> entirely would also leave the external record untouched and would read as GR8-conforming.
      *> THE SHARING.  §13.18.22.1: "The constituent data items and group data items of an external data record
      *> are available in a run unit to every runtime element that describes the record as external."  The two
      *> descriptions carry the same externalized name and define the same number of bytes, as §13.18.22.4 6)
      *> requires; that rule does not constrain the SUBORDINATE data-names, so they are deliberately different
      *> here — MAIN writes through L1X-MAIN and the sub reads and writes through L1X-SUB, which is itself the
      *> evidence that one run-unit storage, not two look-alike copies, is in play.
      *> Sequencing note: the sub has returned (GOBACK) before the CANCEL, so §14.9.5.4 5's active-state
      *> prohibition is not engaged and GR3's "in its initial state" applies to the following CALL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1XCN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1X-SHARED IS EXTERNAL.
          05 L1X-MAIN PIC X(5).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ALPHA" TO L1X-MAIN.
           CALL "L1XCNS".
           DISPLAY "AFTCALL=[" L1X-MAIN "]".
           CANCEL "L1XCNS".
           DISPLAY "AFTCANC=[" L1X-MAIN "]".
           CALL "L1XCNS".
           DISPLAY "AFTRECL=[" L1X-MAIN "]".
           STOP RUN.
       END PROGRAM L1XCN01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1XCNS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1X-SHARED IS EXTERNAL.
          05 L1X-SUB PIC X(5).
       01 L1X-CTR PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       SUB-P.
           ADD 1 TO L1X-CTR.
           DISPLAY "SUBSEES=[" L1X-SUB "] CTR=" L1X-CTR.
           MOVE "OMEGA" TO L1X-SUB.
           GOBACK.
       END PROGRAM L1XCNS.
