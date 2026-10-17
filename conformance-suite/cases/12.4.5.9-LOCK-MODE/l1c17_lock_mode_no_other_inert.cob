      *>--- conformance-case
      *> id: l1c17_lock_mode_no_other_inert
      *> rules: [GR-12.4.5.9.4-3, GR-12.4.5.9.4-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.9.4 GR3 — NO OTHER makes LOCK MODE inert; GR8
      *> GR3: "If a physical file is open in the sharing with no other
      *>  mode, the LOCK MODE clause has no effect. Otherwise, the LOCK
      *>  MODE clause has the effects described in the General rules
      *>  that follow."
      *> GR8: "The setting of a record lock is part of the operation of
      *>  an I-O statement."
      *> cite.py --check 12.4.5.9.4 "If a physical file is open in the
      *>   sharing with no other mode, the LOCK MODE clause has no
      *>   effect." -> OK §12.4.5.9.4 3)
      *> cite.py --check 12.4.5.9.4 "The setting of a record lock is
      *>   part of the operation of an I-O statement."
      *>   -> OK §12.4.5.9.4 8)
      *> cite.py --check 12.4.5.9.4 "The implementor shall specify the
      *>   maximum number of record locks that may be held by a file
      *>   connector; that maximum shall be at least 15."
      *>   -> OK §12.4.5.9.4 7)
      *> cite.py --check 9.1.13.8 "The input-output statement is
      *>   unsuccessful because the statement requested a record lock,
      *>   but this file connector holds the maximum number of locks
      *>   allowed by this implementation." -> OK §9.1.13.8 4)
      *> cite.py --check 9.1.15 "The sharing with no other mode
      *>   specifies exclusive access to a physical file."
      *>   -> OK §9.1.15 1)
      *> Documented choice: docs/CONFORMANCE.md DOC-A.1-155, a file
      *> connector may hold at most 15 record locks; a statement that
      *> would obtain a 16th is unsuccessful with '54'.
      *>
      *> FN and FC carry the SAME LOCK MODE clause (MANUAL WITH LOCK ON
      *> MULTIPLE RECORDS) and execute the SAME 16 READ ... WITH LOCK
      *> statements on records 1..16.  Only their SHARING differs.
      *> LEG NO OTHER (FN): GR3 - the clause has no effect, so the
      *>   WITH LOCK phrases obtain no locks, the 15-lock ceiling is
      *>   never approached and all 16 READs succeed.
      *> LEG ALL OTHER (FC): the clause has effect (GR3 "Otherwise"),
      *>   GR7 multiple record locking: reads 1..15 each obtain a lock;
      *>   the 16th would exceed the connector maximum 15 and "is
      *>   unsuccessful" with '54' (§9.1.13.8 4)).
      *>   GR8: because setting the lock is PART OF the READ, the lock
      *>   exists as soon as FC's first READ completes - observer FO's
      *>   very next READ of record 1 is refused '51' (§9.1.16) - and
      *>   the failure to set the 16th lock is the READ statement's
      *>   OWN I-O status, not a separate step's.
      *>
      *> DERIVED OUTPUT:
      *>   NOOTHER-OK=16 LAST=00   GR3: inert clause, no ceiling
      *>   GR8-FIRST=00 R001       FC's first READ WITH LOCK succeeds
      *>   GR8-OTHER=51            the lock is already set (GR8)
      *>   ALLOTHER-OK=15 LAST=54  GR7 ceiling reported by the READ
      *>                           itself (GR8); re-reading record 1
      *>                           re-locks a held record, which does
      *>                           not count again (DOC-A.1-155).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C17C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "L1C17C.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT FN ASSIGN TO "L1C17C.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K-N
               FILE STATUS IS ST-N
               SHARING WITH NO OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT FC ASSIGN TO "L1C17C.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K-C
               FILE STATUS IS ST-C
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.
           SELECT FO ASSIGN TO "L1C17C.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K-O
               FILE STATUS IS ST-O
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 S-REC PIC X(4).
       FD FN.
       01 N-REC PIC X(4).
       FD FC.
       01 C-REC PIC X(4).
       FD FO.
       01 O-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST-S PIC XX.
       01 ST-N PIC XX.
       01 ST-C PIC XX.
       01 ST-O PIC XX.
       01 K-N  PIC 9(4).
       01 K-C  PIC 9(4).
       01 K-O  PIC 9(4).
       01 I    PIC 99.
       01 OK-N PIC 99 VALUE 0.
       01 OK-C PIC 99 VALUE 0.
       01 LAST-N PIC XX.
       01 LAST-C PIC XX.
       01 NUM  PIC 999.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FS.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 16
               MOVE I TO NUM
               MOVE "R" TO S-REC (1:1)
               MOVE NUM TO S-REC (2:3)
               WRITE S-REC
           END-PERFORM.
           CLOSE FS.
      *> LEG NO OTHER: the LOCK MODE clause has no effect.
           OPEN I-O FN.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 16
               MOVE I TO K-N
               READ FN WITH LOCK
               IF ST-N = "00"
                   ADD 1 TO OK-N
               END-IF
               MOVE ST-N TO LAST-N
           END-PERFORM.
           DISPLAY "NOOTHER-OK=" OK-N " LAST=" LAST-N.
           CLOSE FN.
      *> LEG ALL OTHER: the same clause has effect.
           OPEN I-O FC.
           OPEN INPUT FO.
           MOVE 1 TO K-C.
           READ FC WITH LOCK.
           DISPLAY "GR8-FIRST=" ST-C " " C-REC.
           MOVE 1 TO K-O.
           READ FO.
           DISPLAY "GR8-OTHER=" ST-O.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 16
               MOVE I TO K-C
               READ FC WITH LOCK
               IF ST-C = "00"
                   ADD 1 TO OK-C
               END-IF
               MOVE ST-C TO LAST-C
           END-PERFORM.
           DISPLAY "ALLOTHER-OK=" OK-C " LAST=" LAST-C.
           CLOSE FO.
           CLOSE FC.
           STOP RUN.
