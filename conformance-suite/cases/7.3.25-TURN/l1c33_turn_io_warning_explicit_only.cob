      *>--- conformance-case
      *> id: l1c33_turn_io_warning_explicit_only
      *> rules: [GR-7.3.25.4-3, GR-7.3.25.4-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.25.4 GR4 — EC-I-O-WARNING only explicitly on/off
      *> GR4: "The exception-name EC-I-O-WARNING may be turned on only
      *> by specifying it explicitly within a TURN directive or by its
      *> presence in a WHEN phrase in an exception-checking PERFORM
      *> statement. Similarly, it may be turned off only by an explicit
      *> TURN directive or the end of an exception-checking PERFORM
      *> statement."
      *>   cite.py: OK  §7.3.25.4 4)  (General rules)
      *> Also pins the EC-I-O-WARNING exclusion of GR2 (EC-ALL) and GR3
      *> (level-2 EC-I-O):
      *>   cite.py: OK  §7.3.25.4 2)  (General rules)
      *>   cite.py: OK  §7.3.25.4 3)  (General rules)
      *> Supporting: §9.1.13.2 4) a) - OPEN of an OPTIONAL file that is
      *> not present succeeds with I-O status 05 (INPUT mode creates
      *> nothing, so every OPEN INPUT here gives 05); §9.1.13.1 "The
      *> exception-name for successful completion with an I-O status
      *> value that is not '00' is EC-I-O-WARNING" (cite.py: OK
      *> §9.1.13.1); §15.33.3 1) EXCEPTION-STATUS is the 31-character
      *> last exception status, spaces when none (cite.py: OK
      *> §15.33.3 1)). SET LAST EXCEPTION TO OFF clears it before each
      *> observation so every line stands alone.
      *> DERIVATION (every OPEN INPUT F2 yields FS=05):
      *>   T1 after TURN EC-ALL ON: GR2 excludes the warning -> []
      *>   T2 after TURN EC-I-O ON: GR3 excludes the warning -> []
      *>   T3 after TURN EC-I-O-WARNING ON: explicit -> [EC-I-O-WARNING]
      *>   T4 after TURN EC-ALL OFF: not an explicit OFF for the
      *>      warning, so it stays on -> [EC-I-O-WARNING]
      *>   T5 after TURN EC-I-O OFF: likewise -> [EC-I-O-WARNING]
      *>   T6 after TURN EC-I-O-WARNING OFF: explicit -> []
      *>   Q1 PERFORM ... WHEN EC-I-O-WARNING: the WHEN turns it on for
      *>      imperative-statement-1, so the OPEN raises it and the WHEN
      *>      runs -> "Q1 WHEN [EC-I-O-WARNING]"; the end of the PERFORM
      *>      turns it off, so the next OPEN -> "Q1 AFTER ... []"
      *>   Q2 PERFORM ... WHEN EC-I-O: not an explicit presence of the
      *>      warning -> no WHEN line, "Q2 ... []"
      *>   Q3 PERFORM ... WHEN EC-ALL: likewise -> no WHEN line, "Q3 []"
      *> EDITION: EC-I-O-WARNING and the exception-checking PERFORM are
      *> COBOL-2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C33E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL F2 ASSIGN TO "L1C33E-NOSUCH.DAT"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F2.
       01 R2 PIC X(5).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       M1.
       >>TURN EC-ALL CHECKING ON
           SET LAST EXCEPTION TO OFF.
           OPEN INPUT F2.
           DISPLAY "T1 FS=" FS " [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F2.
       >>TURN EC-I-O CHECKING ON
           SET LAST EXCEPTION TO OFF.
           OPEN INPUT F2.
           DISPLAY "T2 FS=" FS " [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F2.
       >>TURN EC-I-O-WARNING CHECKING ON
           SET LAST EXCEPTION TO OFF.
           OPEN INPUT F2.
           DISPLAY "T3 FS=" FS " [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F2.
       >>TURN EC-ALL CHECKING OFF
           SET LAST EXCEPTION TO OFF.
           OPEN INPUT F2.
           DISPLAY "T4 FS=" FS " [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F2.
       >>TURN EC-I-O CHECKING OFF
           SET LAST EXCEPTION TO OFF.
           OPEN INPUT F2.
           DISPLAY "T5 FS=" FS " [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F2.
       >>TURN EC-I-O-WARNING CHECKING OFF
           SET LAST EXCEPTION TO OFF.
           OPEN INPUT F2.
           DISPLAY "T6 FS=" FS " [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F2.
           SET LAST EXCEPTION TO OFF.
           PERFORM
               OPEN INPUT F2
             WHEN EC-I-O-WARNING
               DISPLAY "Q1 WHEN [" FUNCTION EXCEPTION-STATUS "]"
           END-PERFORM.
           CLOSE F2.
           SET LAST EXCEPTION TO OFF.
           OPEN INPUT F2.
           DISPLAY "Q1 AFTER FS=" FS " [" FUNCTION EXCEPTION-STATUS
               "]".
           CLOSE F2.
           SET LAST EXCEPTION TO OFF.
           PERFORM
               OPEN INPUT F2
             WHEN EC-I-O
               DISPLAY "Q2 WHEN [" FUNCTION EXCEPTION-STATUS "]"
           END-PERFORM.
           DISPLAY "Q2 FS=" FS " [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F2.
           SET LAST EXCEPTION TO OFF.
           PERFORM
               OPEN INPUT F2
             WHEN EC-ALL
               DISPLAY "Q3 WHEN [" FUNCTION EXCEPTION-STATUS "]"
           END-PERFORM.
           DISPLAY "Q3 FS=" FS " [" FUNCTION EXCEPTION-STATUS "]".
           CLOSE F2.
           STOP RUN.
