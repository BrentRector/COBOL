      *>--- conformance-case
      *> id: l1_read_lock_mode_governs
      *> rules: [GR-12.4.5.9.4-4, GR-14.9.30.4-7, GR-14.9.30.4-11]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.30.4 GR7 — "Whether record locking is in effect is
      *> determined by the rules specified in 12.4.5.9, LOCK MODE
      *> clause."  A pure delegation rule: the only thing it can be
      *> observed to do is make the READ's locking behaviour follow the
      *> file control entry, so the two legs below differ in NOTHING
      *> but the presence of the LOCK MODE clause.
      *> 12.4.5.9 GR1 a) "If the LOCK MODE clause is omitted from a file
      *>   control entry, ... If there is a SHARING clause in that file
      *>   control entry, no record locks are set by the execution of
      *>   I-O statements through the associated file connector."
      *> 12.4.5.9 GR4 "If the AUTOMATIC phrase is specified, the lock
      *>   mode is automatic.  Records are locked when any READ
      *>   statement is executed."
      *>
      *> LEG 1 (F1/F2, SHARING but no LOCK MODE).  F1 reads record 1
      *> WITH LOCK — §14.9.30.3 SR4 permits the phrase precisely because
      *> automatic locking was NOT specified — and GR1a means no lock is
      *> set, so F2 reads the same record successfully ('00').
      *> ⛔ WHY "READ ONLY" AND NOT "ALL OTHER" ON LEG 1 — DO NOT
      *> "SIMPLIFY" THESE TWO SELECTS TO MATCH F3/F4.  §14.9.27.3 SR8:
      *>   "When file-name-1 is not subject to an APPLY COMMIT clause,
      *>   then if the sharing phrase is omitted from the OPEN statement
      *>   and the ALL phrase is specified in the SHARING clause of the
      *>   file control entry for file-name-1 or if the ALL phrase is
      *>   specified on the OPEN statement, the LOCK MODE clause shall
      *>   be specified in the file control entry for file-name-1."
      *> LEG 1's whole point is a file control entry with NO LOCK MODE
      *> clause, so the ALL phrase is not available to it: SHARING WITH
      *> ALL OTHER here is non-conforming source and is rejected at the
      *> SELECT itself, before any OPEN is looked at (COBOLNET1512,
      *> DataBinder; see tests/conformance/negative/
      *> sharing-all-no-lockmode.cob, which carries no OPEN at all).
      *> Nor can an OPEN … SHARING WITH READ ONLY phrase rescue it —
      *> §14.9.27.4 GR23 lets the phrase override the clause, but this
      *> build rejects the entry regardless.  SHARING WITH READ ONLY on
      *> the SELECT is therefore the only legal carrier, and it keeps
      *> every conjunct of the derivation — it is still "a SHARING
      *> clause in that file control entry" so §12.4.5.9.4 GR1a applies
      *> verbatim, §9.1.15 item 2 says of it "Record locks are in
      *> effect" and permits a second connector in INPUT mode (Table 19,
      *> read only/input × READ ONLY/INPUT = normal open) — and it does
      *> not name the ALL phrase, so SR8 never fires.  F3/F4 keep ALL
      *> OTHER legally: they carry LOCK MODE IS AUTOMATIC.
      *> LEG 2 (F3/F4, SHARING plus LOCK MODE IS AUTOMATIC).  F3's PLAIN
      *> read locks record 1 (GR4), so F4's read of it is refused with
      *> the record operation conflict status '51' (§14.9.30 GR9,
      *> §9.1.13.8) and no record is made available.
      *> R2='00' against R4='51' IS the delegation: identical
      *> statements, opposite outcomes, decided only by 12.4.5.9.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RD07A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "l1rd07a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST0.
           SELECT F1 ASSIGN TO "l1rd07a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K1
               FILE STATUS IS ST1
               SHARING WITH READ ONLY.
           SELECT F2 ASSIGN TO "l1rd07a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K2
               FILE STATUS IS ST2
               SHARING WITH READ ONLY.
           SELECT F3 ASSIGN TO "l1rd07a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K3
               FILE STATUS IS ST3
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC.
           SELECT F4 ASSIGN TO "l1rd07a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K4
               FILE STATUS IS ST4
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 S-REC PIC X(4).
       FD F1.
       01 R1-REC PIC X(4).
       FD F2.
       01 R2-REC PIC X(4).
       FD F3.
       01 R3-REC PIC X(4).
       FD F4.
       01 R4-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST0 PIC XX.
       01 ST1 PIC XX.
       01 ST2 PIC XX.
       01 ST3 PIC XX.
       01 ST4 PIC XX.
       01 K1  PIC 9(4).
       01 K2  PIC 9(4).
       01 K3  PIC 9(4).
       01 K4  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> Seed two records through an ordinary, non-sharing connector.
           OPEN OUTPUT FS.
           MOVE "R001" TO S-REC.
           WRITE S-REC.
           MOVE "R002" TO S-REC.
           WRITE S-REC.
           CLOSE FS.
      *> LEG 1 - no LOCK MODE clause: 12.4.5.9 GR1a, no locks are set.
           OPEN INPUT F1.
           OPEN INPUT F2.
           MOVE 1 TO K1.
           READ F1 WITH LOCK.
           DISPLAY "R1=" ST1 " " R1-REC.
           MOVE 1 TO K2.
           READ F2.
           DISPLAY "R2=" ST2 " " R2-REC.
           CLOSE F1.
           CLOSE F2.
      *> LEG 2 - LOCK MODE IS AUTOMATIC: 12.4.5.9 GR4, any READ locks.
           OPEN INPUT F3.
           OPEN INPUT F4.
           MOVE 1 TO K3.
           READ F3.
           DISPLAY "R3=" ST3 " " R3-REC.
           MOVE 1 TO K4.
           READ F4.
           DISPLAY "R4=" ST4.
           CLOSE F3.
           CLOSE F4.
           STOP RUN.
