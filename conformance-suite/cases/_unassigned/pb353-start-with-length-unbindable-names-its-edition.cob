*>--- conformance-case
*> id: pb353-start-with-length-unbindable-names-its-edition
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
      *> HERE: 14.9.41.2 prints the phrase as its own bracket, [ WITH LENGTH arithmetic-expression-1 ],
      *> and 14.9.41.3 SR8 ("If the LENGTH phrase is specified, file-name-1 shall reference a file with
      *> indexed organization") is a SEPARATE syntax rule the binder reports on its own - not the
      *> introduction. The KEY operand resolves to nothing, so BindStart returns a BoundUnsupported
      *> before BoundKeyedStart.Length is ever assigned. The WITH LENGTH phrase is still written.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB353N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb353n2.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY PIC X(4).
          05 IX-VAL PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT IXF
           START IXF KEY IS EQUAL TO NO-SUCH-KEY WITH LENGTH 3
           CLOSE IXF
           STOP RUN.
