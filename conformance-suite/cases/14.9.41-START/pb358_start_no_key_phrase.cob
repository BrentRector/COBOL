      *>--- conformance-case
      *> id: pb358_start_no_key_phrase
      *> rules: [GR-14.9.41.4-15, GR-14.9.41.4-16, GR-14.9.41.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 §14.9.41.4 GR15 — "If the KEY phrase is not
      *> specified, the behavior is the same as if KEY IS EQUAL TO
      *> data-name-1 or record-key-name-1 had been specified, with
      *> data-name-1 or record-key-name-1 being the prime record key
      *> for the file." (cite.py --check 14.9.41.4 → OK, §14.9.41.4 15)
      *>
      *> THE RULE NAMES TWO SPELLINGS OF THE PRIME KEY AND THIS
      *> IMPLEMENTATION PROVIDES ONE. record-key-name-1 is declared by
      *> the SOURCE phrase of the RECORD KEY clause (§12.4.5.12.2),
      *> which Annex A.3 item 40 makes processor-dependent and which
      *> docs/CONFORMANCE.md §2 row 40 declines — refused by name as
      *> COBOLNET1954, witnessed by conformance:negative/pb358-*.
      *> So this golden covers the data-name-1 spelling, which is the
      *> whole of GR15 that a conforming program can reach here.
      *> kb/Work PB358.
      *>
      *> ⛔ THE DISCRIMINATOR IS L2, NOT L1. "EQUAL TO" is a stronger
      *> claim than "positions somewhere sensible": an implementation
      *> that implied ">=" for the omitted KEY phrase would pass L1
      *> unchanged, because the record whose key EQUALS the operand is
      *> also the first record whose key is >= it. L2 gives the prime
      *> key a value BETWEEN two records ("AB99", strictly after AB02
      *> and strictly before CD03, §8.8.4.2.7 alphanumeric comparison
      *> over the native sequence). Under the implied EQUAL no record
      *> satisfies the comparison, so §14.9.41.4 GR17 e) 2 — "If the
      *> comparison is not satisfied by any record in the file, the
      *> invalid key condition exists and the execution of the START
      *> statement is unsuccessful" — applies and the INVALID KEY
      *> imperative runs. Under an implied ">=" the START would
      *> SUCCEED and position at CD03. The two readings differ here
      *> and nowhere in L1.
      *> THE STATUS VALUE IS '23', derived not assumed: §9.1.13.5
      *> lists the whole invalid-key family, and '21' (an indexed
      *> WRITE/REWRITE sequence error), '22' (a duplicate key created
      *> by a WRITE or REWRITE) and '24' (a WRITE outside the file's
      *> boundaries) cannot arise from a START, so '23' is the only
      *> applicable value and §9.1.13.1's "if more than one value
      *> applies" tie-break is never reached.
      *>
      *> L3 IS THE COMPLEMENT: the SAME file and the SAME operand
      *> value under an EXPLICIT `KEY IS >=` succeeds and positions at
      *> CD03. Without it, L2's '23' could be read as the file, the
      *> operand or the START verb being broken rather than as the
      *> implied relation being EQUAL.
      *>
      *> L1N ALSO PINS §14.9.41.4 GR16's second sentence — "If the
      *> execution of the START statement is successful, this key of
      *> reference is used for subsequent sequential READ statements
      *> referencing file-name-1" — for the OMITTED-KEY-phrase case:
      *> GR15 makes the prime record key the key of reference, so the
      *> READ NEXT after the one that delivered AB02 walks in PRIME
      *> order and delivers CD03. The records are written AB01, CD03,
      *> AB02 — deliberately NOT in key order — so a walk that merely
      *> followed insertion order would answer AB02 for L1K and AB01
      *> for L1N.
      *>
      *> EDITION: --std 85. GR15 and GR16 carry no edition marker and
      *> Annex E lists no §14.9.41 change, so the data-name-1 spelling
      *> is live at 85/2002/2014/2023; the oldest edition is where a
      *> mis-gated construct would show first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB358STK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb358stk.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS ST-IX.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY PIC X(4).
          05 IX-VAL PIC X(2).
       WORKING-STORAGE SECTION.
       01 ST-IX PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF
           MOVE "AB01" TO IX-KEY
           MOVE "V1" TO IX-VAL
           WRITE IX-REC
           MOVE "CD03" TO IX-KEY
           MOVE "V3" TO IX-VAL
           WRITE IX-REC
           MOVE "AB02" TO IX-KEY
           MOVE "V2" TO IX-VAL
           WRITE IX-REC
           CLOSE IXF
      *> ---- L1: the omitted KEY phrase implies EQUAL TO the prime key
           OPEN INPUT IXF
           MOVE "AB02" TO IX-KEY
           START IXF
               INVALID KEY DISPLAY "L1INV=YES"
               NOT INVALID KEY DISPLAY "L1INV=NO"
           END-START
           DISPLAY "L1S=" ST-IX
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "L1K=" IX-KEY
           DISPLAY "L1V=" IX-VAL
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "L1N=" IX-KEY
      *> ---- L2: EQUAL, not >= — a value between two records fails
           MOVE "AB99" TO IX-KEY
           START IXF
               INVALID KEY DISPLAY "L2INV=YES"
               NOT INVALID KEY DISPLAY "L2INV=NO"
           END-START
           DISPLAY "L2S=" ST-IX
      *> ---- L3: the complement — the same value under an explicit >=
           MOVE "AB99" TO IX-KEY
           START IXF KEY IS >= IX-KEY
               INVALID KEY DISPLAY "L3INV=YES"
               NOT INVALID KEY DISPLAY "L3INV=NO"
           END-START
           DISPLAY "L3S=" ST-IX
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "L3K=" IX-KEY
           CLOSE IXF
           STOP RUN.
