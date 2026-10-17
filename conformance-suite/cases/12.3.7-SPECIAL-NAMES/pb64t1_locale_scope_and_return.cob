*>--- conformance-case
*> id: pb64t1_locale_scope_and_return
*> rules: [GR-12.3.7.4-1, GR-14.6.6-9]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.4 GR1 — "All clauses specified in the SPECIAL-NAMES paragraph of a source unit that contains other
      *> source units apply to each directly or indirectly contained source unit" (locale-names and alphabets reach the
      *> contained program) — and 14.6.6 r9 — "Upon return of control from another COBOL runtime element, the locale in
      *> effect for each locale category at the time of exit from the returning runtime element becomes the current
      *> locale for that category" (a callee's SET LOCALE is NOT unwound; the NOTE puts save/restore on the callee).
      *> kb/Work PB64 T1.
      *>
      *> What each line proves:
      *>   OUTER-1 — the containing program under the root order: "nz" > "ñu".
      *>   INNER   — the CONTAINED program (no CONFIGURATION SECTION of its own, 12.3.3 SR1) references the container's
      *>             locale-name ES and collates under the container's PCS (GR1); its comparison after its own
      *>             SET LOCALE LC_COLLATE TO ES is Spanish: "nz" < "ñu".
      *>   OUTER-2 — back in the container WITHOUT any SET of its own, the comparison is Spanish too (r9: the
      *>             callee's switch stands for the run unit).
      *>   OUTER-3 — the container restores the root explicitly (LC_COLLATE TO USER-DEFAULT).
      *> Non-ASCII appears only inside literals (UTF-8 source); every DISPLAY is ASCII.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1OUTER.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS CUR.
       SPECIAL-NAMES.
           ALPHABET CUR IS LOCALE
           LOCALE ES IS "es-ES".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A               PIC X(2) VALUE "nz".
       01  B               PIC X(2) VALUE "ñu".
       01  VERDICT         PIC X.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM CMP
           DISPLAY "OUTER-1=" VERDICT
           CALL "PB64T1INNER"
           PERFORM CMP
           DISPLAY "OUTER-2=" VERDICT
           SET LOCALE LC_COLLATE TO USER-DEFAULT
           PERFORM CMP
           DISPLAY "OUTER-3=" VERDICT
           STOP RUN.
       CMP.
           IF A < B MOVE "<" TO VERDICT ELSE MOVE ">" TO VERDICT END-IF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1INNER.
      *> No CONFIGURATION SECTION here (12.3.3 SR1): the container's LOCALE ES, ALPHABET CUR and PROGRAM COLLATING
      *> SEQUENCE apply to this contained program (12.3.7.4 GR1 / 12.3.4 GR1).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A               PIC X(2) VALUE "nz".
       01  B               PIC X(2) VALUE "ñu".
       PROCEDURE DIVISION.
       MAIN.
           SET LOCALE LC_COLLATE TO ES
           IF A < B DISPLAY "INNER=<" ELSE DISPLAY "INNER=>" END-IF
           EXIT PROGRAM.
       END PROGRAM PB64T1INNER.
       END PROGRAM PB64T1OUTER.
