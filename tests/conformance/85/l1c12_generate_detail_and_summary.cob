      *> ISO §14.9.16.2 / §14.9.16.4 GR2 — GENERATE data-name-1 and
      *>   GENERATE report-name-1 (summary reporting)
      *>
      *> FORMAT (§14.9.16.2): GENERATE { data-name-1 | report-name-1 } —
      *>   both alternatives are written below:
      *> GENERATE DE1 (a detail report group, SR1) and GENERATE R-1 (a
      *>   report-name whose RD has a CONTROL
      *> clause, SR2).
      *>   cite.py --check 14.9.16.2 "GENERATE"
      *>     -> OK  §14.9.16.2   (General format)
      *>   cite.py --check 14.9.16.3 "Data-name-1 shall name a detail
      *>     report group."
      *>     -> OK  §14.9.16.3 1)  (Syntax rules)
      *>   cite.py --check 14.9.16.3 "Report-name-1 may be used only if
      *>     the referenced report description
      *>     entry contains a CONTROL clause."  -> OK  §14.9.16.3 2)
      *>       (Syntax rules)
      *> GR2: "Execution of a GENERATE report-name-1 statement results
      *>   in the same processing as for a
      *> GENERATE data-name statement for the same report, except that
      *>   no detail is printed."
      *>   cite.py --check 14.9.16.4 "Execution of a GENERATE
      *>     report-name-1 statement results in the same
      *>     processing as for a GENERATE data-name statement for the
      *>       same report, except that no detail is
      *>     printed."  -> OK  §14.9.16.4 2)  (General rules)
      *> GR4 c)/d) (first GENERATE after INITIATE): "each control
      *>   heading is printed, wherever defined, in
      *> order from major to minor" ... "The specified detail is
      *>   printed, unless summary reporting is
      *> specified."  (cite.py prints these sub-items under the label
      *>   "4) a)")
      *>   cite.py --check 14.9.16.4 "each control heading is printed,
      *>     wherever defined, in order from major
      *>     to minor"  -> OK  §14.9.16.4 4) a)  (General rules)
      *>   cite.py --check 14.9.16.4 "The specified detail is printed,
      *>     unless summary reporting is
      *>     specified."  -> OK  §14.9.16.4 4) a)  (General rules)
      *> GR5 a)/b) (subsequent GENERATEs): "each control footing and
      *>   control heading is printed, if defined,
      *> up to the level of the control break" and the detail unless
      *>   summary reporting.
      *>   cite.py --check 14.9.16.4 "each control footing and control
      *>     heading is printed, if defined, up to
      *>     the level of the control break"  -> OK  §14.9.16.4 5) a)
      *>       (General rules)
      *>
      *> The report is not divided into pages (no PAGE clause), so no
      *>   page heading/footing/fit test applies
      *> (GR6).  Every group is LINE PLUS 1, so each printed group is
      *>   one line.  The report file is read back
      *> byte by byte and each non-empty line is DISPLAYed.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT (K1 is the only control):
      *>   K1=1, GENERATE R-1  : the chronologically first GENERATE ->
      *>     GR4 c) prints CH1 ("CH1"), GR4 d)
      *>                         prints NO detail (summary reporting).
      *>                           => CH1
      *>   K1=1, GENERATE DE1  : no break; GR5 b) prints the detail.
      *>     => DE1
      *>   K1=2, GENERATE R-1  : break on K1 -> GR5 a) CF1 then CH1 (now
      *>     K1=2);
      *>                         GR5 b) NO detail.
      *>                           => CF, CH2
      *>   K1=2, GENERATE DE1  : no break; detail.
      *>     => DE2
      *>   K1=2, GENERATE R-1  : no break, summary -> nothing at all (a
      *>     wrong implementation that printed the
      *>                         detail would add a second DE2 here).
      *>   TERMINATE           : final control footing CF1 (§14.9.46.4).
      *>     => CF
      *> The CF line carries only a literal so it does not depend on the
      *>   §13.18.16 prior-value rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C12A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D SYM-X0C
               ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "L1C12A.RPT".
           SELECT CHK ASSIGN TO "L1C12A.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  K1      PIC 9     VALUE 1.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROL IS K1.
       01  CH1 TYPE CONTROL HEADING K1 LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "CH".
           02  COLUMN 3 PIC 9 SOURCE K1.
       01  DE1 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DE".
           02  COLUMN 3 PIC 9 SOURCE K1.
       01  CF1 TYPE CONTROL FOOTING K1 LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "CF".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE R-1.
           GENERATE DE1.
           MOVE 2 TO K1.
           GENERATE R-1.
           GENERATE DE1.
           GENERATE R-1.
           TERMINATE R-1.
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
               IF CHK-REC NOT = SYM-X0D AND CHK-REC NOT = SYM-X0C
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
