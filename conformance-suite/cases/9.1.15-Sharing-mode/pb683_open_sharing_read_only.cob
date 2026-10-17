*>--- conformance-case
*> id: pb683_open_sharing_read_only
*> rules: [GR-9.1.15-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §9.1.15 — "The SHARING phrase on an OPEN statement overrides
      *> the SHARING clause in the file control entry for establishing the
      *> sharing mode."  A connector whose SELECT declares NEITHER a SHARING
      *> clause NOR a LOCK MODE clause is therefore open FOR FILE SHARING
      *> when its own OPEN statement writes SHARING WITH READ ONLY, and
      *> §9.1.15 item 2 says of that mode "Record locks are in effect".
      *> §9.1.16 — "While locked by a given file connector, a record is not
      *> accessible to another file connector in the same or a different run
      *> unit, except by the execution of a READ statement with the IGNORING
      *> LOCK phrase."  So the READ below is refused, and §14.9.30.4 GR9/GR10
      *> b) price the refusal: "A value is placed into the I-O status
      *> associated with file-name-1 to indicate the record operation
      *> conflict condition" — '51' (§9.1.13.8 item 1), and no record is made
      *> available (GR10 c).
      *>
      *> ⛔ WHY THIS GOLDEN EXISTS (kb/Work PB683).  Every operand of that
      *> derivation is a RUN-TIME fact.  The emitter used to decide record-
      *> lock governance from the file control entry and the statement's own
      *> phrases — both compile-time facts — and routed an unphrased READ on
      *> a connector like B or C to an UNGOVERNED runtime entry, which
      *> answered '00' and handed over a record another connector had locked.
      *> Leg C is the discriminator that made the defect unmistakable: RETRY
      *> 0 TIMES is a behavioural NO-OP by §14.7.9.3 GR4 a) — "If the RETRY
      *> phrase is not specified or the result of the evaluation of
      *> arithmetic-expression-1 or arithmetic-expression-2 is negative or
      *> zero, the statement is unsuccessful" — yet writing it changed the
      *> answer from '00' to '51', because the compile-time predicate could
      *> see the RETRY phrase and could not see the OPEN's SHARING phrase.
      *> B and C shall now agree, on all three organizations.
      *>
      *> Each leg is one physical file with two connectors.  A carries the
      *> clauses (SHARING WITH ALL OTHER needs a LOCK MODE clause by
      *> §14.9.27.3 SR8) and locks the first record with an explicit WITH
      *> LOCK phrase, legal under MANUAL locking (§12.4.5.9 GR5).  B and C
      *> carry NO sharing or locking clause at all and become sharing
      *> participants only through their own OPEN statement.  Table 19
      *> permits the opens: A is INPUT, and §9.1.15 item 2 refuses a READ
      *> ONLY connector only when another connector's "open mode is other
      *> than input".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB683RDO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SA ASSIGN TO "pb683ro1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SA-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT SB ASSIGN TO "pb683ro1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SB-ST.
           SELECT SC ASSIGN TO "pb683ro1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SC-ST.
           SELECT RA ASSIGN TO "pb683ro2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS RA-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT RB ASSIGN TO "pb683ro2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS RB-ST.
           SELECT RC ASSIGN TO "pb683ro2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS RC-ST.
           SELECT IA ASSIGN TO "pb683ro3.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IA-KEY
               FILE STATUS IS IA-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT IB ASSIGN TO "pb683ro3.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IB-KEY
               FILE STATUS IS IB-ST.
           SELECT IC ASSIGN TO "pb683ro3.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IC-KEY
               FILE STATUS IS IC-ST.
       DATA DIVISION.
       FILE SECTION.
       FD SA.
       01 SA-REC PIC X(4).
       FD SB.
       01 SB-REC PIC X(4).
       FD SC.
       01 SC-REC PIC X(4).
       FD RA.
       01 RA-REC PIC X(4).
       FD RB.
       01 RB-REC PIC X(4).
       FD RC.
       01 RC-REC PIC X(4).
       FD IA.
       01 IA-REC.
          05 IA-KEY PIC X(4).
       FD IB.
       01 IB-REC.
          05 IB-KEY PIC X(4).
       FD IC.
       01 IC-REC.
          05 IC-KEY PIC X(4).
       WORKING-STORAGE SECTION.
       01 SA-ST PIC XX.
       01 SB-ST PIC XX.
       01 SC-ST PIC XX.
       01 RA-ST PIC XX.
       01 RB-ST PIC XX.
       01 RC-ST PIC XX.
       01 IA-ST PIC XX.
       01 IB-ST PIC XX.
       01 IC-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> ── SEQUENTIAL organization ──────────────────────────────────────
           OPEN OUTPUT SA.
           MOVE "R001" TO SA-REC. WRITE SA-REC.
           MOVE "R002" TO SA-REC. WRITE SA-REC.
           CLOSE SA.
           OPEN INPUT SA.
           READ SA WITH LOCK END-READ.
           DISPLAY "S-A=" SA-ST.
           OPEN INPUT SHARING WITH READ ONLY SB.
           DISPLAY "S-OB=" SB-ST.
           READ SB END-READ.
           DISPLAY "S-B=" SB-ST.
           OPEN INPUT SHARING WITH READ ONLY SC.
           READ SC RETRY 0 TIMES END-READ.
           DISPLAY "S-C=" SC-ST.
           CLOSE SA. CLOSE SB. CLOSE SC.
      *> ── RELATIVE organization ────────────────────────────────────────
           OPEN OUTPUT RA.
           MOVE "R001" TO RA-REC. WRITE RA-REC.
           MOVE "R002" TO RA-REC. WRITE RA-REC.
           CLOSE RA.
           OPEN INPUT RA.
           READ RA WITH LOCK END-READ.
           DISPLAY "R-A=" RA-ST.
           OPEN INPUT SHARING WITH READ ONLY RB.
           DISPLAY "R-OB=" RB-ST.
           READ RB END-READ.
           DISPLAY "R-B=" RB-ST.
           OPEN INPUT SHARING WITH READ ONLY RC.
           READ RC RETRY 0 TIMES END-READ.
           DISPLAY "R-C=" RC-ST.
           CLOSE RA. CLOSE RB. CLOSE RC.
      *> ── INDEXED organization ─────────────────────────────────────────
           OPEN OUTPUT IA.
           MOVE "K001" TO IA-REC. WRITE IA-REC.
           MOVE "K002" TO IA-REC. WRITE IA-REC.
           CLOSE IA.
           OPEN INPUT IA.
           READ IA WITH LOCK END-READ.
           DISPLAY "I-A=" IA-ST.
           OPEN INPUT SHARING WITH READ ONLY IB.
           DISPLAY "I-OB=" IB-ST.
           READ IB END-READ.
           DISPLAY "I-B=" IB-ST.
           OPEN INPUT SHARING WITH READ ONLY IC.
           READ IC RETRY 0 TIMES END-READ.
           DISPLAY "I-C=" IC-ST.
           CLOSE IA. CLOSE IB. CLOSE IC.
           STOP RUN.
