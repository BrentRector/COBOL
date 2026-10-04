       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB669V.
      *> kb/Work PB669. A locked record is inaccessible to EVERY other
      *> file connector, not only to the ones that declared a posture.
      *> ISO 9.1.16: "While locked by a given file connector, a record
      *> is not accessible to another file connector in the same or a
      *> different run unit, except by the execution of a READ statement
      *> with the IGNORING LOCK phrase." The sentence carries NO
      *> qualification on the reading connector's own LOCK MODE clause,
      *> and 12.4.5.9.4 GR1 a) / b) 1. are worded to say only that no
      *> record locks are SET through such a connector. The connector
      *> that SETS no lock still SEES every other connector's locks: it
      *> asks for none (no WITH LOCK phrase anywhere below), and this
      *> golden measures what it is refused.
      *>
      *> RE-DERIVED BY kb/Work PB322 (determination A). This golden used
      *> to open F-B with NEITHER a SHARING NOR a LOCK MODE clause, beside
      *> F-A, in the I-O mode. With no sharing phrase and no SHARING
      *> clause a connector opened I-O is SHARING WITH NO OTHER
      *> (9.1.15 / 14.9.27.4 GR23, docs/CONFORMANCE.md DOC-A.1-131), and
      *> Table 19 refuses it beside any other connector, so a clause-less
      *> I-O connector can never be open next to a locker. F-B therefore
      *> now takes its sharing mode from the OPEN's SHARING WITH ALL OTHER
      *> phrase (14.9.27.4 GR23), which 14.9.27.3 SR8 allows only beside
      *> a LOCK MODE clause in F-B's file control entry; F-B writes
      *> LOCK MODE IS MANUAL and never asks for a lock. The truly
      *> clause-less reader of a locked record is
      *> 2002/pb322_clauseless_reader_sees_record_lock.
      *>
      *> Before PB669 every governed verb in FileRegistry returned to
      *> the UNGOVERNED body when the connector was absent from the
      *> opt-in posture map, so READB, REWB and DELB below all answered
      *> '00' and DELB actually REMOVED a record another connector held
      *> locked. The same opt-in-overlay shape PB321 removed from OPEN.
      *>
      *> EXPECTED VALUES, COMPUTED FROM THE RULES:
      *>   READB  '51' - 14.9.30.4 GR9 with no RETRY phrase is the
      *>          record operation conflict condition (9.1.13.8 item 1).
      *>          Only the STATUS is displayed: GR10 c) makes the record
      *>          area's content undefined after the conflict.
      *>   IGNB   '00' ALPHA - 14.9.30.4 GR12, "the requested record is
      *>          made available, even if it is locked". The phrase is
      *>          admissible because F-B's effective LOCK MODE is not
      *>          AUTOMATIC (14.9.30.3 SR4).
      *>   REWB   '51' - 14.9.35.4 GR11, "the record identified for
      *>          rewriting is locked by another file connector"; GR14
      *>          then leaves the record and the record area unaffected.
      *>   DELB   '51' - 14.9.10.4 GR6, "the record identified for
      *>          deletion is locked by another file connector".
      *>   READB2 '00' - GR9 names the record IDENTIFIED for access, so
      *>          an unlocked record of the same physical file is
      *>          unaffected; record 2 is not locked.
      *>   READA2 '00' - the REVERSE direction. F-B sets no lock at all
      *>          (12.4.5.9.4 GR1 b) 2., above), so the MANUAL connector
      *>          reads the record F-B just read with no conflict.
      *>   AFTER  '00' ALPHA - 14.9.47 GR1: UNLOCK releases every record
      *>          lock F-A holds, and record 1 is available again with
      *>          the content REWB failed to replace.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-A ASSIGN TO "pb669v.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS A-KEY
               FILE STATUS IS A-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-B ASSIGN TO "pb669v.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS B-KEY
               FILE STATUS IS B-ST
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD F-A.
       01 A-REC PIC X(5).
       FD F-B.
       01 B-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 A-KEY PIC 9(4).
       01 B-KEY PIC 9(4).
       01 A-ST  PIC XX.
       01 B-ST  PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F-B.
           MOVE 1 TO B-KEY. MOVE "ALPHA" TO B-REC. WRITE B-REC.
           MOVE 2 TO B-KEY. MOVE "BRAVO" TO B-REC. WRITE B-REC.
           CLOSE F-B.
           OPEN I-O F-A. DISPLAY "OPENA=" A-ST.
           OPEN I-O SHARING WITH ALL OTHER F-B. DISPLAY "OPENB=" B-ST.
           MOVE 1 TO A-KEY. READ F-A WITH LOCK.
           DISPLAY "READA=" A-ST " " A-REC.
           MOVE 1 TO B-KEY. READ F-B.
           DISPLAY "READB=" B-ST.
           MOVE 1 TO B-KEY. READ F-B IGNORING LOCK.
           DISPLAY "IGNB=" B-ST " " B-REC.
           MOVE 1 TO B-KEY. MOVE "ZULU " TO B-REC. REWRITE B-REC.
           DISPLAY "REWB=" B-ST.
           MOVE 1 TO B-KEY. DELETE F-B RECORD.
           DISPLAY "DELB=" B-ST.
           MOVE 2 TO B-KEY. READ F-B.
           DISPLAY "READB2=" B-ST " " B-REC.
           MOVE 2 TO A-KEY. READ F-A.
           DISPLAY "READA2=" A-ST " " A-REC.
           UNLOCK F-A.
           MOVE 1 TO B-KEY. READ F-B.
           DISPLAY "AFTER=" B-ST " " B-REC.
           CLOSE F-A. CLOSE F-B.
           STOP RUN.
