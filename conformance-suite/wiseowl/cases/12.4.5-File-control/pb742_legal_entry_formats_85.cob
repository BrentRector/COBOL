*>--- conformance-case
*> id: pb742_legal_entry_formats_85
*> rules: [SR-12.4.5.2-8, SR-12.4.5.2-9, GR-12.4.5.3-5, GR-12.4.5.3-7,
*>    GR-12.4.5.3-8, GR-12.4.5.3-9, SR-12.4.5.6.3-2, SR-12.4.5.12.3-2]
*> accept: [85]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 12.4.5 - the LEGAL twin of the pb742-* / pb743-* negatives, and the
      *> over-rejection guard on the two rules they add (kb/Work PB742, PB743). Every shape the new
      *> rows could get wrong is here and must BIND AND RUN:
      *>   - a SEQUENTIAL file with NO key clause and no ORGANIZATION clause at all, i.e. the shape
      *>     12.4.5.10.3 GR6 makes record sequential. It is the population SR8's row is screened
      *>     over, so it is the one that proves the row fires on the CLAUSE and not on the
      *>     organization (SQF, below - it is read and written).
      *>   - an INDEXED file whose prime RECORD KEY is a GROUP item: 12.4.5.12.3 SR2's category arm
      *>     admits it because 13.18.29.4 GR3 makes "a group item for which a GROUP-USAGE clause is
      *>     not specified or implied" an ALPHANUMERIC GROUP ITEM. A category screen written as a
      *>     bare elementary-PICTURE test would refuse this, so it is the category arm's boundary.
      *>   - an ALTERNATE RECORD KEY that is a plain PIC X elementary item, inside the record.
      *>   - a RELATIVE file whose RELATIVE KEY is a NUMERIC item in WORKING-STORAGE. Three rules
      *>     meet on it and none may fire: 12.4.5.13.3 SR2 REQUIRES the unsigned integer that
      *>     12.4.5.12.3 SR2 forbids for a record key (so the category arm must not leak across key
      *>     roles), SR3 REQUIRES it outside the record, and SR9 admits the clause because the file
      *>     IS relative.
      *>   - a SORT-MERGE file described by an SD whose entry is a legal 12.4.5.1 Format 4 - ASSIGN
      *>     plus "[ ORGANIZATION IS ] SEQUENTIAL" and nothing else. This one is load-bearing: the
      *>     screen used to skip every sort-merge file with a blanket early return, and SR8/SR9's
      *>     second sentence is what replaced it, so a legal SD must still draw nothing.
      *> ⚠ A category-NATIONAL key is legal under both SR2s and is NOT exercised here: a national
      *> leaf inside a file record is refused by this compiler's own byte-addressed-surface rule
      *> (PicCategory.National, D-N2), so a golden for it would test that refusal, not SR2.
      *> DERIVATION - every expected line follows from the rules, nothing from the compiler:
      *>  · 12.4.5.12.4 GR1 makes the prime key unique and READ NEXT follows the key sequence, so the
      *>    indexed walk prints AA/BB/CC in that order whatever order they were written in.
      *>  · 12.4.5.13.4 GR1/GR2: a random READ names the record whose relative record number equals
      *>    the RELATIVE KEY's value, so R=0002 prints "TWO ".
      *>  · 14.9.40.4 GR8 a): SORT's ASCENDING key returns the lower key value first, so the drain
      *>    prints S=1 then S=2 from records released in the order 2, 1.
      *>  · The sequential file is written with one record and read back verbatim (9.1.x): SEQ=HELLO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB742LEGALFORMATS85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb742legal-sq.dat".
           SELECT IXF ASSIGN TO "pb742legal-ix.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-ALT WITH DUPLICATES.
           SELECT RLF ASSIGN TO "pb742legal-rl.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WS-RK.
           SELECT SRT ASSIGN TO "pb742legal-srt.tmp"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC PIC X(5).
       FD IXF.
       01 IX-REC.
          05 IX-KEY.
             10 IX-K1 PIC X.
             10 IX-K2 PIC X.
          05 IX-ALT PIC X(2).
          05 IX-DATA PIC X(4).
       FD RLF.
       01 RL-REC PIC X(4).
       SD SRT.
       01 SR-REC.
          05 SR-K PIC 9.
          05 SR-T PIC X(3).
       WORKING-STORAGE SECTION.
       01 WS-RK  PIC 9(4).
       01 WS-EOF PIC 9 VALUE 0.
       01 WS-SEOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SQF.
           MOVE "HELLO" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE SQF.
           OPEN INPUT SQF.
           READ SQF AT END CONTINUE END-READ.
           DISPLAY "SEQ=" SQ-REC.
           CLOSE SQF.
           OPEN OUTPUT IXF.
           MOVE "CC" TO IX-KEY. MOVE "ZZ" TO IX-ALT. MOVE "TRI " TO IX-DATA.
           WRITE IX-REC.
           MOVE "AA" TO IX-KEY. MOVE "ZZ" TO IX-ALT. MOVE "ONE " TO IX-DATA.
           WRITE IX-REC.
           MOVE "BB" TO IX-KEY. MOVE "YY" TO IX-ALT. MOVE "TWO " TO IX-DATA.
           WRITE IX-REC.
           CLOSE IXF.
           OPEN INPUT IXF.
           DISPLAY "PRIME WALK:".
           PERFORM UNTIL WS-EOF = 1
               READ IXF NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "  " IX-KEY " " IX-ALT " " IX-DATA
               END-READ
           END-PERFORM.
           CLOSE IXF.
           OPEN OUTPUT RLF.
           MOVE 1 TO WS-RK. MOVE "ONE " TO RL-REC. WRITE RL-REC.
           MOVE 2 TO WS-RK. MOVE "TWO " TO RL-REC. WRITE RL-REC.
           CLOSE RLF.
           OPEN INPUT RLF.
           MOVE 2 TO WS-RK.
           READ RLF INVALID KEY CONTINUE
               NOT INVALID KEY DISPLAY "R=" WS-RK " " RL-REC
           END-READ.
           CLOSE RLF.
           SORT SRT ON ASCENDING KEY SR-K
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           DISPLAY "DONE".
           STOP RUN.
       FEED.
           MOVE 2 TO SR-K MOVE "BBB" TO SR-T RELEASE SR-REC.
           MOVE 1 TO SR-K MOVE "AAA" TO SR-T RELEASE SR-REC.
       DRAIN.
           PERFORM UNTIL WS-SEOF = "Y"
               RETURN SRT RECORD
                   AT END MOVE "Y" TO WS-SEOF
                   NOT AT END DISPLAY "S=" SR-K " " SR-T
               END-RETURN
           END-PERFORM.
