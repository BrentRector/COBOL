*>--- conformance-case
*> id: pb704_sort_duplicates_in_order_2014
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB704 - `SORT ... WITH DUPLICATES IN ORDER` (ISO 14.9.40.2). ORDER is the phrase's own
      *> word, not a name: it rode `cobolWord`, and the 8.9 funnel - which screens every IDENTIFIER
      *> occurrence POSITION-BLIND - answered `'ORDER' is a reserved word ... and cannot be used as a
      *> user-defined word` at every edition from 2002 (8.9 reserves ORDER since 2002). The phrase is the
      *> ONLY way a program can demand 14.9.40.4 GR3's defined order for records equal on every key, so the
      *> workaround was a program whose result GR4 leaves UNDEFINED. ORDER is now a lexer token with a
      *> cobol-words.json nameSlot row (the FORMAT precedent), so the phrase parses at every edition and
      *> `01 ORDER PIC X.` still draws COBOLNET0901 at 2002+.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>   14.9.40.4 GR8 a) - ASCENDING, so the record with the LOWER key is returned first: every A
      *>                      before every B.
      *>   14.9.40.4 GR3 b) - among records whose key data items are all equal, the order of return is
      *>                      "the order in which these records are released by an input procedure".
      *>                      IN-PROC releases B1 A1 B2 A2 B3 A3, so the A group returns A1 A2 A3 and the
      *>                      B group B1 B2 B3.
      *> Hence OUT=A1 A2 A3 B1 B2 B3, then DONE. Without the DUPLICATES phrase GR4 makes that order
      *> undefined, which is exactly what this phrase buys and what the rejection took away.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB704DUP14.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S-FILE ASSIGN TO "pb704dup14.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  S-FILE.
       01  S-REC.
           05  S-KEY PIC X.
           05  S-TAG PIC X.
       WORKING-STORAGE SECTION.
       01  W-I   PIC 9 VALUE 0.
       01  W-EOF PIC X VALUE "N".
       01  W-SRC.
           05  FILLER PIC XX VALUE "B1".
           05  FILLER PIC XX VALUE "A1".
           05  FILLER PIC XX VALUE "B2".
           05  FILLER PIC XX VALUE "A2".
           05  FILLER PIC XX VALUE "B3".
           05  FILLER PIC XX VALUE "A3".
       01  W-SRC-R REDEFINES W-SRC.
           05  W-PAIR PIC XX OCCURS 6 TIMES.
       PROCEDURE DIVISION.
       MAIN-SECT SECTION.
       MAIN.
           SORT S-FILE ON ASCENDING KEY S-KEY
               WITH DUPLICATES IN ORDER
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "DONE".
           STOP RUN.
       IN-PROC SECTION.
       IN-P.
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 6
               MOVE W-PAIR (W-I) TO S-REC
               RELEASE S-REC
           END-PERFORM.
       OUT-PROC SECTION.
       OUT-P.
           PERFORM UNTIL W-EOF = "Y"
               RETURN S-FILE AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY "OUT=" S-REC
               END-RETURN
           END-PERFORM.
