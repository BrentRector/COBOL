*>--- conformance-case
*> id: pb64t1_sort_locale_snapshot
*> rules: [GR-14.6.6-5, SR-14.9.34.3-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.6.6 r5 — "For a SORT or MERGE statement specifying an alphabet-name associated with a locale in the
      *> COLLATING SEQUENCE phrase, category LC_COLLATE in the associated locale is used for that statement. A locale
      *> switch during execution of a SORT or MERGE statement has no effect on the processing of that SORT or MERGE
      *> statement." (kb/Work PB64 T1 — the SORT snapshot rule owed with SET LOCALE; DESIGN-locale-facility §12 T3.)
      *>
      *> The alphabet CUR is the locale CURRENT at use (12.3.7.4 GR7e). The INPUT PROCEDURE of the FIRST sort switches
      *> LC_COLLATE to Spanish BETWEEN two RELEASEs — the sequence the sort phase uses is the one in effect when the
      *> SORT statement BEGAN (the harness's pinned root), so "ñu" sorts BEFORE "nz" (root: n-tilde is n + tilde, then
      *> u < z decides). The SECOND sort begins after the switch, so it uses Spanish: n-tilde is a primary after n and
      *> "nz" sorts before "ñu".
      *> Non-ASCII appears only inside literals (UTF-8 source); every DISPLAY is ASCII.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1SNAP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET CUR IS LOCALE
           LOCALE ES IS "es-ES".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO "PB64T1SNAP.SRT".
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SORT-REC         PIC X(2).
       WORKING-STORAGE SECTION.
       01  EOF-FLAG        PIC X.
       01  PASS            PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           SORT SORT-FILE ON ASCENDING KEY SORT-REC
               COLLATING SEQUENCE IS CUR
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN
           MOVE 2 TO PASS
           SORT SORT-FILE ON ASCENDING KEY SORT-REC
               COLLATING SEQUENCE IS CUR
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN
           STOP RUN.
       FEED.
           MOVE "nz" TO SORT-REC  RELEASE SORT-REC
           SET LOCALE LC_COLLATE TO ES
           MOVE "ñu" TO SORT-REC  RELEASE SORT-REC
           MOVE "na" TO SORT-REC  RELEASE SORT-REC.
       DRAIN.
           MOVE "N" TO EOF-FLAG
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SORT-FILE AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END PERFORM SHOW
               END-RETURN
           END-PERFORM.
       SHOW.
           IF SORT-REC = "nz" DISPLAY "PASS" PASS "=nz"
           ELSE IF SORT-REC = "na" DISPLAY "PASS" PASS "=na"
           ELSE DISPLAY "PASS" PASS "=n-tilde-u" END-IF END-IF.
