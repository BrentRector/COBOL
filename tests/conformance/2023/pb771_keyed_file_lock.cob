       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB771KEY.
      *> kb/Work PB771 — ISO 9.1.15's FILE LOCK is owed by EVERY
      *> organization. 9.1.6: "There are three organizations: sequential,
      *> relative, and indexed", and 9.1.15 3) names none of them — "The
      *> successful opening of a file establishes a file lock for the
      *> applicable sharing rules, thereby preventing other run units from
      *> opening that file with incompatible sharing rules."
      *>
      *> RELATIVE and INDEXED held no host handle at all between OPEN and
      *> CLOSE: their whole record store was loaded at the OPEN and rewritten
      *> at the CLOSE through short-lived bookkeeping handles, so SHARING
      *> WITH NO OTHER — 9.1.15 1)'s "exclusive access to a physical file" —
      *> protected a keyed file from nothing outside the run unit. They now
      *> hold one, postured by 9.1.15's three rules and given back at the
      *> CLOSE ("The file lock is removed by an explicit or implicit CLOSE
      *> statement executed for that file connector").
      *>
      *> A file lock is not observable from inside one run unit — it names no
      *> requester — so what a conformance program can and must certify is
      *> the OTHER half of 9.1.15, which taking a handle could silently
      *> break: the gate INSIDE the run unit stays the file connectors and
      *> Table 19, never the operating environment's handle. "Before access
      *> to a shared physical file is allowed through an OPEN statement, the
      *> sharing mode and the open mode of that OPEN statement shall be
      *> allowed by all other file connectors that are currently associated
      *> with the physical file, as described in 9.1.13, I-O status;
      *> 14.9.27, OPEN statement; and Table 19". (The lock ITSELF is measured
      *> from a handle outside the connector by
      *> FileLockPostureDriftTests.EveryOrganizationHoldsALiveFileLockWhileOpen,
      *> which no COBOL program can express.)
      *>
      *> The legs, and where each expected value comes from:
      *>
      *> L1 — RELATIVE, two connectors that declare SHARING WITH ALL OTHER
      *> (RE-DERIVED BY kb/Work PB322: two connectors with NO sharing
      *> specification are SHARING WITH READ ONLY for INPUT and SHARING WITH
      *> NO OTHER for EXTEND, docs/CONFORMANCE.md DOC-A.1-131, and Table 19
      *> refuses that pair), one INPUT and one EXTEND. Table 19 prints
      *> "Normal open" for ALL OTHER / EXTEND against all other / input, so
      *> both opens are '00' (14.9.27.4 GR1 with 9.1.13.2). The WRITE is '00' (14.9.51.4 GR12) and 14.9.51.4 GR29 a)
      *> fixes its record number — "a record number that is one greater than
      *> the highest relative record number existing in the physical file" —
      *> which is 3 over the two seeded records, moved back into the RELATIVE
      *> KEY item by the same rule. The INPUT connector's second READ
      *> delivers the SECOND record: 9.1.12 makes the file position indicator
      *> that connector's own, and a sibling's arrival does not move it.
      *>
      *> L2 — the control that the arbiter is untouched for RELATIVE: against
      *> a connector open SHARING WITH NO OTHER a second OPEN is unsuccessful
      *> with '61' — 9.1.13.9 1) a), "An attempt is made to open a physical
      *> file that is currently open by another file connector in the sharing
      *> with no other mode".
      *>
      *> L3 — INDEXED, the same ALL OTHER declaration with the ACCESS axis
      *> moved: an INPUT connector, then an I-O one at DYNAMIC access.
      *> 9.1.15 3) (SHARING WITH ALL OTHER) "allows concurrent access to a
      *> physical file through other file connectors specifying input, I-O,
      *> or extend mode", so Table 19 prints "Normal open" and both are '00'.
      *> The WRITE is '00' because 14.9.27.4 GR8's Table 20 lists WRITE under
      *> the I-O column for random and dynamic access, and 14.9.51.4 GR39
      *> lets a dynamic-access indexed WRITE release keys in any order.
      *>
      *> L4 — the same '61' control for INDEXED.
      *>
      *> NREL / NIXD — the records in each physical file afterwards, read
      *> back through a fresh OPEN. 14.9.10.4 GR5's "the physical file" is
      *> ONE medium, so a CLOSE persists what the run unit holds rather than
      *> one connector's snapshot: three records each, the two seeded plus
      *> the one released.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-RS ASSIGN TO "pb771rel.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RS-KEY
               FILE STATUS IS RS-ST.
           SELECT F-RA ASSIGN TO "pb771rel.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RA-KEY
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS RA-ST.
           SELECT F-RB ASSIGN TO "pb771rel.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RB-KEY
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS RB-ST.
           SELECT F-RX ASSIGN TO "pb771rel.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RX-KEY
               SHARING WITH NO OTHER
               FILE STATUS IS RX-ST.
           SELECT F-IS ASSIGN TO "pb771ixd.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IS-K
               FILE STATUS IS IS-ST.
           SELECT F-IA ASSIGN TO "pb771ixd.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IA-K
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS IA-ST.
           SELECT F-IB ASSIGN TO "pb771ixd.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IB-K
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS IB-ST.
           SELECT F-IX ASSIGN TO "pb771ixd.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-K
               SHARING WITH NO OTHER
               FILE STATUS IS IX-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-RS.
       01 RS-REC PIC X(4).
       FD F-RA.
       01 RA-REC PIC X(4).
       FD F-RB.
       01 RB-REC PIC X(4).
       FD F-RX.
       01 RX-REC PIC X(4).
       FD F-IS.
       01 IS-REC.
          05 IS-K  PIC X(4).
          05 IS-D  PIC X(4).
       FD F-IA.
       01 IA-REC.
          05 IA-K  PIC X(4).
          05 IA-D  PIC X(4).
       FD F-IB.
       01 IB-REC.
          05 IB-K  PIC X(4).
          05 IB-D  PIC X(4).
       FD F-IX.
       01 IX-REC.
          05 IX-K  PIC X(4).
          05 IX-D  PIC X(4).
       WORKING-STORAGE SECTION.
       01 RS-ST  PIC XX.
       01 RA-ST  PIC XX.
       01 RB-ST  PIC XX.
       01 RX-ST  PIC XX.
       01 IS-ST  PIC XX.
       01 IA-ST  PIC XX.
       01 IB-ST  PIC XX.
       01 IX-ST  PIC XX.
       01 RS-KEY PIC 9(3) VALUE 0.
       01 RA-KEY PIC 9(3) VALUE 0.
       01 RB-KEY PIC 9(3) VALUE 0.
       01 RX-KEY PIC 9(3) VALUE 0.
       01 NREL   PIC 9 VALUE 0.
       01 NIXD   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           OPEN OUTPUT F-RS
           MOVE "AAAA" TO RS-REC
           WRITE RS-REC
           MOVE "BBBB" TO RS-REC
           WRITE RS-REC
           CLOSE F-RS

      *> L1 — RELATIVE: the ALL OTHER pair Table 19 permits.
           OPEN INPUT F-RA
           DISPLAY "L1-A=" RA-ST
           READ F-RA
           DISPLAY "L1-R1=" RA-REC " " RA-ST
           OPEN EXTEND F-RB
           DISPLAY "L1-B=" RB-ST
           MOVE "CCCC" TO RB-REC
           WRITE RB-REC
           DISPLAY "L1-W=" RB-ST " L1-K=" RB-KEY
           READ F-RA
           DISPLAY "L1-R2=" RA-REC " " RA-ST
           CLOSE F-RB
           CLOSE F-RA

      *> L2 — RELATIVE: SHARING WITH NO OTHER is still exclusive.
           OPEN INPUT F-RX
           DISPLAY "L2-X=" RX-ST
           OPEN INPUT F-RA
           DISPLAY "L2-A=" RA-ST
           CLOSE F-RX

           OPEN OUTPUT F-IS
           MOVE "K001AAAA" TO IS-REC
           WRITE IS-REC
           MOVE "K002BBBB" TO IS-REC
           WRITE IS-REC
           CLOSE F-IS

      *> L3 — INDEXED: an INPUT connector, then an I-O one that releases.
           OPEN INPUT F-IB
           DISPLAY "L3-B=" IB-ST
           OPEN I-O F-IA
           DISPLAY "L3-A=" IA-ST
           MOVE "K003CCCC" TO IA-REC
           WRITE IA-REC
           DISPLAY "L3-W=" IA-ST
           CLOSE F-IA
           CLOSE F-IB

      *> L4 — INDEXED: the same '61' control.
           OPEN INPUT F-IX
           DISPLAY "L4-X=" IX-ST
           OPEN INPUT F-IB
           DISPLAY "L4-B=" IB-ST
           CLOSE F-IX

           OPEN INPUT F-RS
           PERFORM 10 TIMES
               READ F-RS
                   AT END EXIT PERFORM
               END-READ
               ADD 1 TO NREL
           END-PERFORM
           CLOSE F-RS
           OPEN INPUT F-IS
           PERFORM 10 TIMES
               READ F-IS
                   AT END EXIT PERFORM
               END-READ
               ADD 1 TO NIXD
           END-PERFORM
           CLOSE F-IS
           DISPLAY "NREL=" NREL " NIXD=" NIXD
           STOP RUN.
