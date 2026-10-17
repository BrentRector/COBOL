*>--- conformance-case
*> id: pb144_indexed_delete_record
*> rules: [SR-14.9.10.3-2, GR-14.9.10.4-1, GR-14.9.10.4-3, GR-14.9.10.4-8, GR-14.9.10.4-9, GR-14.9.10.4-11]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB144IXD.
      *> kb/Work PB144 - the DELETE RECORD legs no fixture has ever
      *> executed. MEASURED before this was written: 37 fixtures mention
      *> INDEXED and NOT ONE issues a DELETE against one, and both DELETE
      *> unit tests build RELATIVE connectors -- so IndexedConnector.Delete
      *> and KeyedIoEmitter's key-slice emission had never run in the
      *> suite. Every expected value below is derived from the rule named
      *> beside it, not from what the code happens to do.
      *>
      *>   IXDEL00 - 14.9.10.4 GR3: indexed + DYNAMIC access, the record
      *>             identified by the PRIME RECORD KEY is removed -> 00.
      *>   IXGONE  - GR5: after successful execution the record "can no
      *>             longer be accessed" -> a random re-READ gives 23.
      *>   IXAREA* - GR8: "The execution of a DELETE RECORD statement does
      *>             not affect the content of the record area." The area
      *>             is DISPLAYed before AND after, and the two must be
      *>             identical. The probe READS the area rather than
      *>             noting that the code does not write it -- this rule
      *>             is satisfied by omission today, which is exactly why
      *>             nothing had ever contradicted it.
      *>   IXNEXT  - GR9: "The file position indicator is not affected by
      *>             the execution of a DELETE RECORD statement." After a
      *>             READ NEXT positions on K02 and K02 is deleted, the
      *>             next READ NEXT must return K03 -- the FPI survived
      *>             deletion of the record it was on.
      *>   IXIK23  - GR3 last sentence + GR11: a key the file does not
      *>             contain is the invalid key condition -> 23, the
      *>             INVALID KEY imperative RUNS and NOT INVALID KEY is
      *>             skipped (9.1.14).
      *>   IXNIK   - GR11 the other way: a successful delete takes the
      *>             NOT INVALID KEY branch.
      *>   IX43    - GR2: in SEQUENTIAL access the prior statement shall
      *>             have been a successful READ; with none, 43.
      *>   IX49    - GR1: the open mode shall be I-O; INPUT gives 49.
      *>
      *> NOTE: no INVALID KEY phrase appears on a sequential-access DELETE
      *> anywhere below -- 14.9.10.3 SR2 forbids it and COBOLNET1720 now
      *> enforces that under strict.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-D ASSIGN TO "pb144ixd.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS D-KEY
               FILE STATUS IS D-ST.
           SELECT F-S ASSIGN TO "pb144ixs.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS S-KEY
               FILE STATUS IS S-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-D.
       01 D-REC.
          05 D-KEY PIC X(3).
          05 D-VAL PIC X(5).
       FD F-S.
       01 S-REC.
          05 S-KEY PIC X(3).
          05 S-VAL PIC X(5).
       WORKING-STORAGE SECTION.
       01 D-ST PIC XX.
       01 S-ST PIC XX.
       01 WS-BEFORE PIC X(8).
       01 WS-AFTER  PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
      *> ---- seed four records through the DYNAMIC connector ----------
           OPEN OUTPUT F-D.
           MOVE "K01" TO D-KEY. MOVE "VAL01" TO D-VAL. WRITE D-REC.
           MOVE "K02" TO D-KEY. MOVE "VAL02" TO D-VAL. WRITE D-REC.
           MOVE "K03" TO D-KEY. MOVE "VAL03" TO D-VAL. WRITE D-REC.
           MOVE "K04" TO D-KEY. MOVE "VAL04" TO D-VAL. WRITE D-REC.
           CLOSE F-D.
      *> ---- GR1: DELETE on a connector open INPUT is 49 --------------
           OPEN INPUT F-D.
           MOVE "K01" TO D-KEY.
           DELETE F-D RECORD. DISPLAY "IX49=" D-ST.
           CLOSE F-D.
      *> ---- GR3 + GR11: a random delete by prime key -----------------
           OPEN I-O F-D.
           MOVE "K01" TO D-KEY.
           DELETE F-D RECORD
               INVALID KEY DISPLAY "IXNIK=WRONG-BRANCH"
               NOT INVALID KEY DISPLAY "IXNIK=NOT-INVALID"
           END-DELETE.
           DISPLAY "IXDEL00=" D-ST.
      *> ---- GR5: the removed record can no longer be accessed --------
           MOVE "K01" TO D-KEY.
           READ F-D. DISPLAY "IXGONE=" D-ST.
      *> ---- GR3 last sentence + GR11: an absent key -> 23 ------------
           MOVE "K99" TO D-KEY.
           DELETE F-D RECORD
               INVALID KEY DISPLAY "IXIKFIRED=YES"
               NOT INVALID KEY DISPLAY "IXIKFIRED=WRONG-BRANCH"
           END-DELETE.
           DISPLAY "IXIK23=" D-ST.
      *> ---- GR8 + GR9 on the SAME delete -----------------------------
      *> Position on K02 with a READ NEXT, capture the record area, then
      *> delete the record the FPI is sitting on.
           MOVE "K02" TO D-KEY.
           START F-D KEY IS EQUAL TO D-KEY. DISPLAY "IXSTART=" D-ST.
           READ F-D NEXT. DISPLAY "IXRDNEXT=" D-ST.
           MOVE D-REC TO WS-BEFORE.
           DELETE F-D RECORD
               INVALID KEY DISPLAY "IXGR8=WRONG-BRANCH"
           END-DELETE.
           MOVE D-REC TO WS-AFTER.
           DISPLAY "IXAREABEF=" WS-BEFORE.
           DISPLAY "IXAREAAFT=" WS-AFTER.
           IF WS-BEFORE = WS-AFTER
               DISPLAY "IXGR8=AREA-UNCHANGED"
           ELSE
               DISPLAY "IXGR8=AREA-CHANGED"
           END-IF.
      *> GR9: the FPI is unaffected, so the next sequential read returns
      *> the record that FOLLOWED the deleted K02 -- namely K03.
           READ F-D NEXT. DISPLAY "IXNEXTST=" D-ST.
           DISPLAY "IXNEXT=" D-KEY.
           CLOSE F-D.
      *> ---- GR2: sequential access with no prior successful READ -----
           OPEN OUTPUT F-S.
           MOVE "S01" TO S-KEY. MOVE "SVAL1" TO S-VAL. WRITE S-REC.
           CLOSE F-S.
           OPEN I-O F-S.
           DELETE F-S RECORD. DISPLAY "IX43=" S-ST.
           CLOSE F-S.
           STOP RUN.
