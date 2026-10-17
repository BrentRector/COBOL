*>--- conformance-case
*> id: pb353-read-advancing-on-lock-unbindable-names-its-edition
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB353. An edition gate keyed on a BOUND NODE'S SHAPE is silently un-gated on every path
      *> that bails out before the node is built: a written construct's PARSE node is always present, but
      *> the bound node it would have produced is dropped (BoundUnsupported / BoundNop) the moment the
      *> binder refuses the statement, so an introduction gate must fire on RECOGNITION. Each program in
      *> this family violates a SECOND rule as well, so the binder refuses it before the bound node exists;
      *> a per-edition compiler must still name the edition of the post-85 construct that was WRITTEN.
      *> Measured before the fix: each of these reported its other error ALONE at --std 85.
      *> HERE: 14.9.30.2 prints ADVANCING ON LOCK as its own bracket - the THIRD printed spelling of the
      *> 2002 record-lock introduction, and the one whose gate stayed on the BOUND arm while IGNORING
      *> LOCK and the retention phrase gated on recognition beside it, so one construct id fired from
      *> BOTH arms. The direction here is the version-invariant NEXT, which isolates the lock phrase as
      *> the only post-85 construct under test; the file-name resolves to nothing, so KeyedIoBinder
      *> returns before BoundKeyedRead.AdvancingOnLock is ever assigned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB353N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLG ASSIGN TO "pb353n4.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RG-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD RLG.
       01 RG-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 RG-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O RLG
           READ NO-SUCH-FILE NEXT ADVANCING ON LOCK
               AT END DISPLAY "E"
           END-READ
           CLOSE RLG
           STOP RUN.
