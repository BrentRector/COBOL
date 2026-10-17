      *>--- conformance-case
      *> id: l1c31_scope_termination
      *> rules: [GR-14.5.3.2-2, GR-14.5.3.3-1, GR-14.5.3.3-3, GR-14.5.3.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.5.3.2 2), §14.5.3.3 1) 3) 4) — explicit and implicit
      *> scope termination of statements.
      *> cite.py --check 14.5.3.2 "any unterminated statements that
      *>   appear between that statement-name and the explicit scope
      *>   terminator" -> OK  §14.5.3.2 2)  (Explicit scope termination)
      *> cite.py --check 14.5.3.3 "the next-encountered statement-name"
      *>   -> OK  §14.5.3.3 1) a)  (cite.py labels the run-in list
      *>   "1) a)" although the quoted text is item 1) b) -- the known
      *>   list-item mislabel, PB1554; the clause and item 1) are right)
      *> cite.py --check 14.5.3.3 "for a conditional statement not
      *>   contained within another statement, by a separator period"
      *>   -> OK  §14.5.3.3 3)
      *> cite.py --check 14.5.3.3 "If all permitted occurrences of a
      *>   phrase have already been specified for a given statement"
      *>   -> OK  §14.5.3.3 4)
      *> Every IF below holds a conditional statement as statement-1 or
      *> statement-2 (§14.9.19.3 1) allows conditional statements there);
      *> no conditional is placed where a format writes
      *> imperative-statement.
      *> S1 -- §14.5.3.2 2): "An explicit scope terminator terminates
      *> the scope of: 1) the most-recently preceding unterminated
      *> statement having the statement-name ... and 2) any unterminated
      *> statements that appear between that statement-name and the
      *> explicit scope terminator." END-IF therefore also ends the
      *> unterminated ADD ... ON SIZE ERROR / SEARCH inside the IF, so
      *>    the
      *> DISPLAY after END-IF runs whatever the conditional did:
      *>  S1A N=999, ADD overflows: "S1A SE" then "S1A AFTER N=999".
      *>  S1B N=5, no size error: "S1B AFTER N=006" (were the DISPLAY
      *>      still inside ON SIZE ERROR it would not print).
      *>  S1C IF false: "S1C AFTER".
      *>  S1D SEARCH finds no "Z": AT END "S1D END", then "S1D AFTER"
      *>      (were it still inside the last WHEN it would not print).
      *> S2 -- §14.5.3.3 1): a single imperative statement not contained
      *> in another ends at the next statement-name or a period; until
      *> then its syntax continues, even across lines:
      *>  MOVE 7 TO A <newline> B -> B is a second receiver:
      *>      "S2 A=7 B=7";
      *>  DISPLAY "S2X" DISPLAY "S2Y" on one line -> the statement-name
      *>      DISPLAY ends the first: two lines "S2X", "S2Y".
      *>  (Item a), an element other than a statement-name or period
      *>  following an exhausted top-level statement, has no conforming
      *>  top-level form distinct from b)/c) in these statements.)
      *> S3 -- §14.5.3.3 3): a top-level conditional statement ends only
      *> at the separator period, so every statement up to it belongs to
      *> the conditional's phrase:
      *>  IF false DISPLAY "S3 WRONG1" DISPLAY "S3 WRONG2". -> neither
      *>      prints; the next sentence prints "S3 AFTER".
      *>  N=5: ADD 1 TO N ON SIZE ERROR DISPLAY "S3 SE1" DISPLAY
      *>      "S3 SE2". -> no size error, neither prints; "S3 N=006".
      *> S4 -- §14.5.3.3 4): "Any phrase encountered is the next phrase
      *> of the most-recently preceding unterminated statement with
      *>    which
      *> that phrase may be syntactically associated. If all permitted
      *> occurrences of a phrase have already been specified for a given
      *> statement, a subsequent occurrence of that phrase is not
      *> syntactically associated with that statement."
      *>  S4A IF F=1 IF G=1 ... ELSE ...: the one ELSE belongs to the
      *>      inner IF. F=1,G=2: "S4A Q"; F=2: nothing (association with
      *>      the outer IF would print "S4A WRONG"); then "S4A DONE".
      *>  S4B IF F=1 IF G=1 X ELSE Y ELSE Z: the inner IF's one ELSE is
      *>      used, so the second ELSE goes to the outer IF.
      *>      F=1,G=2: "S4B Y"; F=2: "S4B Z".
      *>  S4C IF F=1 ADD .. ON SIZE ERROR .. ELSE ..: ELSE is the next
      *>      phrase of the containing IF and ends the contained ADD.
      *>      F=2: "S4C ELSE"; F=1,N=5: ADD runs, "S4C N=006".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C31H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F  PIC 9 VALUE 0.
       01 G  PIC 9 VALUE 0.
       01 A  PIC 9 VALUE 0.
       01 B  PIC 9 VALUE 0.
       01 N  PIC 999 VALUE 0.
       01 TT VALUE "ABC".
          05 TE PIC X OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO F. MOVE 999 TO N.
           IF F = 1 ADD 1 TO N ON SIZE ERROR DISPLAY "S1A SE"
           END-IF DISPLAY "S1A AFTER N=" N.
           MOVE 5 TO N.
           IF F = 1 ADD 1 TO N ON SIZE ERROR DISPLAY "S1B SE"
           END-IF DISPLAY "S1B AFTER N=" N.
           MOVE 2 TO F.
           IF F = 1 ADD 1 TO N ON SIZE ERROR DISPLAY "S1C SE"
           END-IF DISPLAY "S1C AFTER".
           MOVE 1 TO F. SET IX TO 1.
           IF F = 1 SEARCH TE AT END DISPLAY "S1D END"
               WHEN TE (IX) = "Z" DISPLAY "S1D FOUND"
           END-IF DISPLAY "S1D AFTER".
           MOVE 7 TO A
               B
           DISPLAY "S2 A=" A " B=" B.
           DISPLAY "S2X" DISPLAY "S2Y".
           MOVE 2 TO F.
           IF F = 1 DISPLAY "S3 WRONG1" DISPLAY "S3 WRONG2".
           DISPLAY "S3 AFTER".
           MOVE 5 TO N.
           ADD 1 TO N ON SIZE ERROR DISPLAY "S3 SE1" DISPLAY "S3 SE2".
           DISPLAY "S3 N=" N.
           MOVE 1 TO F. MOVE 2 TO G.
           IF F = 1 IF G = 1 DISPLAY "S4A P" ELSE DISPLAY "S4A Q".
           MOVE 2 TO F.
           IF F = 1 IF G = 1 DISPLAY "S4A P" ELSE DISPLAY "S4A WRONG".
           DISPLAY "S4A DONE".
           MOVE 1 TO F.
           IF F = 1 IF G = 1 DISPLAY "S4B X" ELSE DISPLAY "S4B Y"
           ELSE DISPLAY "S4B Z".
           MOVE 2 TO F.
           IF F = 1 IF G = 1 DISPLAY "S4B X" ELSE DISPLAY "S4B Y"
           ELSE DISPLAY "S4B Z".
           MOVE 5 TO N.
           IF F = 1 ADD 1 TO N ON SIZE ERROR DISPLAY "S4C SE"
           ELSE DISPLAY "S4C ELSE".
           MOVE 1 TO F.
           IF F = 1 ADD 1 TO N ON SIZE ERROR DISPLAY "S4C SE"
           ELSE DISPLAY "S4C ELSE".
           DISPLAY "S4C N=" N.
           STOP RUN.
