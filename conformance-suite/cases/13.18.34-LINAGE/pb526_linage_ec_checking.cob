*>--- conformance-case
*> id: pb526_linage_ec_checking
*> rules: [GR-13.18.34.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.34.4 GR6 b) 2 with CHECKING FOR EC-I-O-LINAGE ENABLED
      *> — the half of kb/Work PB526 that the format-1 twin
      *> (2023/pb526_linage_ec_condition) cannot reach, and the ONE place
      *> the raised exception-NAME is observable.
      *>
      *> ⛔ WHY THE NAME NEEDS ITS OWN WITNESS. Every other EC-I-O
      *> condition is derived from the I-O status's first digit:
      *>   python scripts/spec/cite.py --check 9.1.13.1 "If the first
      *>   character of the resulting I-O status value is one of the above
      *>   values or the I-O status value starts with a '0' and the second
      *>   character is not '0', the associated exception condition is set
      *>   to exist."   -> OK  §9.1.13.1  (General)
      *> and that list maps a '9x' to EC-I-O-IMP. EC-I-O-LINAGE is not in
      *> the list at all — §13.18.34.4 GR6 b) 2 NAMES it:
      *>   python scripts/spec/cite.py --check 13.18.34.4 "If the value
      *>   does not conform to these two rules, the EC-I-O-LINAGE
      *>   exception condition is set to exist."
      *>   -> OK  §13.18.34.4 6) b) 2.  (General rules)
      *> so the status alone would raise the WRONG condition. The rule
      *> that lets the specific one win is §14.6.13.1.1:
      *>   python scripts/spec/cite.py --check 14.6.13.1.1 "Unless
      *>   otherwise specified, if more than one exception is detected
      *>   during the execution of a statement, the one that is set to
      *>   exist is undefined."   -> OK  §14.6.13.1.1  (General)
      *> — GR6 b) 2 is the "otherwise specified". FUNCTION
      *> EXCEPTION-STATUS is what makes the answer visible:
      *>   python scripts/spec/cite.py --check 15.33.3 "A 31-character,
      *>   left-justified, alphanumeric character string that is the
      *>   exception-name or the value 'EXCEPTION-OBJECT', as applicable,
      *>   associated with the last exception status."
      *>   -> OK  §15.33.3 1)  (Returned value rule)
      *> so ST=EC-I-O-LINAGE below is the assertion, and ST=EC-I-O-IMP
      *> would be the defect this fixture exists to catch.
      *>
      *> THE DECLARATIVE IS FORMAT 3, SELECTED BY NAME:
      *>   python scripts/spec/cite.py --check 14.9.49.4 "All format 3 USE
      *>   statements in which file-name-2 is specified and
      *>   exception-name-2 is a level-3 exception-name are examined. If
      *>   the exception condition that was raised matches
      *>   exception-name-2 and the exception condition is associated with
      *>   file-name-2, that declarative is executed."
      *>   -> OK  §14.9.49.4 3) c)  (General rules)
      *> and it is reached because EC-I-O-LINAGE is Fatal (Table 13) and
      *> §14.6.13.1.3 5) runs an applicable USE for a fatal condition. A
      *> fatal condition whose declarative completes NORMALLY terminates
      *> the run unit, so the declarative ends in RESUME AT NEXT
      *> STATEMENT, which §14.6.13.1.2 1) makes a non-normal completion:
      *>   python scripts/spec/cite.py --check 14.9.33.4 "If the NEXT
      *>   STATEMENT phrase is specified, control is transferred to an
      *>   implicit CONTINUE statement that is determined as follows:"
      *>   -> OK  §14.9.33.4 2)  (General rules)
      *> which is the same continuation §13.18.34.4 GR6 b) 2 describes.
      *>
      *> EXPECTED VALUES. LPC is `LINAGE IS WS-SZ LINES` with WS-SZ = 0,
      *> so GR6 b) 1's value rule is violated at the completion of the
      *> OPEN OUTPUT (GR6 b) 1 reads the operand there) and again at the
      *> WRITE, which the GR6 b) 2 latch re-raises for. Each raise runs
      *> the declarative once — EC-USE prints the '90' status
      *> (docs/CONFORMANCE.md §7 DOC-A.1-110), the raised name, and the
      *> counter GR6 b) 2 sets to 0 — and each RESUME returns to the
      *> DISPLAY following the interrupted statement. FUNCTION
      *> EXCEPTION-STATUS returns a 31-character alphanumeric value
      *> (§15.33.3 r1), so the name is space-padded; the trailing spaces are
      *> part of the expected line.
       >>TURN EC-I-O-LINAGE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB526ECK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPC ASSIGN TO "pb526eck.prt"
               FILE STATUS IS FS-C.
       DATA DIVISION.
       FILE SECTION.
       FD LPC LINAGE IS WS-SZ LINES.
       01 C-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-SZ PIC 9(3) VALUE 0.
       01 FS-C  PIC XX  VALUE "??".
       01 LC-C  PIC 9(3).
       PROCEDURE DIVISION.
       DECLARATIVES.
       EC-SECT SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-LINAGE FILE LPC.
       EC-PARA.
           MOVE LINAGE-COUNTER OF LPC TO LC-C.
           DISPLAY "EC-USE FS=" FS-C " LC=" LC-C
               " ST=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-P.
           OPEN OUTPUT LPC.
           DISPLAY "C-OPEN FS=" FS-C.
           MOVE "AAAA" TO C-REC.
           WRITE C-REC.
           DISPLAY "C-W1 FS=" FS-C.
           CLOSE LPC.
           DISPLAY "C-CLOSE FS=" FS-C.
           STOP RUN.
