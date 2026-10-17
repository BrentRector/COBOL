*>--- conformance-case
*> id: pb353-start-first-unbindable-file-names-its-edition
*> rules: [GR-14.9.41.4-20, GR-14.9.41.4-21]
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
      *> HERE: 14.9.41.2 prints FIRST / LAST / KEY as ONE general format, and 14.9.41.3 SR2 ("If the
      *> organization of the file referenced by file-name-1 is sequential, either the FIRST or the LAST
      *> phrase shall be specified") makes the phrase MANDATORY on a sequential file rather than
      *> different there - so the FIRST/LAST introduction cannot depend on anything the binder resolves.
      *> This file-name resolves to a SORT-MERGE file, which 13.4.6.3 SR3 ("File-name-1 shall not be
      *> specified in an input-output statement") forbids to START, so BindStart returns before
      *> KeyedStartMode.First is ever assigned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB353N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SDF ASSIGN TO "pb353n1.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS SD-KEY.
       DATA DIVISION.
       FILE SECTION.
       SD SDF.
       01 SD-REC.
          05 SD-KEY PIC X(4).
          05 SD-VAL PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           START SDF FIRST
           STOP RUN.
