*>--- conformance-case
*> id: pb353-invoke-unbindable-target-names-its-edition
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
      *> HERE: invokeStatement is its own grammar rule with exactly one use site, so presence IS the
      *> gate. 14.9.23.2 prints the target as identifier-1 / class-name-1; here it resolves to neither a
      *> data item nor a class of the compilation group, so OoBindInvoke returns COBOLNET0823 before any
      *> BoundInvoke exists. Keying on that node was ALSO over-inclusive: BoundInvoke is equally the
      *> bound form of a synthesized PROPERTY get/set and of NEW / SELF-NEW.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB353N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE NO-SUCH-OBJ "M1" RETURNING WS-R
           STOP RUN.
