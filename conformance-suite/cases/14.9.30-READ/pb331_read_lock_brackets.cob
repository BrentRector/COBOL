*>--- conformance-case
*> id: pb331_read_lock_brackets
*> rules: [SR-14.9.30.3-3, SR-14.9.30.3-10, SR-14.9.30.3-11, SR-14.9.30.3-12, FMT-14.9.30.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB331LKB.
      *> !! THE PRINTED READ FORMATS, WRITTEN AS THE STANDARD PRINTS THEM (kb/Work PB331).
      *> ISO 14.9.30.2 (PDF page 722, RENDERED) gives the READ lock options as TWO
      *> INDEPENDENT PLAIN BRACKETS followed by the KEY phrase:
      *>     [ ADVANCING ON LOCK | IGNORING LOCK | retry-phrase ]
      *>     [ WITH LOCK | WITH NO LOCK ]
      *>     [ KEY IS { data-name-1 | record-key-name-1 } ]      (Format 2 only)
      *> 5.2.1: "The words, phrases, clauses, punctuation, and operands in each general
      *> format shall be written in the compilation group in the sequence given in the
      *> general format", so this order is the CONFORMING one. Every READ below was a
      *> syntax error until PB331: the grammar had the KEY slot BEFORE the lock phrases.
      *> 5.2.6.1 makes a series of brackets "a unique combination of possibilities", so
      *> S1 legally selects from BOTH brackets at once; 5.2.6.2 makes each bracket at
      *> most one alternative (negative/pb331-read-two-from-one-lock-bracket).
      *> 5.2.3: ON is NOT underlined in ADVANCING ON LOCK on page 722, so S2's spelling
      *> without it is conforming too.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb331lkb-ix.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IX-KEY
               LOCK MODE IS MANUAL
               FILE STATUS IS IX-ST.
           SELECT SQF ASSIGN TO "pb331lkb-sq.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               LOCK MODE IS MANUAL
               FILE STATUS IS SQ-ST.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC X(4).
          05 IX-DATA PIC X(5).
       FD SQF.
       01 SQ-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 IX-ST PIC XX.
       01 SQ-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF.
           MOVE "K001" TO IX-KEY. MOVE "ALPHA" TO IX-DATA. WRITE IX-REC.
           MOVE "K002" TO IX-KEY. MOVE "BRAVO" TO IX-DATA. WRITE IX-REC.
           CLOSE IXF.
           OPEN I-O IXF.
      *> R1 - the retention bracket THEN the KEY phrase (the printed order).
           MOVE "K001" TO IX-KEY.
           READ IXF RECORD WITH NO LOCK KEY IS IX-KEY
               INVALID KEY DISPLAY "R1=INVALID|" IX-ST
               NOT INVALID KEY DISPLAY "R1=" IX-DATA "|" IX-ST
           END-READ.
      *> R2 - the contention bracket (IGNORING LOCK) THEN the KEY phrase.
           MOVE "K002" TO IX-KEY.
           READ IXF RECORD IGNORING LOCK KEY IS IX-KEY
               INVALID KEY DISPLAY "R2=INVALID|" IX-ST
               NOT INVALID KEY DISPLAY "R2=" IX-DATA "|" IX-ST
           END-READ.
      *> R3 - the retry-phrase alternative of the SAME bracket, THEN the KEY phrase,
      *> on a key that is not in the file: 9.1.13.5 makes that the invalid key
      *> condition with I-O status '23'.
           MOVE "K009" TO IX-KEY.
           READ IXF RECORD RETRY 3 TIMES KEY IS IX-KEY
               INVALID KEY DISPLAY "R3=INVALID|" IX-ST
               NOT INVALID KEY DISPLAY "R3=" IX-DATA "|" IX-ST
           END-READ.
           CLOSE IXF.
           OPEN OUTPUT SQF.
           MOVE "AAAA" TO SQ-REC. WRITE SQ-REC.
           MOVE "BBBB" TO SQ-REC. WRITE SQ-REC.
           MOVE "CCCC" TO SQ-REC. WRITE SQ-REC.
           CLOSE SQF.
           OPEN INPUT SQF.
      *> S1 - ONE alternative from EACH bracket. 14.9.30.3 SR3 forbids IGNORING LOCK
      *> with "the LOCK phrase"; 14.9.30.4 GR11 b)/d) name "the NO LOCK phrase" and
      *> "the LOCK phrase" as DIFFERENT phrases, so this pair is legal.
           READ SQF NEXT RECORD IGNORING LOCK WITH NO LOCK
               AT END DISPLAY "S1=EOF|" SQ-ST
               NOT AT END DISPLAY "S1=" SQ-REC "|" SQ-ST
           END-READ.
      *> S2 - ADVANCING LOCK, the optional word ON omitted (5.2.3).
           READ SQF NEXT RECORD ADVANCING LOCK
               AT END DISPLAY "S2=EOF|" SQ-ST
               NOT AT END DISPLAY "S2=" SQ-REC "|" SQ-ST
           END-READ.
           CLOSE SQF.
           STOP RUN.
