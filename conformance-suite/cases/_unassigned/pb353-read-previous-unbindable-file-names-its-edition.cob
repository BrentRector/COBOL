*>--- conformance-case
*> id: pb353-read-previous-unbindable-file-names-its-edition
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
      *> HERE: 14.9.30.2 Format 1 prints PREVIOUS as an alternative of the direction phrase, and
      *> 14.9.30.3 SR8 ("If neither the NEXT phrase nor the PREVIOUS phrase is specified and ACCESS MODE
      *> SEQUENTIAL is specified ... the NEXT phrase is implied") makes an IMPLIED direction not a
      *> WRITTEN phrase - so only the PREVIOUS alternative gates.
      *> 8.4.2.1: the READ names no declared file, so the ONE file-name resolution step returns before
      *> IBoundRead.Kind exists. READ ... PREVIOUS is still written.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB353N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "pb353n3.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD RLF.
       01 RL-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 RL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O RLF
           READ NO-SUCH-FILE PREVIOUS
               AT END DISPLAY "E"
           END-READ
           CLOSE RLF
           STOP RUN.
