*>--- conformance-case
*> id: pb673_external_assign_executor
*> rules: [GR-12.4.5.3-3, GR-14.9.27.4-26]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §12.4.5.3 GR3 b) - THE ASSIGN ... USING SOURCE IS THE EXECUTING RUNTIME ELEMENT'S.
      *> "When the USING phrase of the ASSIGN clause is specified, the file connector referenced by file-name-1
      *> is associated with a physical file identified by the content of the data item referenced by data-name-1
      *> in the runtime element that executes the OPEN, SORT, or MERGE statement."
      *>
      *> The connector here is EXTERNAL, so §13.18.22.4 GR4 a) makes it ONE file connector for the whole run
      *> unit ("the file connector associated with this file description entry is an external file connector"),
      *> shared by BOTH describing programs - and §12.4.5.3 GR1 b) asks of their file control entries only "A
      *> consistent specification for data-name-1, device-name-1, and literal-1 in the ASSIGN clause. The
      *> implementor shall specify the consistency rules"; it does NOT require the same data item the way GR1 i)
      *> does for FILE STATUS ("where data-name-4 shall reference the same corresponding external data item").
      *> COBOL.NET's consistency rule compares the ASSIGN operand text (device class dropped, case ignored) and
      *> the USING data-name by SPELLING (docs/CONFORMANCE.md §7, DOC-A.1-72), which both entries below satisfy
      *> while each program keeps its OWN storage for FNAME.
      *>
      *> ORDER OF EVENTS: PB673A activates (registering the run-unit connector), CALLs PB673B - which activates
      *> and is therefore the LAST program to have run its file-section prologue - and only then executes the
      *> OPEN. GR3 b) names the EXECUTOR, so the physical file is the one PB673A's FNAME names.
      *>
      *> EXPECTED, derived from the rule:
      *>   OPEN=00  the association succeeds and the file is created (§14.9.27.4 Table 18, OUTPUT).
      *>   CHKB=05  "pb673b.dat" - the name in the LAST ACTIVATOR's data item - was never associated, so the
      *>            OPTIONAL connector finds it absent (§14.9.27.4 GR14 / §9.1.13.2 item 5).
      *>   CHKA=00  "pb673a.dat" - the name in the EXECUTOR's data item - exists and holds the record.
      *>   READA=AAAAA  read back through the same EXTERNAL connector.
      *> Before kb/Work PB673 the connector held ONE installed source closure, re-installed unguarded at every
      *> activation, so PB673B's FNAME answered PB673A's OPEN: the record landed in pb673b.dat, silently.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB673A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN USING FNAME
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS EXT-ST.
           SELECT OPTIONAL CHKB ASSIGN TO "pb673b.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-CK.
           SELECT OPTIONAL CHKA ASSIGN TO "pb673a.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-CK.
       DATA DIVISION.
       FILE SECTION.
       FD  F IS EXTERNAL.
       01  F-REC PIC X(5).
       FD  CHKB.
       01  CHKB-REC PIC X(5).
       FD  CHKA.
       01  CHKA-REC PIC X(5).
       WORKING-STORAGE SECTION.
      *> §12.4.5.2 SR7 - data-name-1 is an alphanumeric item, not subordinate to F's FD. It is PB673A's OWN
      *> storage: §12.4.5.3 GR1 b) does not make it external, and GR3 b) is what decides whose is read.
       01  FNAME  PIC X(20) VALUE "pb673a.dat".
       01  WS-CK  PIC XX.
      *> §12.4.5.3 GR1 i) + §14.8.4.2 - an external connector's FILE STATUS item shall be the SAME corresponding
      *> external data item in every describing element, so this one is EXTERNAL and identically named in both.
       01  EXT-ST IS EXTERNAL PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB673B".
           OPEN OUTPUT F.
           DISPLAY "OPEN=" EXT-ST.
           MOVE "AAAAA" TO F-REC.
           WRITE F-REC.
           CLOSE F.
           OPEN INPUT CHKB.
           DISPLAY "CHKB=" WS-CK.
           CLOSE CHKB.
           OPEN INPUT CHKA.
           DISPLAY "CHKA=" WS-CK.
           CLOSE CHKA.
           OPEN INPUT F.
           READ F.
           DISPLAY "READA=" F-REC.
           CLOSE F.
           STOP RUN.
       END PROGRAM PB673A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB673B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN USING FNAME
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS EXT-ST.
       DATA DIVISION.
       FILE SECTION.
      *> §13.4.5.4 GR2 d) - the same smallest and largest record size in every describing entry.
       FD  F IS EXTERNAL.
       01  F-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  FNAME  PIC X(20) VALUE "pb673b.dat".
       01  EXT-ST IS EXTERNAL PIC XX.
       PROCEDURE DIVISION.
       BMAIN.
      *> Activating is the whole job: this program is the LAST to run its prologue and must NOT thereby become
      *> the one whose FNAME the caller's OPEN reads.
           GOBACK.
       END PROGRAM PB673B.
