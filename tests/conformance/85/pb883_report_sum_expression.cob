      *> ⛔ arithmetic-expression-1 IS A SUM ADDEND FORM, AND THE SUM CLAUSE HAS A ROUNDED PHRASE (kb/Work PB883).
      *> ISO 13.18.54.2's general format (PDF p487 rendered) writes
      *>     { SUM OF { data-name-1 | identifier-1 | arithmetic-expression-1 } … [ UPON { data-name-2 } … ] } …
      *>     [ RESET ON { data-name-3 | FINAL } ] [ rounded-phrase ]
      *> and 13.18.54.3 SR1 confirms it: "Each data-name-1, identifier-1 or arithmetic-expression-1 is an addend."
      *> The grammar had `sumOperand : dataReference (OF reportName)?` — no expression form at all — so `SUM WS-K + 1`
      *> died as a raw parse error at every edition.  SR6 governs what such an expression may contain: "If the
      *> addend is arithmetic-expression-1, any identifiers it contains may reference entries in any section of the
      *> data division other than the report section."  13.18.54.4 GR3 gives it its accumulation: "The adding is
      *> consistent with the general rules of the ADD statement with the ON SIZE ERROR phrase or, in the case of an
      *> arithmetic expression, the COMPUTE statement with the ON SIZE ERROR phrase."
      *>
      *> ⚠ DETERMINATION — WHAT THE SUM CLAUSE'S ROUNDED PHRASE ROUNDS.  13.18.54.4 GR4 says "the content of the
      *> sum counter is computed according to the general rules for the COMPUTE statement with the ROUNDED
      *> phrase".  COBOL.NET reads that as governing the ACCUMULATION of each addend into the counter (GR3's
      *> implicit ADD/COMPUTE).  The rejected reading is that it governs GR4's subsequent MOVE of the counter to
      *> the printable item: GR1 derives the counter's digits, integral AND fractional, from that item's own
      *> PICTURE, so that transfer is always scale-identical and the phrase could never change any result — a
      *> reading that makes a normative phrase dead.  13.18.54.3 SR3 ("The ROUNDED phrase may be specified in the
      *> SUM clause only if the COLUMN clause is specified for the subject of the entry") is honoured either way.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  WS-K = 10, WS-F = 1.55, WS-M = 3.  One GENERATE, so GR7 c) 1)
      *> accumulates each addend exactly once into a counter GR2 zeroed at the INITIATE.
      *>   COLUMN 1  PIC 9999 SUM WS-K + 1            — the expression addend: 10 + 1            => 0011
      *>   COLUMN 7  PIC 9999 SUM WS-K * WS-M WS-K    — GR9: "If the SUM clause specifies more than
      *>                                                one addend, the result is the same as when all
      *>                                                the addends were summed separately … and the
      *>                                                results added together": 30 + 10             => 0040
      *>   COLUMN 13 PIC 9999 SUM WS-F ROUNDED        — counter scale 0 (GR1 from PIC 9999); 1.55
      *>                                                rounded away from zero (14.7.4.3 rule 1 — no
      *>                                                DEFAULT ROUNDED MODE clause, so
      *>                                                NEAREST-AWAY-FROM-ZERO)                   => 0002
      *>   COLUMN 19 PIC 9999 SUM WS-F                — the same addend truncated (14.7.4.3 r2)   => 0001
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB883RSE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb883rse.txt".
           SELECT CHK ASSIGN TO "pb883rse.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-SE.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-K    PIC 99    VALUE 10.
       01  WS-M    PIC 99    VALUE 3.
       01  WS-F    PIC 9V99  VALUE 1.55.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-SE CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  COLUMN 1  PIC 9999 SUM WS-K + 1.
               03  COLUMN 7  PIC 9999 SUM WS-K * WS-M WS-K.
               03  COLUMN 13 PIC 9999 SUM WS-F ROUNDED.
               03  COLUMN 19 PIC 9999 SUM WS-F.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-SE.
           GENERATE DET.
           TERMINATE R-SE.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
