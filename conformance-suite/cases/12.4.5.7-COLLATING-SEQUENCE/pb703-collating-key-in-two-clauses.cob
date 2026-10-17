*>--- conformance-case
*> id: pb703-collating-key-in-two-clauses
*> rules: [SR-12.4.5.7.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §12.4.5.7.3 SR8 - "Neither data-name-1 nor record-key-name-1 shall be
      *> specified in more than one COLLATING SEQUENCE clause." IX-KEY is named by TWO
      *> key-level (Format-2) COLLATING SEQUENCE clauses of ONE file control entry, which
      *> is exactly "more than one COLLATING SEQUENCE clause" - the violation.
      *>
      *> This case is the DISCRIMINATOR for kb/Work PB703: the fix relaxed the CLAUSE
      *> boundary, not the rule. Its sibling positive golden
      *> conformance-suite/cases/12.4.5.7-COLLATING-SEQUENCE/pb703_collating_key_named_twice.cob names IX-KEY TWICE
      *> INSIDE ONE clause and must COMPILE, because §12.4.5.7.2's Format-2 figure prints
      *> the ellipsis immediately right of the closing brace of
      *> { data-name-1 | record-key-name-1 } and §5.2.7 makes the brace group the repeated
      *> portion - so one clause may list a name more than once, and one clause is never
      *> "more than one". Delete the boundary and this program compiles; delete the rule
      *> and this program compiles. Both directions are pinned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB703TWO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "ZYXWVUTSRQPONMLKJIHGFEDCBA"
           ALPHABET DREV IS "9876543210".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb703-two-clauses.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-ALT
               COLLATING SEQUENCE OF IX-KEY IS REV
               COLLATING SEQUENCE OF IX-ALT IX-KEY IS DREV.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC X(1).
          05 IX-ALT  PIC X(1).
          05 IX-DATA PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO WS-N.
           STOP RUN.
