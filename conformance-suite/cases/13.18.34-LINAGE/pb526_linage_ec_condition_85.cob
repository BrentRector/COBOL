      *>--- conformance-case
      *> id: pb526_linage_ec_condition_85
      *> rules: [GR-13.18.34.4-3, GR-13.18.34.4-6, DOC-A.1-110]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The COBOL-85 EDITION TWIN of
      *> conformance-suite/cases/9.1.13.11-Implementor-defined-condition-with-unsuccessful-completion/pb526_linage_ec_condition, which carries
      *> the full derivation and every cite.py --check line.
      *>
      *> WHY THE TWIN EXISTS. §13.18.34.4 GR6 b)'s two value rules and the
      *> continuation they prescribe are VERSION-INVARIANT — no edition
      *> gate touches the LINAGE clause — but the two channels the rule
      *> reaches a program through are not equally old. The exception
      *> condition it names is the 2002 EC model's (checking is enabled by
      *> a >>TURN directive, which COBOL-85 does not have), while the I-O
      *> STATUS and the format-1 USE declarative that read it are COBOL-85
      *> constructs. §14.6.13.1.3 3) is what joins them:
      *>   python scripts/spec/cite.py --check 14.6.13.1.3 "If the
      *>   exception condition is a fatal EC-I-O exception condition, then
      *>   the rules for 9.1.13, I-O status apply, then if the implementor
      *>   has not specified otherwise, the following rules apply for all
      *>   fatal exception conditions."   -> OK  §14.6.13.1.3 3)
      *> so the whole of GR6 b) 2's observable behaviour — the '90' status
      *> (docs/CONFORMANCE.md §7 DOC-A.1-110), the declarative, the
      *> counter pinned at 0, the continuation with the statement after
      *> the WRITE, every later WRITE re-raising, and the CLOSE that ends
      *> it — is reachable from a program that uses NO construct newer
      *> than 1985. THAT is what this file asserts: compile it at
      *> `--std 85` and the answers are identical to the 2023 twin's.
      *> Without it the rule would be pinned only where the EC model
      *> exists, and a regression that made the behaviour depend on the
      *> EC subsystem being present would stay green.
      *>
      *> The expected values are the 2023 twin's, line for line, for the
      *> same reasons; see that file for each one's derivation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB526E85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPA ASSIGN TO "pb526e85-a.prt"
               FILE STATUS IS FS-A.
           SELECT LPB ASSIGN TO "pb526e85-b.prt"
               FILE STATUS IS FS-B.
       DATA DIVISION.
       FILE SECTION.
       FD LPA LINAGE IS WS-SZ LINES.
       01 A-REC PIC X(4).
       FD LPB LINAGE IS 5 LINES WITH FOOTING AT WS-FT.
       01 B-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-SZ PIC 9(3) VALUE 2.
       01 WS-FT PIC 99  VALUE 0.
       01 FS-A  PIC XX  VALUE "??".
       01 FS-B  PIC XX  VALUE "??".
       01 LA    PIC 9(3).
       01 LB    PIC 9(3).
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-A-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON LPA.
       ERR-A-PARA.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "USE-A FS=" FS-A " LC=" LA.
       ERR-B-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON LPB.
       ERR-B-PARA.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "USE-B FS=" FS-B " LC=" LB.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-P.
           OPEN OUTPUT LPA.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-OPEN FS=" FS-A " LC=" LA.
           MOVE "AAAA" TO A-REC.
           WRITE A-REC.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-W1 FS=" FS-A " LC=" LA.
           MOVE 0 TO WS-SZ.
           MOVE "BBBB" TO A-REC.
           WRITE A-REC.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-W2 FS=" FS-A " LC=" LA.
           MOVE "CCCC" TO A-REC.
           WRITE A-REC.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-W3 FS=" FS-A " LC=" LA.
           CLOSE LPA.
           DISPLAY "A-CLOSE FS=" FS-A.
           MOVE 3 TO WS-SZ.
           OPEN OUTPUT LPA.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-REOPEN FS=" FS-A " LC=" LA.
           MOVE "DDDD" TO A-REC.
           WRITE A-REC.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-W4 FS=" FS-A " LC=" LA.
           CLOSE LPA.
           OPEN OUTPUT LPB.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "B-OPEN FS=" FS-B " LC=" LB.
           MOVE "EEEE" TO B-REC.
           WRITE B-REC AFTER ADVANCING 2 LINES
               AT END-OF-PAGE DISPLAY "B-EOP1"
               NOT AT END-OF-PAGE DISPLAY "B-NOEOP1"
           END-WRITE.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "B-W1 FS=" FS-B " LC=" LB.
           CLOSE LPB.
           DISPLAY "B-CLOSE FS=" FS-B.
           MOVE 3 TO WS-FT.
           OPEN OUTPUT LPB.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "B-REOPEN FS=" FS-B " LC=" LB.
           MOVE "FFFF" TO B-REC.
           WRITE B-REC AFTER ADVANCING 2 LINES
               AT END-OF-PAGE DISPLAY "B-EOP2"
               NOT AT END-OF-PAGE DISPLAY "B-NOEOP2"
           END-WRITE.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "B-W2 FS=" FS-B " LC=" LB.
           CLOSE LPB.
           STOP RUN.
