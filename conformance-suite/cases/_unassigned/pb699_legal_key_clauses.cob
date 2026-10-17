      *>--- conformance-case
      *> id: pb699_legal_key_clauses
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 12.4.5 - the LEGAL twin of the pb699-* negatives, and the over-rejection
      *> guard on the entry-time key screen (kb/Work PB699). Every shape the screen could get wrong
      *> is here and must BIND AND RUN:
      *>   - a prime RECORD KEY that is a GROUP item (12.4.5.12.3 SR2 admits an alphanumeric group;
      *>     13.18.29.4 GR3 makes a group with no GROUP-USAGE clause an alphanumeric group item),
      *>   - an ALTERNATE RECORD KEY WITH DUPLICATES inside the same record,
      *>   - a QUALIFIED key reference (IX-K IN IX-REC), which resolves through the record and so
      *>     satisfies "within a record description entry associated with the file-name",
      *>   - a relative file whose RELATIVE KEY is an unsigned integer in WORKING-STORAGE, i.e.
      *>     OUTSIDE the record (12.4.5.13.3 SR2 and SR3 both satisfied), under ACCESS DYNAMIC so
      *>     12.4.5.2 SR10's requirement is met by writing the clause.
      *> Expected output is derived from the file semantics, not observed: the indexed walk prints the
      *> three records in PRIME KEY order (12.4.5.12.4 GR1 - the prime key is unique and READ NEXT
      *> follows the key sequence: A1/A2/A3), and the relative reads name records 2 then 1 by
      *> relative record number (12.4.5.13.4 GR1/GR2), printing their contents in that order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P699LEGALKEYS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb699-legal-keys-ix.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-K IN IX-REC
               ALTERNATE RECORD KEY IS IX-ALT WITH DUPLICATES.
           SELECT RLF ASSIGN TO "pb699-legal-keys-rl.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WS-RK.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-K.
             10 IX-K1 PIC X.
             10 IX-K2 PIC X.
          05 IX-ALT PIC X(2).
          05 IX-DATA PIC X(4).
       FD RLF.
       01 RL-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-RK  PIC 9(4).
       01 WS-EOF PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF.
           MOVE "A1" TO IX-K. MOVE "ZZ" TO IX-ALT. MOVE "ONE " TO IX-DATA.
           WRITE IX-REC.
           MOVE "A2" TO IX-K. MOVE "ZZ" TO IX-ALT. MOVE "TWO " TO IX-DATA.
           WRITE IX-REC.
           MOVE "A3" TO IX-K. MOVE "YY" TO IX-ALT. MOVE "TRI " TO IX-DATA.
           WRITE IX-REC.
           CLOSE IXF.
           OPEN INPUT IXF.
           DISPLAY "PRIME WALK:".
           PERFORM UNTIL WS-EOF = 1
               READ IXF NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "  " IX-K " " IX-ALT " " IX-DATA
               END-READ
           END-PERFORM.
           CLOSE IXF.
           OPEN OUTPUT RLF.
           MOVE 1 TO WS-RK. MOVE "R-01" TO RL-REC. WRITE RL-REC.
           MOVE 2 TO WS-RK. MOVE "R-02" TO RL-REC. WRITE RL-REC.
           CLOSE RLF.
           OPEN INPUT RLF.
           DISPLAY "RELATIVE READS:".
           MOVE 2 TO WS-RK.
           READ RLF INVALID KEY CONTINUE
               NOT INVALID KEY DISPLAY "  " WS-RK " " RL-REC
           END-READ.
           MOVE 1 TO WS-RK.
           READ RLF INVALID KEY CONTINUE
               NOT INVALID KEY DISPLAY "  " WS-RK " " RL-REC
           END-READ.
           CLOSE RLF.
           STOP RUN.
