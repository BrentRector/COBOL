      *>--- conformance-case
      *> id: l1c11_key_native_national_default
      *> rules: [GR-12.4.5.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.3 GR6 b) — no COLLATING SEQUENCE: national keys
      *>   (prime and alternate) use the native national order
      *> Rule: "If no COLLATING SEQUENCE clause is specified: ... b) the
      *>   collating sequence for national record keys, both primary
      *>   and alternate, is the native national collating sequence."
      *>   cite.py --check 12.4.5.3 "the collating sequence for national
      *>   record keys, both primary and alternate, is the native
      *>   national collating sequence"
      *>   -> OK  §12.4.5.3 6) b)  (General rules)
      *>   cite.py --check 9.1.8.2 "the order of sequential access when
      *>   NEXT is specified or implied on a READ statement is
      *>   ascending based on the value of the key of reference
      *>   according to the collating sequence of the physical file"
      *>   -> OK  §9.1.8.2   (Sequential access mode)
      *> Native national order is UTF-16 code-unit order
      *> (docs/CONFORMANCE.md DOC-A.1-8): "B" (U+0042) < "a" (U+0061)
      *> < "f" (U+0066) < "é" (U+00E9). A locale or case-folding order
      *> would instead put a < B and é < f.
      *> Records (prime, alternate), written in this order:
      *>   (é1, a2) (f1, é2) (a1, B2) (B1, f2)
      *> Derivation (one line per record read, "<walk> <prime key>"):
      *>   PRIME B1, a1, f1, é1  READ NEXT from OPEN by the prime key.
      *>   ALT a1, é1, B1, f1    START on the alternate key >= SPACES,
      *>                         then READ NEXT: alternate values
      *>                         B2 < a2 < f2 < é2 give those primes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-IX ASSIGN TO "L1C11F.IDX"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-ALT
               FILE STATUS IS IX-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-IX.
       01 IX-REC.
          05 IX-KEY PIC N(2).
          05 IX-ALT PIC N(2).
       WORKING-STORAGE SECTION.
       01 IX-ST  PIC XX.
       01 W-TAG  PIC X(5).
       01 W-EOF  PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F-IX.
           MOVE N"é1" TO IX-KEY. MOVE N"a2" TO IX-ALT.
           WRITE IX-REC.
           MOVE N"f1" TO IX-KEY. MOVE N"é2" TO IX-ALT.
           WRITE IX-REC.
           MOVE N"a1" TO IX-KEY. MOVE N"B2" TO IX-ALT.
           WRITE IX-REC.
           MOVE N"B1" TO IX-KEY. MOVE N"f2" TO IX-ALT.
           WRITE IX-REC.
           CLOSE F-IX.
           OPEN INPUT F-IX.
           MOVE "PRIME" TO W-TAG.
           PERFORM WALK.
           MOVE SPACES TO IX-ALT.
           START F-IX KEY IS NOT LESS THAN IX-ALT
               INVALID KEY DISPLAY "START-FAILED " IX-ST
           END-START.
           MOVE "ALT" TO W-TAG.
           PERFORM WALK.
           CLOSE F-IX.
           STOP RUN.
       WALK.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               READ F-IX NEXT RECORD
                   AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY W-TAG " " IX-KEY
               END-READ
           END-PERFORM.
