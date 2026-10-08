      *> ISO §12.4.5.9.4 GR1 a) and b) 1. — LOCK MODE omitted: a
      *> SHARING clause, or an OPEN SHARING phrase, sets no record lock.
      *> RULE §12.4.5.9.4 GR1 (cite.py --check 12.4.5.9.4 OK, 1) a) and
      *>   1) b) 1.): "If the LOCK MODE clause is omitted from a file
      *>   control entry, a) If there is a SHARING clause in that file
      *>   control entry, no record locks are set by the execution of
      *>   I-O statements through the associated file connector.
      *>   b) If there is no SHARING clause in that file control entry,
      *>   1. If an OPEN statement for the associated file connector has
      *>   a SHARING phrase, no record locks are set by the execution of
      *>   I-O statements for that opening of the associated file
      *>   connector."
      *> (GR1 b) 2. — no clause and no phrase — is the implementor's
      *>   choice and is NOT pinned here or by any plain-read test: a
      *>   MANUAL default also sets no lock on a plain READ (14.9.30.4
      *>   GR11 d)). The discriminating pin, READ ... WITH LOCK through
      *>   a clause-less connector observed by another connector, is
      *>   2002/gn1_lock_mode_omitted_implementor_default; this program
      *>   is the evidence for a) and b) 1., and the two close GR1.)
      *> SHAPE. Three legs, each: connector X opens INPUT with the READ
      *>   ONLY sharing mode, the OBSERVER FE opens INPUT, X reads
      *>   record 1 WITH LOCK, then FE reads record 1. The legs differ
      *>   ONLY in X's file control entry:
      *>   LEG A  FA: SHARING WITH READ ONLY clause, no LOCK MODE:
      *>          GR1 a).
      *>   LEG B  FB: no SHARING clause, no LOCK MODE; the READ ONLY
      *>          mode comes from the OPEN phrase: GR1 b) 1.
      *>   LEG C  FC: FB plus LOCK MODE IS MANUAL, the CONTROL: it shows
      *>          FE really does see a lock X sets, so A2/B2 = '00' is
      *>          the absence of a lock and not a blind observer.
      *> The OBSERVER FE: SHARING WITH READ ONLY + LOCK MODE IS MANUAL.
      *>   Record locking is enabled for it (§9.1.15 2): of the read
      *>   only mode "Record locks are in effect"; it has a LOCK MODE),
      *>   so §14.9.30.4 GR9 applies to its READ: a record "locked by
      *>   another file connector" is a record operation conflict, and
      *>   with no RETRY phrase the READ gets I-O status 51 (§9.1.13.8
      *>   1): "an attempt to access a record that is currently locked
      *>   by another file connector"). A plain READ on FE sets no lock
      *>   of its own (§14.9.30.4 GR11 d): manual mode, no LOCK phrase).
      *> WHY THE READ ONLY MODE EVERYWHERE: §14.9.27.3 SR8 requires a
      *>   LOCK MODE clause whenever the ALL phrase is in effect, so a
      *>   LOCK-MODE-less entry cannot use ALL OTHER. Table 19: an INPUT
      *>   open requesting READ ONLY against an existing read only /
      *>   input connector is a "Normal open", so every OPEN succeeds.
      *>   WITH LOCK is legal on FA/FB/FC: none specifies automatic
      *>   locking (§14.9.30.3 SR4).
      *> DERIVATION of every output line (records R001, R002 seeded):
      *>   A1=00 R001  FA's READ finds record 1, which nobody locks.
      *>   A2=00 R001  GR1 a): FA set no lock, so FE's READ of record 1
      *>               meets no lock — no conflict, '00'.
      *>   B1=00 R001  as A1.
      *>   B2=00 R001  GR1 b) 1.: the OPEN had a SHARING phrase, so FB
      *>               set no lock — '00'.
      *>   C1=00 R001  FC's READ WITH LOCK: manual mode, LOCK phrase
      *>               written, locks in effect (read only mode) — the
      *>               lock on record 1 IS set (§12.4.5.9.4 GR5,
      *>               §14.9.30.4 GR11 d)).
      *>   C2=51       FE's READ of record 1, locked by FC: GR9 conflict
      *>               no RETRY — '51'; the record area is not shown.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1LKM01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "l1lkm01.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST0.
           SELECT FA ASSIGN TO "l1lkm01.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS KA
               FILE STATUS IS STA
               SHARING WITH READ ONLY.
           SELECT FB ASSIGN TO "l1lkm01.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS KB
               FILE STATUS IS STB.
           SELECT FC ASSIGN TO "l1lkm01.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS KC
               FILE STATUS IS STC
               LOCK MODE IS MANUAL.
           SELECT FE ASSIGN TO "l1lkm01.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS KE
               FILE STATUS IS STE
               SHARING WITH READ ONLY
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 S-REC PIC X(4).
       FD FA.
       01 A-REC PIC X(4).
       FD FB.
       01 B-REC PIC X(4).
       FD FC.
       01 C-REC PIC X(4).
       FD FE.
       01 E-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST0 PIC XX.
       01 STA PIC XX.
       01 STB PIC XX.
       01 STC PIC XX.
       01 STE PIC XX.
       01 KA  PIC 9(4).
       01 KB  PIC 9(4).
       01 KC  PIC 9(4).
       01 KE  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> Seed two records; no other connector is open.
           OPEN OUTPUT FS.
           MOVE "R001" TO S-REC.
           WRITE S-REC.
           MOVE "R002" TO S-REC.
           WRITE S-REC.
           CLOSE FS.
      *> LEG A - SHARING clause, no LOCK MODE: GR1 a).
           OPEN INPUT FA.
           OPEN INPUT FE.
           MOVE 1 TO KA.
           READ FA WITH LOCK.
           DISPLAY "A1=" STA " " A-REC.
           MOVE 1 TO KE.
           READ FE.
           DISPLAY "A2=" STE " " E-REC.
           CLOSE FA.
           CLOSE FE.
      *> LEG B - no SHARING clause, no LOCK MODE, OPEN SHARING phrase:
      *> GR1 b) 1.
           OPEN INPUT SHARING WITH READ ONLY FB.
           OPEN INPUT FE.
           MOVE 1 TO KB.
           READ FB WITH LOCK.
           DISPLAY "B1=" STB " " B-REC.
           MOVE 1 TO KE.
           READ FE.
           DISPLAY "B2=" STE " " E-REC.
           CLOSE FB.
           CLOSE FE.
      *> LEG C - CONTROL: LEG B plus LOCK MODE IS MANUAL.
           OPEN INPUT SHARING WITH READ ONLY FC.
           OPEN INPUT FE.
           MOVE 1 TO KC.
           READ FC WITH LOCK.
           DISPLAY "C1=" STC " " C-REC.
           MOVE 1 TO KE.
           READ FE.
           DISPLAY "C2=" STE.
           CLOSE FC.
           CLOSE FE.
           STOP RUN.
