*>--- conformance-case
*> id: pb683_open_sharing_no_other
*> rules: [GR-9.1.15-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §9.1.15 — "The SHARING phrase on an OPEN statement overrides
      *> the SHARING clause in the file control entry for establishing the
      *> sharing mode.  If there is no SHARING phrase on the OPEN statement,
      *> the sharing mode is determined by the SHARING clause in the file
      *> control entry."  The companion of pb683_open_sharing_read_only for
      *> the OTHER spelling a clause-less SELECT can acquire at OPEN time
      *> (kb/Work PB683), on all three organizations.
      *>
      *> §9.1.15 item 1 — "The sharing with no other mode specifies exclusive
      *> access to a physical file.  Associating this file connector with the
      *> physical file will be unsuccessful if the physical file is currently
      *> open through other file connectors.  If the OPEN statement is
      *> successful, subsequent requests to open the physical file through
      *> other file connectors before this file connector is closed will be
      *> unsuccessful.  Record locks are ignored."  Four observations, and
      *> the golden makes all four, in both directions:
      *>   OPEN N first  → N='00', then S's plain OPEN is refused  ('61')
      *>   OPEN S first  → S='00', then N's NO OTHER OPEN is refused ('61')
      *>   N's own unphrased READ, which is now routed through the record-
      *>   lock-governed path because N IS a sharing participant, still
      *>   answers '00': the governed route may not invent a conflict where
      *>   the standard says record locks are ignored.
      *> '61' is §9.1.13.9 item 1, "the sharing mode ... is incompatible" —
      *> §14.9.27.4 Table 19's Unsuccessful open cell.
      *>
      *> N's file control entry writes NO SHARING clause and NO LOCK MODE
      *> clause: every sharing fact about it comes from its own OPEN
      *> statement, which is exactly the fact a compile-time governance
      *> predicate could not see.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB683NOO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SS ASSIGN TO "pb683no1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SS-ST.
           SELECT SN ASSIGN TO "pb683no1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SN-ST.
           SELECT RS ASSIGN TO "pb683no2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS RS-ST.
           SELECT RN ASSIGN TO "pb683no2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS RN-ST.
           SELECT IS-F ASSIGN TO "pb683no3.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IS-KEY
               FILE STATUS IS IS-ST.
           SELECT IN-F ASSIGN TO "pb683no3.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IN-KEY
               FILE STATUS IS IN-ST.
       DATA DIVISION.
       FILE SECTION.
       FD SS.
       01 SS-REC PIC X(4).
       FD SN.
       01 SN-REC PIC X(4).
       FD RS.
       01 RS-REC PIC X(4).
       FD RN.
       01 RN-REC PIC X(4).
       FD IS-F.
       01 IS-REC.
          05 IS-KEY PIC X(4).
       FD IN-F.
       01 IN-REC.
          05 IN-KEY PIC X(4).
       WORKING-STORAGE SECTION.
       01 SS-ST PIC XX.
       01 SN-ST PIC XX.
       01 RS-ST PIC XX.
       01 RN-ST PIC XX.
       01 IS-ST PIC XX.
       01 IN-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> ── SEQUENTIAL organization ──────────────────────────────────────
           OPEN OUTPUT SS.
           MOVE "R001" TO SS-REC. WRITE SS-REC.
           MOVE "R002" TO SS-REC. WRITE SS-REC.
           CLOSE SS.
           OPEN INPUT SHARING WITH NO OTHER SN.
           DISPLAY "S-N=" SN-ST.
           OPEN INPUT SS.
           DISPLAY "S-S=" SS-ST.
           READ SN END-READ.
           DISPLAY "S-NR=" SN-ST.
           CLOSE SN.
           OPEN INPUT SS.
           DISPLAY "S-S2=" SS-ST.
           OPEN INPUT SHARING WITH NO OTHER SN.
           DISPLAY "S-N2=" SN-ST.
           CLOSE SS.
      *> ── RELATIVE organization ────────────────────────────────────────
           OPEN OUTPUT RS.
           MOVE "R001" TO RS-REC. WRITE RS-REC.
           MOVE "R002" TO RS-REC. WRITE RS-REC.
           CLOSE RS.
           OPEN INPUT SHARING WITH NO OTHER RN.
           DISPLAY "R-N=" RN-ST.
           OPEN INPUT RS.
           DISPLAY "R-S=" RS-ST.
           READ RN END-READ.
           DISPLAY "R-NR=" RN-ST.
           CLOSE RN.
           OPEN INPUT RS.
           DISPLAY "R-S2=" RS-ST.
           OPEN INPUT SHARING WITH NO OTHER RN.
           DISPLAY "R-N2=" RN-ST.
           CLOSE RS.
      *> ── INDEXED organization ─────────────────────────────────────────
           OPEN OUTPUT IS-F.
           MOVE "K001" TO IS-REC. WRITE IS-REC.
           MOVE "K002" TO IS-REC. WRITE IS-REC.
           CLOSE IS-F.
           OPEN INPUT SHARING WITH NO OTHER IN-F.
           DISPLAY "I-N=" IN-ST.
           OPEN INPUT IS-F.
           DISPLAY "I-S=" IS-ST.
           READ IN-F END-READ.
           DISPLAY "I-NR=" IN-ST.
           CLOSE IN-F.
           OPEN INPUT IS-F.
           DISPLAY "I-S2=" IS-ST.
           OPEN INPUT SHARING WITH NO OTHER IN-F.
           DISPLAY "I-N2=" IN-ST.
           CLOSE IS-F.
           STOP RUN.
