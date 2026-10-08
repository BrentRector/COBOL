      *> kb/Work PB2650 -- FROM literal-1 in the FROM phrase of WRITE,
      *> REWRITE and RELEASE, at its introducing edition (COBOL-2002).
      *> The general formats print one phrase for all three statements,
      *> FROM { identifier-1 | literal-1 } (ISO 1989:2023 14.9.51.2,
      *> 14.9.35.2, 14.9.32.2), and each makes it the implicit
      *> "MOVE literal-1 TO record-name-1" followed by the same statement
      *> without FROM: WRITE 14.9.51.4 GR5 a), REWRITE 14.9.35.4 GR7 a),
      *> RELEASE 14.9.32.4 GR4 a). Its negatives are the three
      *> pb2650-*-from-literal-85 cases: X3.23-1985 admits only
      *> identifier-1 after FROM.
      *> DERIVATION (every record PIC X(5); the MOVE to an alphanumeric
      *> receiver aligns left with space fill, 14.6.8.5; a figurative
      *> constant is repeated to the receiver's size, 8.3.3.6.4 GR2):
      *>   WRITE FROM "HELLO", FROM "AB", FROM ALL "XY" writes the
      *>   records HELLO, "AB   ", XYXYX.
      *>   REWRITE FROM "WORLD" replaces record 1; REWRITE FROM SPACE
      *>   replaces record 2 (SPACE is one alphanumeric space,
      *>   14.9.35.4 GR8, space-filled by the MOVE) - read back:
      *>     F=[WORLD]  F=[     ]  F=[XYXYX]
      *>   RELEASE FROM "BRAVO", FROM "AL", FROM ALL "C" releases
      *>   BRAVO, "AL   ", CCCCC; returned in ASCENDING key order
      *>   (A < B < C in every collating sequence):
      *>     S=[AL   ]  S=[BRAVO]  S=[CCCCC]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2650P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb2650p.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT S-F ASSIGN TO "pb2650p.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(5).
       SD  S-F.
       01  S-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  W-REC PIC X(5).
       01  W-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           WRITE F-REC FROM "HELLO".
           WRITE F-REC FROM "AB".
           WRITE F-REC FROM ALL "XY".
           CLOSE F.
           OPEN I-O F.
           READ F.
           REWRITE F-REC FROM "WORLD".
           READ F.
           REWRITE F-REC FROM SPACE.
           CLOSE F.
           OPEN INPUT F.
           PERFORM 3 TIMES
               READ F
               DISPLAY "F=[" F-REC "]"
           END-PERFORM.
           CLOSE F.
           SORT S-F ON ASCENDING KEY S-REC
               INPUT PROCEDURE IS IP
               OUTPUT PROCEDURE IS OP.
           STOP RUN.
       IP.
           RELEASE S-REC FROM "BRAVO".
           RELEASE S-REC FROM "AL".
           RELEASE S-REC FROM ALL "C".
       OP.
           PERFORM UNTIL W-EOF = "Y"
               RETURN S-F INTO W-REC
                   AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY "S=[" W-REC "]"
               END-RETURN
           END-PERFORM.
