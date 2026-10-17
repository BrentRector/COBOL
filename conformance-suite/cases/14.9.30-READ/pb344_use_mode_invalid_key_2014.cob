*>--- conformance-case
*> id: pb344_use_mode_invalid_key_2014
*> rules: [GR-14.9.30.4-28]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB344 — the ISO edition the compilation targets reaches the file connectors and the
      *> generated USE-declarative selector.  Until PB344 no edition reached either, so the 2023 rules
      *> were served to --std 85, 2002 and 2014 as well.
      *>
      *> THE RULES, and every expected value below derived from them (no observation):
      *>  14.9.49.4 GR6 — the declarative runs on unsuccessful execution "unless an AT END or INVALID KEY
      *>    phrase takes precedence"; b)–e) scope it by OPEN MODE ("for any file open in the input mode or in
      *>    the process of being opened in the input mode", and its OUTPUT / I-O / EXTEND siblings).  (C1..C5)
      *>  14.9.49.4 GR5 — a declarative naming the FILE takes precedence over an open-mode one.        (C3)
      *>  14.9.30.4 GR28 — "If the file position indicator indicates that an optional input file is not
      *>    present, the invalid key condition exists ... (See 9.1.14, Invalid key condition.)"        (C4, C5)
      *>  9.1.13.5 item 3 b) — that condition's status is '23'.                                       (C4, C5)
      *>  14.9.27.4 GR13 — OPEN INPUT of an absent OPTIONAL file succeeds; 9.1.13.1 makes it '05'.     (C4, C5)
      *>  14.9.51.4 — a WRITE whose record key duplicates an existing one is the invalid key condition,
      *>    status '22' (9.1.13.5 item 2 b)).                                                    (C1, C2, C3)
      *>
      *> EDITIONS - C1..C5 ARE IDENTICAL AT 85, 2002, 2014 AND 2023, and that is the DETERMINATION this
      *> program pins (kb/Work PB344).  Annex E.2 item 19 a) reads "INVALID KEY processing.  If an INVALID KEY
      *> phrase is not specified and an invalid key condition occurs, any declarative that specified INPUT,
      *> OUTPUT, I-O, or EXTEND would not have been executed.  It will now be executed", which read as a
      *> behaviour change would make C1 and C4 edition-dependent.  It is not one.  Annex E is INFORMATIVE,
      *> E.1 scopes it to "a list of the substantive changes between the previous COBOL standard and this
      *> Working Draft International Standard" - ONE prior edition - and item 19's own justification says the
      *> previous standard "was not clear or missing processing of some I-O exceptions", the sub-item adding
      *> "This appears to be an error in previous COBOL standards".  A silence is not a prohibition, and its
      *> sibling item 19 b) is refuted outright for 1985 by that edition's validation suite (NIST CCVS
      *> SQ122A/SQ136A/SQ137A/SQ138A/SQ148A); nothing distinguishes the two halves' evidence.  GR6's
      *> open-mode tier is therefore edition-invariant - C3's file-name precedence (GR5) and C2/C5's written
      *> INVALID KEY phrase were already so - and all four copies expect the same lines.
      *> VERSION_CHANGE_REFERENCE row 25.
      *>
      *> A DECLARATIVE RUNS BEFORE THE STATEMENT'S OWN PHRASES AND BEFORE THE NEXT STATEMENT (GR6 — after the
      *> I-O status is set, 9.1.13.1), so each "DECL-" line precedes the status line of the case that raised it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB344K14.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb344k14f.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS F-K
               FILE STATUS IS F-ST.
           SELECT G ASSIGN TO "pb344k14g.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS G-K
               FILE STATUS IS G-ST.
           SELECT OPTIONAL OPTI ASSIGN TO "pb344k14i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS OI-K
               FILE STATUS IS OI-ST.
           SELECT OPTIONAL OPTR ASSIGN TO "pb344k14r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS OR-K
               FILE STATUS IS OR-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC.
          05 F-K PIC X(4).
          05 F-V PIC X(6).
       FD G.
       01 G-REC.
          05 G-K PIC X(4).
          05 G-V PIC X(6).
       FD OPTI.
       01 OI-REC.
          05 OI-K PIC X(4).
          05 OI-V PIC X(6).
       FD OPTR.
       01 OR-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       01 G-ST PIC XX.
       01 OI-ST PIC XX.
       01 OR-ST PIC XX.
       01 OR-K PIC 9(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-IO SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON I-O.
       D-IO-P.
           DISPLAY "DECL-IO".
       D-IN SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       D-IN-P.
           DISPLAY "DECL-IN".
       D-FILE SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON G.
       D-FILE-P.
           DISPLAY "DECL-FILE".
       END DECLARATIVES.
       MAIN SECTION.
       M-SEED.
           OPEN OUTPUT F
           MOVE "K001" TO F-K MOVE "AAAAAA" TO F-V WRITE F-REC
           CLOSE F
           OPEN OUTPUT G
           MOVE "K001" TO G-K MOVE "AAAAAA" TO G-V WRITE G-REC
           CLOSE G.
       M-C1.
      *> C1 — an invalid key condition with NO INVALID KEY phrase, on a file open I-O: the open-mode tier.
           OPEN I-O F
           MOVE "K001" TO F-K MOVE "BBBBBB" TO F-V
           WRITE F-REC
           DISPLAY "C1=" F-ST.
       M-C2.
      *> C2 — the same condition WITH the phrase: GR6's "unless an INVALID KEY phrase takes precedence".
           MOVE "K001" TO F-K MOVE "CCCCCC" TO F-V
           WRITE F-REC
               INVALID KEY DISPLAY "C2=INVKEY|" F-ST
           END-WRITE
           CLOSE F.
       M-C3.
      *> C3 — the FILE-NAME-scoped declarative (GR5), which Annex E.2 item 19 a) does not touch.
           OPEN I-O G
           MOVE "K001" TO G-K MOVE "BBBBBB" TO G-V
           WRITE G-REC
           DISPLAY "C3=" G-ST
           CLOSE G.
       M-C4.
      *> C4 — GR28 on the INDEXED organization: an absent OPTIONAL file opened INPUT, random READ, no phrase.
           OPEN INPUT OPTI
           DISPLAY "C4OPEN=" OI-ST
           MOVE "K001" TO OI-K
           READ OPTI
           DISPLAY "C4=" OI-ST
           CLOSE OPTI.
       M-C5.
      *> C5 — GR28 on the RELATIVE organization, with the phrase written.
           OPEN INPUT OPTR
           DISPLAY "C5OPEN=" OR-ST
           MOVE 1 TO OR-K
           READ OPTR
               INVALID KEY DISPLAY "C5=INVKEY|" OR-ST
           END-READ
           CLOSE OPTR
           STOP RUN.
