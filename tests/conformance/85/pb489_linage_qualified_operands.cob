      *> ISO §13.18.34.2 — the LINAGE clause's general format prints
      *> data-name-1 … data-name-4, and a data-name in a general format
      *> is a QUALIFIED-DATA-NAME (§8.4.2.2.2 Format 1):
      *>   python scripts/spec/cite.py --check 8.4.2.2.3 "A name may be
      *>   qualified even though it does not need qualification"
      *>   -> OK  §8.4.2.2.3 2)  (Syntax rules)
      *>   python scripts/spec/cite.py --check 8.4.2.2.3 "For each non
      *>   unique user-defined name that is explicitly referenced,
      *>   uniqueness shall be established through a sequence of
      *>   qualifiers that precludes any ambiguity of reference."
      *>   -> OK  §8.4.2.2.3 1)  (Syntax rules)
      *> so `LINAGE IS PG-SZ OF R-GRP LINES` is legal — and, in THIS
      *> program, where W-GRP and R-GRP each declare PG-SZ, FT-AT, TP-MG
      *> and BT-MG, the qualified form is the ONLY legal way to name any
      *> of the eight items.
      *>
      *> WHAT THE VALUE IS. §13.18.34.4 GR6:
      *>   python scripts/spec/cite.py --check 13.18.34.4 "If a literal
      *>   is specified, the value is always that literal."
      *>   -> OK  §13.18.34.4 6) a)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "If a data-name
      *>   is specified, the value is the content of the data item
      *>   referenced by the associated data-name"
      *>   -> OK  §13.18.34.4 6) b)
      *> GR6 a) and GR6 b) are two routes to ONE number, so a clause
      *> written with four literals and a clause written with four
      *> qualified data-names HOLDING THOSE LITERALS describe the SAME
      *> logical page. That equivalence is the whole assertion here, and
      *> it is why the fixture writes two files and compares them rather
      *> than restating a page-geometry derivation: PRTL carries
      *> `LINAGE 2 FOOTING 2 TOP 3 BOTTOM 2` as literals, PRTQ carries the
      *> same four values as `… OF R-GRP`, and every observable of the
      *> two files must agree, value for value.
      *>
      *> ⛔ THE DISTINGUISHER IS DECLARATION ORDER. W-GRP is declared
      *> FIRST and holds 5 / 4 / 1 / 4 — different in every operand. A
      *> compiler that keeps only the base word of the operand and then
      *> resolves it by first match binds W-GRP's items, and this fixture
      *> was written because that is exactly what COBOL.NET did: the
      *> IN/OF qualifier was accepted by the grammar and discarded by the
      *> binder, so the whole page model was built on another data item's
      *> value with no diagnostic at any stage (kb/Work PB489, measured).
      *> Under that defect PRTQ has page size 5, footing 4, top margin 1
      *> and bottom margin 4, so BOTH observables below diverge: the
      *> end-of-page branches change (a 5-line body is not reached by
      *> three single-line advances) and so do the line numbers.
      *>
      *> OBSERVABLE 1 — THE END-OF-PAGE BRANCH PER WRITE, which is what
      *> makes the FOOTING operand visible at all (§13.18.34.4 GR1: the
      *> footing start is the one phrase that is NOT part of the logical
      *> page size, so it can only be seen through the end-of-page
      *> condition). §13.18.34.4 GR7 d) sets LINAGE-COUNTER to one at
      *> OPEN OUTPUT and GR7 c) 2 adds each advance, so on a 2-line body
      *> with the footing start at 2 every one of the three writes leaves
      *> the counter in the footing area or past the page size — the
      *> §14.9.51.4 GR26 b) and GR26 a) arms respectively, both of which
      *> raise the end-of-page condition. All six branches are therefore
      *> EOP, on both files.
      *>
      *> OBSERVABLE 2 — THE LINES ON THE MEDIUM, which is what makes the
      *> TOP and BOTTOM operands visible (a margin has no effect on the
      *> counter). The derivation of the three line numbers for exactly
      *> this geometry is the one already recorded in
      *> tests/conformance/85/pb523_linage_margins_on_the_medium_85.cob —
      *> LINAGE 2 / TOP 3 / BOTTOM 2, three WRITEs AFTER ADVANCING 1 LINE:
      *> lines 1-4 blank, AAAA on line 5, lines 6-10 blank, BBBB on line
      *> 11, CCCC on line 12. The reader counts line feeds, because the
      *> line end of a LINAGE file is the host newline (CR LF or LF:
      *> docs/CONFORMANCE.md A.1 item 146 (c'), kb/Work PB1664), so a
      *> byte offset would differ by host and a line number does not.
      *> The FOOTING phrase does not move them: GR1 — "The logical page
      *> size is the sum of the values referenced by each phrase except
      *> the FOOTING phrase."
      *>   python scripts/spec/cite.py --check 13.18.34.4 "The logical
      *>   page size is the sum of the values referenced by each phrase
      *>   except the FOOTING phrase."   -> OK  §13.18.34.4 1)
      *>
      *> EDITION. The LINAGE clause, its four operands and §8.4.2.2's
      *> qualification rules are present in all four supported editions,
      *> so there is ONE copy of this fixture and it sits at the earliest
      *> of them; the behaviour does not differ by edition. The reader is
      *> a plain record-sequential FD over a one-character record, not
      *> ORGANIZATION LINE SEQUENTIAL, because that phrase is COBOL-2023
      *> (§12.4.5.10.3 GR2) and would not compile here.
      *>
      *> THE OPTIONAL WORDS, IN THE SAME COMPARISON. §13.18.34.2's figure
      *> underlines LINAGE, FOOTING, TOP and BOTTOM and NOTHING else, so
      *> IS, LINES, WITH, AT and the two `LINES AT` are optional words:
      *>   python scripts/spec/cite.py --check 8.3.2.4.3 "uppercase words
      *>   that are not underlined are called optional words and may be
      *>   specified at the user's option with no effect on the semantics
      *>   of the format"   -> OK  §8.3.2.4.3 (Optional words)
      *> PRTL therefore
      *> writes the MINIMUM spelling — `LINAGE 2 FOOTING 2 TOP 3 BOTTOM 2`
      *> — while PRTQ writes every optional word, so the two files also
      *> assert that the two spellings are the SAME clause. A grammar that
      *> required any of those words would not compile this fixture at all.
      *>
      *> ⛔ WHY EACH WRITE IS PRECEDED BY ITS OWN MOVE — §14.9.51.4 GR4
      *> makes the released record no longer available in the record area
      *> unless the file is named in a SAME RECORD AREA clause, and
      *> neither file is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB489Q.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A ARE 11.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTL ASSIGN TO "pb489q-l.prt".
           SELECT PRTQ ASSIGN TO "pb489q-q.prt".
           SELECT RDF ASSIGN TO "pb489q-l.prt".
           SELECT RDG ASSIGN TO "pb489q-q.prt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTL LINAGE 2 FOOTING 2 TOP 3 BOTTOM 2.
       01 L-REC PIC X(4).
       FD PRTQ LINAGE IS PG-SZ OF R-GRP LINES
               WITH FOOTING AT FT-AT OF R-GRP
               LINES AT TOP TP-MG OF R-GRP
               LINES AT BOTTOM BT-MG OF R-GRP.
       01 Q-REC PIC X(4).
       FD RDF.
       01 F-CHAR PIC X.
       FD RDG.
       01 G-CHAR PIC X.
       WORKING-STORAGE SECTION.
       01 W-GRP.
          05 PG-SZ PIC 99 VALUE 5.
          05 FT-AT PIC 99 VALUE 4.
          05 TP-MG PIC 99 VALUE 1.
          05 BT-MG PIC 99 VALUE 4.
       01 R-GRP.
          05 PG-SZ PIC 99 VALUE 2.
          05 FT-AT PIC 99 VALUE 2.
          05 TP-MG PIC 99 VALUE 3.
          05 BT-MG PIC 99 VALUE 2.
       01 POSN PIC 9(4) VALUE 0.
       01 EOF-SW PIC 9 VALUE 0.
       01 AT-A PIC 9(4) VALUE 0.
       01 AT-B PIC 9(4) VALUE 0.
       01 AT-C PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRTL.
           MOVE "AAAA" TO L-REC.
           WRITE L-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "L1 EOP"
               NOT AT END-OF-PAGE DISPLAY "L1 NOEOP"
           END-WRITE.
           MOVE "BBBB" TO L-REC.
           WRITE L-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "L2 EOP"
               NOT AT END-OF-PAGE DISPLAY "L2 NOEOP"
           END-WRITE.
           MOVE "CCCC" TO L-REC.
           WRITE L-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "L3 EOP"
               NOT AT END-OF-PAGE DISPLAY "L3 NOEOP"
           END-WRITE.
           CLOSE PRTL.
           OPEN OUTPUT PRTQ.
           MOVE "AAAA" TO Q-REC.
           WRITE Q-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "Q1 EOP"
               NOT AT END-OF-PAGE DISPLAY "Q1 NOEOP"
           END-WRITE.
           MOVE "BBBB" TO Q-REC.
           WRITE Q-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "Q2 EOP"
               NOT AT END-OF-PAGE DISPLAY "Q2 NOEOP"
           END-WRITE.
           MOVE "CCCC" TO Q-REC.
           WRITE Q-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "Q3 EOP"
               NOT AT END-OF-PAGE DISPLAY "Q3 NOEOP"
           END-WRITE.
           CLOSE PRTQ.
           MOVE 0 TO POSN. MOVE 0 TO EOF-SW.
           MOVE 0 TO AT-A. MOVE 0 TO AT-B. MOVE 0 TO AT-C.
           OPEN INPUT RDF.
           PERFORM UNTIL EOF-SW = 1
               READ RDF
                   AT END MOVE 1 TO EOF-SW
                   NOT AT END
                       IF F-CHAR = SYM-X0A
                           ADD 1 TO POSN
                       END-IF
                       IF F-CHAR = "A" AND AT-A = 0
                           COMPUTE AT-A = POSN + 1
                       END-IF
                       IF F-CHAR = "B" AND AT-B = 0
                           COMPUTE AT-B = POSN + 1
                       END-IF
                       IF F-CHAR = "C" AND AT-C = 0
                           COMPUTE AT-C = POSN + 1
                       END-IF
               END-READ
           END-PERFORM.
           CLOSE RDF.
           DISPLAY "L A=" AT-A " B=" AT-B " C=" AT-C.
           MOVE 0 TO POSN. MOVE 0 TO EOF-SW.
           MOVE 0 TO AT-A. MOVE 0 TO AT-B. MOVE 0 TO AT-C.
           OPEN INPUT RDG.
           PERFORM UNTIL EOF-SW = 1
               READ RDG
                   AT END MOVE 1 TO EOF-SW
                   NOT AT END
                       IF G-CHAR = SYM-X0A
                           ADD 1 TO POSN
                       END-IF
                       IF G-CHAR = "A" AND AT-A = 0
                           COMPUTE AT-A = POSN + 1
                       END-IF
                       IF G-CHAR = "B" AND AT-B = 0
                           COMPUTE AT-B = POSN + 1
                       END-IF
                       IF G-CHAR = "C" AND AT-C = 0
                           COMPUTE AT-C = POSN + 1
                       END-IF
               END-READ
           END-PERFORM.
           CLOSE RDG.
           DISPLAY "Q A=" AT-A " B=" AT-B " C=" AT-C.
           STOP RUN.
