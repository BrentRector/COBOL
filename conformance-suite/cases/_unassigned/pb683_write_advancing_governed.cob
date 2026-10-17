*>--- conformance-case
*> id: pb683_write_advancing_governed
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.51.4 GR11 (ALL FILES) — "If record locks have an effect
      *> for the write file connector and the WITH LOCK phrase is specified
      *> or implied, the record lock associated with the record written is
      *> set when the execution of the WRITE statement is successful."  GR10
      *> and GR11 are ALL FILES rules, and §14.9.51.2 Format 1 prints the
      *> ADVANCING phrase, the retry-phrase and the WITH LOCK bracket
      *> TOGETHER, so `WRITE R BEFORE ADVANCING 1 LINE WITH LOCK` is one
      *> legal statement that both advances the medium and locks the record.
      *> §14.9.51.4 GR12 — "The successful execution of a WRITE statement
      *> releases a logical record to the operating environment" — is what
      *> gives a print-control WRITE a record to lock at all.
      *> The other connector is then refused by §9.1.16 ("While locked by a
      *> given file connector, a record is not accessible to another file
      *> connector in the same or a different run unit") with the record
      *> operation conflict status of §14.9.30.4 GR9/GR10 b), '51'.
      *>
      *> ⛔ WHY THIS GOLDEN EXISTS (kb/Work PB683).  The emitter rendered
      *> three different runtime entries for WRITE — plain, ADVANCING, and
      *> the COBOL-2023 combined BEFORE-AND-AFTER form — and only the plain
      *> one was record-lock governed.  Neither print-control entry had a
      *> lock or RETRY parameter at all, so both phrases were silently
      *> dropped and leg A answered '10' where leg B answered '51'.  The
      *> three legs below are the SAME statement in the three printed
      *> shapes; they shall now agree.
      *>
      *> Each leg is one physical file with two connectors, both SHARING
      *> WITH ALL OTHER (which §14.9.27.3 SR8 makes conditional on a LOCK
      *> MODE clause) and LOCK MODE IS MANUAL, so the lock is set only by
      *> the explicit WITH LOCK phrase (§12.4.5.9 GR5).  The reader's
      *> §14.9.30.4 GR9 conflict check precedes the physical retrieval, so
      *> the '51' does not depend on the writer's stream having reached the
      *> medium.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB683ADV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT AW ASSIGN TO "pb683ad1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS AW-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT AR ASSIGN TO "pb683ad1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS AR-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT BW ASSIGN TO "pb683ad2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS BW-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT BR ASSIGN TO "pb683ad2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS BR-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT CW ASSIGN TO "pb683ad3.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS CW-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT CR ASSIGN TO "pb683ad3.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS CR-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD AW.
       01 AW-REC PIC X(5).
       FD AR.
       01 AR-REC PIC X(5).
       FD BW.
       01 BW-REC PIC X(5).
       FD BR.
       01 BR-REC PIC X(5).
       FD CW.
       01 CW-REC PIC X(5).
       FD CR.
       01 CR-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 AW-ST PIC XX.
       01 AR-ST PIC XX.
       01 BW-ST PIC XX.
       01 BR-ST PIC XX.
       01 CW-ST PIC XX.
       01 CR-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> LEG A — the single ADVANCING phrase (§14.9.51.2 Format 1).
           OPEN OUTPUT AW.
           MOVE "ALPHA" TO AW-REC.
           WRITE AW-REC BEFORE ADVANCING 1 LINE WITH LOCK.
           DISPLAY "A-W=" AW-ST.
           OPEN INPUT AR.
           READ AR END-READ.
           DISPLAY "A-R=" AR-ST.
           CLOSE AW. CLOSE AR.
      *> LEG B — the control: the same statement with no ADVANCING phrase.
           OPEN OUTPUT BW.
           MOVE "ALPHA" TO BW-REC.
           WRITE BW-REC WITH LOCK.
           DISPLAY "B-W=" BW-ST.
           OPEN INPUT BR.
           READ BR END-READ.
           DISPLAY "B-R=" BR-ST.
           CLOSE BW. CLOSE BR.
      *> LEG C — COBOL-2023's combined form, in the ONE spelling §14.9.51.2
      *> Format 1 prints: both WORDS, one ADVANCING, one operand (the choice
      *> indicators enclose the words only — kb/Work PB712 corrected this leg,
      *> which used to carry two ADVANCING operands). §14.9.51.4 GR25 e)/f)
      *> place the single advance after the presentation; SR17 forbids PAGE.
           OPEN OUTPUT CW.
           MOVE "ALPHA" TO CW-REC.
           WRITE CW-REC BEFORE AFTER ADVANCING 1 LINE
               WITH LOCK.
           DISPLAY "C-W=" CW-ST.
           OPEN INPUT CR.
           READ CR END-READ.
           DISPLAY "C-R=" CR-ST.
           CLOSE CW. CLOSE CR.
           STOP RUN.
