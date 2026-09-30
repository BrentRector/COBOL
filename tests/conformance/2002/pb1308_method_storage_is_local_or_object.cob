      *> kb/Work PB1308, decision R62 - what a method DOES have, now that its WORKING-STORAGE is illegal at every edition.
      *> ISO §13.5.3 SR1: the working-storage section "may be specified only in a factory definition or an instance
      *> definition, but not in a method definition" (negative: pb1308-method-working-storage-every-edition).
      *> A method's own storage is LOCAL-STORAGE (ISO §8.6.4: "allocated and set to initial state each time the
      *> runtime element containing them is activated") and LINKAGE; state that persists across activations lives in
      *> the instance definition's working-storage (object data, one copy per instance).
      *> Derivation (two instances A and B, TICK invoked A, A, B):
      *>   OBJ = object data, one copy per instance, persistent across activations -> A: 1, 2   B: 1
      *>   LOC = local-storage, initial state on EVERY activation                  -> always 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1308P1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1308PC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE PB1308PC.
       01 B USAGE OBJECT REFERENCE PB1308PC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1308PC "NEW" RETURNING A.
           INVOKE PB1308PC "NEW" RETURNING B.
           INVOKE A "TICK".
           INVOKE A "TICK".
           INVOKE B "TICK".
           STOP RUN.
       END PROGRAM PB1308P1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1308PC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ-CTR PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. TICK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LOC-CTR PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO OBJ-CTR.
           ADD 1 TO LOC-CTR.
           DISPLAY "OBJ=" OBJ-CTR " LOC=" LOC-CTR.
       END METHOD TICK.
       END OBJECT.
       END CLASS PB1308PC.
