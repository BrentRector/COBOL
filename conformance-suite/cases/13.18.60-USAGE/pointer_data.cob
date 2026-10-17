*>--- conformance-case
*> id: pointer_data
*> rules: [SR-13.18.60.3-9, GR-13.18.60.4-23]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60.4 / §8.4.3.10 / §14.9.39 — COBOL-2002 POINTER data (Phase 1).
      *> A USAGE POINTER item is an 8-byte opaque machine-address handle. NULL is the predefined null
      *> address (the all-zero handle). Phase 1 covers: declaration, SET p TO NULL, SET p TO another
      *> pointer, and equality comparison (= / NOT =) against NULL and against another pointer.
      *> (ADDRESS OF / BASED / ALLOCATE — taking a real address — are deferred to Phase 2; until then every
      *> pointer can only hold NULL, so the comparisons below are all against the null handle.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. POINTERDATA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 Q USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
      *> SET a pointer to NULL, then test it is NULL.
           SET P TO NULL.
           IF P = NULL THEN DISPLAY "PNULL=YES" ELSE DISPLAY "PNULL=NO".
           IF P NOT = NULL THEN DISPLAY "PNOTNULL=YES" ELSE DISPLAY "PNOTNULL=NO".
      *> Two null pointers compare equal.
           SET Q TO NULL.
           IF P = Q THEN DISPLAY "PEQQ=YES" ELSE DISPLAY "PEQQ=NO".
      *> Pointer-to-pointer assignment (handle copy), then re-test equality.
           SET P TO Q.
           IF P = Q THEN DISPLAY "AFTERSET=YES" ELSE DISPLAY "AFTERSET=NO".
           STOP RUN.
