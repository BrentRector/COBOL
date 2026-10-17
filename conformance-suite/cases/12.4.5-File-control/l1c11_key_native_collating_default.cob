      *>--- conformance-case
      *> id: l1c11_key_native_collating_default
      *> rules: [GR-12.4.5.3-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.3 GR6 a) — no COLLATING SEQUENCE clause: record
      *>   keys (prime and alternate) use the native alphanumeric order
      *> Rule: "If no COLLATING SEQUENCE clause is specified: a) the
      *>   collating sequence for alphanumeric record keys, both
      *>   primary and alternate, is the native alphanumeric collating
      *>   sequence"
      *>   cite.py --check 12.4.5.3 "the collating sequence for
      *>   alphanumeric record keys, both primary and alternate, is
      *>   the native alphanumeric collating sequence"
      *>   -> OK  §12.4.5.3 6) a)  (General rules)
      *>   cite.py --check 9.1.8.2 "the order of sequential access when
      *>   NEXT is specified or implied on a READ statement is
      *>   ascending based on the value of the key of reference
      *>   according to the collating sequence of the physical file"
      *>   -> OK  §9.1.8.2   (Sequential access mode)
      *> The PROGRAM COLLATING SEQUENCE AL-R puts "a" then "Z" before
      *> every other character:
      *>   cite.py --check 12.3.7.4 "Any characters of the native
      *>   collating sequence that are not specified in the literal
      *>   phrase shall assume a position in the collating sequence
      *>   that is greater than that of the highest character
      *>   specified in this literal phrase"
      *>   -> OK  §12.3.7.4 7) 2.  (General rules)
      *>   cite.py --check 12.3.6.4 "the initial alphanumeric program
      *>   collating sequence is the collating sequence associated with
      *>   alphabet-name-1" -> OK  §12.3.6.4 9)  (General rules)
      *> It governs comparisons but is NOT a COLLATING SEQUENCE clause
      *> of the file control entry, so GR6 a) still gives the keys the
      *> native order: UTF-16 code-unit order (docs/CONFORMANCE.md
      *> DOC-A.1-8), where "1" < "B" < "Z" < "a".
      *> Records (prime, alternate), written in this order:
      *>   (ZZ, Z1) (aa, 11) (BB, a1) (11, B1)
      *> Derivation:
      *>   PCS aa<11=Y   the relation uses AL-R ("a" ranks first), so
      *>                 the program sequence really is in effect.
      *>   PRIME 11 BB ZZ aa  READ NEXT from OPEN by the prime key in
      *>                 native order ("1"<"B"<"Z"<"a"); under AL-R it
      *>                 would be aa ZZ 11 BB.
      *>   ALT aa 11 ZZ BB    START on the alternate key >= SPACES, then
      *>                 READ NEXT: alternate values 11 < B1 < Z1 < a1
      *>                 natively give primes aa 11 ZZ BB (under AL-R:
      *>                 a1 Z1 11 B1 -> BB ZZ aa 11).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11E.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX
           PROGRAM COLLATING SEQUENCE IS AL-R.
       SPECIAL-NAMES.
           ALPHABET AL-R IS "a" "Z".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-IX ASSIGN TO "L1C11E.IDX"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-ALT
               FILE STATUS IS IX-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-IX.
       01 IX-REC.
          05 IX-KEY PIC XX.
          05 IX-ALT PIC XX.
       WORKING-STORAGE SECTION.
       01 IX-ST  PIC XX.
       01 W-LINE PIC X(20).
       01 W-PTR  PIC 99.
       01 W-EOF  PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           IF "aa" < "11"
               DISPLAY "PCS aa<11=Y"
           ELSE
               DISPLAY "PCS aa<11=N"
           END-IF.
           OPEN OUTPUT F-IX.
           MOVE "ZZZ1" TO IX-REC. WRITE IX-REC.
           MOVE "aa11" TO IX-REC. WRITE IX-REC.
           MOVE "BBa1" TO IX-REC. WRITE IX-REC.
           MOVE "11B1" TO IX-REC. WRITE IX-REC.
           CLOSE F-IX.
           OPEN INPUT F-IX.
           MOVE "PRIME" TO W-LINE.
           MOVE 6 TO W-PTR.
           PERFORM WALK.
           DISPLAY W-LINE.
           MOVE SPACES TO IX-ALT.
           START F-IX KEY IS NOT LESS THAN IX-ALT
               INVALID KEY DISPLAY "START-FAILED " IX-ST
           END-START.
           MOVE "ALT" TO W-LINE.
           MOVE 4 TO W-PTR.
           PERFORM WALK.
           DISPLAY W-LINE.
           CLOSE F-IX.
           STOP RUN.
       WALK.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               READ F-IX NEXT RECORD
                   AT END MOVE "Y" TO W-EOF
                   NOT AT END
                       STRING " " IX-KEY DELIMITED BY SIZE
                           INTO W-LINE WITH POINTER W-PTR
                       END-STRING
               END-READ
           END-PERFORM.
