*>--- conformance-case
*> id: pointer_alloc
*> rules: [GR-14.9.3.4-3, GR-14.9.3.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.3 (ALLOCATE) / §14.9.15 (FREE) — dynamic storage as managed ManagedPointers (COBOL-2002).
      *>   ALLOCATE based-item        — obtain storage sized for the BASED item; set its data-address pointer.
      *>   ALLOCATE n CHARACTERS ...  — obtain n bytes; RETURNING gives the pointer.
      *>   FREE p                     — release the storage; set the pointer to NULL (the GC reclaims the bytes).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PTRALLOC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P  USAGE POINTER.
       01 B  PIC X(5) BASED.
       PROCEDURE DIVISION.
       MAIN.
      *> Form 2: allocate storage for the BASED item B (5 bytes); B now has its own storage.
           ALLOCATE B.
           MOVE "HELLO" TO B.
           DISPLAY "B=" B.
      *> Form 1: allocate 5 bytes, return pointer P; rebase B onto it; write+read through it.
           ALLOCATE 5 CHARACTERS RETURNING P.
           SET ADDRESS OF B TO P.
           MOVE "WORLD" TO B.
           DISPLAY "B2=" B.
      *> FREE the pointer -> NULL.
           FREE P.
           IF P = NULL THEN DISPLAY "FREED=YES" ELSE DISPLAY "FREED=NO".
           STOP RUN.
