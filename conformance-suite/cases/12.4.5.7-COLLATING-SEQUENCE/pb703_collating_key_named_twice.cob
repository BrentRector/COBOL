*>--- conformance-case
*> id: pb703_collating_key_named_twice
*> rules: [SR-12.4.5.7.3-8, GR-12.4.5.7.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §12.4.5.7.2 Format 2 (key-level) COLLATING SEQUENCE: the general format is
      *> `COLLATING SEQUENCE OF { data-name-1 | record-key-name-1 } … IS alphabet-name-3`,
      *> and by §5.2.7 ("the ellipsis applies to the portion of the format between the
      *> determined pair of delimiters") the `…` repeats the BRACE GROUP — so ONE clause
      *> may list one or more names, the same name among them. §12.4.5.7.3 SR8 forbids a
      *> name only "in more than one COLLATING SEQUENCE clause", and one clause is never
      *> more than one, so `OF IX-KEY IX-KEY IS REV` is legal (kb/Work PB703 — it was
      *> rejected COBOLNET1582 by a duplicate screen hoisted out of the clause loop).
      *>
      *> Expected values, derived: §12.4.5.7.4 GR6 — "Alphabet-name-3 applies to record
      *> keys identified by data-name-1 or record-key-name-1" — gives IX-KEY the REV
      *> sequence and IX-ALT the DREV sequence, and §12.4.5.5.3 GR2 c) makes sequential
      *> retrieval "ascending within a given key of reference according to the collating
      *> sequence for that key".
      *>   REV  = "ZYXWVUTSRQPONMLKJIHGFEDCBA": weights Z=0 < M=13 < A=25, so the prime
      *>          key ascends Z, M, A (records written A/3, M/1, Z/2 → alts 2, 1, 3).
      *>   DREV = "9876543210": weights '3'=6 < '2'=7 < '1'=8, so the alternate key
      *>          ascends 3, 2, 1 (keys A, Z, M) and START KEY >= "3" (weight 6, the
      *>          lowest of the three) positions on the first of them.
      *> Native ordinal weights would print A, M, Z and 1, 2, 3 — the two halves order
      *> DIFFERENTLY, so neither can pass with the other's sequence applied.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB703KEY.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "ZYXWVUTSRQPONMLKJIHGFEDCBA"
           ALPHABET DREV IS "9876543210".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb703-collating-key-twice.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-ALT
               FILE STATUS IS WS-FS
               COLLATING SEQUENCE OF IX-KEY IX-KEY IS REV
               COLLATING SEQUENCE OF IX-ALT IS DREV.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC X(1).
          05 IX-ALT  PIC X(1).
          05 IX-DATA PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-EOF PIC 9 VALUE 0.
       01 WS-FS  PIC X(2) VALUE "00".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF.
           MOVE "A" TO IX-KEY. MOVE "3" TO IX-ALT. WRITE IX-REC.
           MOVE "M" TO IX-KEY. MOVE "1" TO IX-ALT. WRITE IX-REC.
           MOVE "Z" TO IX-KEY. MOVE "2" TO IX-ALT. WRITE IX-REC.
           CLOSE IXF.
           OPEN INPUT IXF.
           DISPLAY "PRIME UNDER REV:".
           PERFORM UNTIL WS-EOF = 1
               READ IXF NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "  K=" IX-KEY " A=" IX-ALT
               END-READ
           END-PERFORM.
           CLOSE IXF.
           OPEN INPUT IXF.
           MOVE "3" TO IX-ALT.
           START IXF KEY IS >= IX-ALT
               INVALID KEY DISPLAY "  START-INVALID"
               NOT INVALID KEY DISPLAY "ALT UNDER DREV:"
           END-START.
           MOVE 0 TO WS-EOF.
           PERFORM UNTIL WS-EOF = 1
               READ IXF NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "  K=" IX-KEY " A=" IX-ALT
               END-READ
           END-PERFORM.
           CLOSE IXF.
           STOP RUN.
