      *> ISO §12.4.5.9.4 GR1 b) 2. — clause-less connector sets no lock
      *> RULE §12.4.5.9.4 GR1 (cite.py --check 12.4.5.9.4 OK, 1) b) 2.):
      *>   "If the LOCK MODE clause is omitted from a file control
      *>   entry, ... b) If there is no SHARING clause in that file
      *>   control entry, ... 2. If an OPEN statement for the
      *>   associated file connector has no SHARING phrase, the type
      *>   of record locking for that opening of a shared file
      *>   associated with that file connector is defined by the
      *>   implementor. The implementor may ... specify that the
      *>   default is no record locking."
      *> The implementor's choice (docs/CONFORMANCE.md DOC-A.1-153) is
      *>   NO RECORD LOCKING: such a connector sets no record lock, and
      *>   an explicit WITH LOCK phrase obtains nothing. Branches a) and
      *>   b) 1. are 2002/l1_lock_mode_omitted_sets_no_lock; this
      *>   program is the discriminating pin of b) 2. that golden left
      *>   open (its blocker, kb/Work PB322, has landed).
      *> SHAPE. Two legs, each: connector X opens INPUT with NO SHARING
      *>   phrase, the OBSERVER FE opens INPUT, X reads record 1 WITH
      *>   LOCK, then FE reads record 1. The legs differ ONLY in X's
      *>   file control entry:
      *>   LEG X  FX: neither a SHARING nor a LOCK MODE clause: the
      *>          subject, GR1 b) 2.
      *>   LEG M  FM: FX plus LOCK MODE IS MANUAL, the CONTROL: it shows
      *>          FE really does see a lock its opener sets, so X2='00'
      *>          is the absence of a lock and not a blind observer.
      *> SHARING MODES. §9.1.15: with no phrase and no SHARING clause
      *>   "the implementor defines the sharing mode"; the choice
      *>   (DOC-A.1-131, 2002/pb322_default_sharing_by_open_mode) is
      *>   SHARING WITH READ ONLY for OPEN INPUT, and a LOCK MODE clause
      *>   alone is not a sharing specification, so FX and FM both open
      *>   in the read only mode, where "Record locks are in effect"
      *>   (§9.1.15 2)); "The rules are the same for a given standard
      *>   sharing mode regardless of whether the sharing mode is ...
      *>   specified as the default by the implementor" (§9.1.15).
      *> The OBSERVER FE: SHARING WITH READ ONLY + LOCK MODE IS MANUAL,
      *>   so §14.9.30.4 GR9 applies to its READ: a record locked by
      *>   another file connector is a record operation conflict and,
      *>   with no RETRY phrase, I-O status 51 (§9.1.13.8 1)). Its plain
      *>   READ sets no lock of its own (§14.9.30.4 GR11 d)).
      *> WITH LOCK is legal on FX and FM: neither specifies automatic
      *>   locking (§14.9.30.3 SR4).
      *> DERIVATION of every output line (records R001, R002 seeded):
      *>   SEED=00     the seeding OPEN OUTPUT meets no other connector.
      *>   X-OPEN=00 00  FX: READ ONLY input, nothing else open; FE:
      *>               Table 19, a READ ONLY input request beside a READ
      *>               ONLY input connector is a normal open.
      *>   X1=00 R001  FX's READ finds record 1, which nobody locks.
      *>   X2=00 R001  GR1 b) 2. with the no-record-locking default: FX
      *>               set no lock, so FE's READ meets none - '00'.
      *>   M-OPEN=00 00  as X-OPEN (FM has the same sharing mode).
      *>   M1=00 R001  FM's READ WITH LOCK: manual mode, LOCK phrase
      *>               written, locks in effect (read only mode) - the
      *>               lock on record 1 IS set (§12.4.5.9.4 GR5,
      *>               §14.9.30.4 GR11 d)).
      *>   M2=51       FE's READ of record 1, locked by FM: GR9 conflict
      *>               no RETRY - '51'; the record area is not shown.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GN1LKD01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "gn1lkd01.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST0.
           SELECT FX ASSIGN TO "gn1lkd01.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS KX
               FILE STATUS IS STX.
           SELECT FM ASSIGN TO "gn1lkd01.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS KM
               FILE STATUS IS STM
               LOCK MODE IS MANUAL.
           SELECT FE ASSIGN TO "gn1lkd01.dat"
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
       FD FX.
       01 X-REC PIC X(4).
       FD FM.
       01 M-REC PIC X(4).
       FD FE.
       01 E-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST0 PIC XX.
       01 STX PIC XX.
       01 STM PIC XX.
       01 STE PIC XX.
       01 KX  PIC 9(4).
       01 KM  PIC 9(4).
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
           DISPLAY "SEED=" ST0.
      *> LEG X - no SHARING clause, no LOCK MODE, no OPEN phrase:
      *> GR1 b) 2.
           OPEN INPUT FX.
           OPEN INPUT FE.
           DISPLAY "X-OPEN=" STX " " STE.
           MOVE 1 TO KX.
           READ FX WITH LOCK.
           DISPLAY "X1=" STX " " X-REC.
           MOVE 1 TO KE.
           READ FE.
           DISPLAY "X2=" STE " " E-REC.
           CLOSE FX.
           CLOSE FE.
      *> LEG M - CONTROL: LEG X plus LOCK MODE IS MANUAL.
           OPEN INPUT FM.
           OPEN INPUT FE.
           DISPLAY "M-OPEN=" STM " " STE.
           MOVE 1 TO KM.
           READ FM WITH LOCK.
           DISPLAY "M1=" STM " " M-REC.
           MOVE 1 TO KE.
           READ FE.
           DISPLAY "M2=" STE.
           CLOSE FM.
           CLOSE FE.
           STOP RUN.
