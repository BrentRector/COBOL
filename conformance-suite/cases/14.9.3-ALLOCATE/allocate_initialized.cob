*>--- conformance-case
*> id: allocate_initialized
*> rules: [GR-14.9.3.4-4, GR-14.9.3.4-6, GR-14.9.3.4-7, GR-14.9.3.4-10]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.3 — ALLOCATE … INITIALIZED (COBOL-2002).
      *>   GR7 (based form): the allocated storage is initialized "as if an INITIALIZE data-name-1
      *>        WITH FILLER ALL TO VALUE THEN TO DEFAULT statement were executed" — VALUE clauses win,
      *>        then numeric/numeric-edited items get ZERO (the EDITED zero through MOVE editing),
      *>        character items get SPACES; FILLER items are included (WITH FILLER).
      *>   GR4a: with data-name-1 AND RETURNING, the pointer also receives the address.
      *>   GR6 (CHARACTERS form): INITIALIZED = all bytes binary zeros.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALLOCINIP10AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P   USAGE POINTER.
       01 P2  USAGE POINTER.
       01 G   BASED.
          05 G-ID   PIC X(4) VALUE "AB12".
          05 G-CNT  PIC 9(3) VALUE 7.
          05 FILLER PIC X(3).
          05 G-AMT  PIC 9(4).
          05 G-TXT  PIC X(5).
          05 G-ED   PIC Z9.
       01 W   PIC X(10) BASED.
       01 Z   PIC X(6) BASED.
       PROCEDURE DIVISION.
       MAIN.
      *> GR7 + GR4a: VALUE-carrying members take their VALUE (ALL TO VALUE); the rest default
      *> (THEN TO DEFAULT) — numerics to ZERO, the edited member to the EDITED zero, X to SPACES.
           ALLOCATE G INITIALIZED RETURNING P.
           DISPLAY "ID=[" G-ID "]".
           DISPLAY "CNT=" G-CNT.
           DISPLAY "AMT=" G-AMT.
           DISPLAY "TXT=[" G-TXT "]".
           DISPLAY "ED=[" G-ED "]".
      *> GR4a witness: P addresses the SAME initialized storage — window its first 10 characters
      *> ("AB12" + "007" + the WITH-FILLER spaces) through a second based item.
           SET ADDRESS OF W TO P.
           DISPLAY "W=[" W "]".
      *> GR7 on an elementary based item: no VALUE -> THEN TO DEFAULT -> SPACES.
           ALLOCATE W INITIALIZED.
           DISPLAY "WS=[" W "]".
      *> GR6: ALLOCATE n CHARACTERS INITIALIZED -> binary zeros (LOW-VALUE), observed through Z.
           ALLOCATE 6 CHARACTERS INITIALIZED RETURNING P2.
           SET ADDRESS OF Z TO P2.
           IF Z = LOW-VALUE THEN DISPLAY "ZEROS=YES" ELSE DISPLAY "ZEROS=NO".
           FREE P P2.
           IF P = NULL AND P2 = NULL THEN DISPLAY "FREED=YES" ELSE DISPLAY "FREED=NO".
           STOP RUN.
