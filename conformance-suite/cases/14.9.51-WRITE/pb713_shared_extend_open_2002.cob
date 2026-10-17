*>--- conformance-case
*> id: pb713_shared_extend_open_2002
*> rules: [GR-14.9.51.4-18]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB713E02.
      *> kb/Work PB713 at the COBOL-2002 FLOOR — the shared OPEN EXTEND on
      *> every organization and framing that is legal in this edition.
      *>
      *> The full matrix is 2023/pb713_shared_extend_open; this is the
      *> subset that survives the edition floor, and the split is a fact
      *> about the constructs rather than a convenience. ORGANIZATION IS
      *> LINE SEQUENTIAL is COBOL-2023 (12.4.5.10.3 GR2; kb/Work PB688), so
      *> its four cases cannot appear here at all. What CAN, and does:
      *> record sequential FIXED width, record sequential RECORD VARYING —
      *> which is the framing whose whole-store probe carried the SAME
      *> defect as the reported line-sequential one — plus the RELATIVE and
      *> INDEXED organizations, and the two sharing spellings 14.9.27.3 SR8
      *> does not force a LOCK MODE clause onto.
      *>
      *> THE RULES. 14.9.27.4 GR1: "The execution of the OPEN statement
      *> causes the value of the I-O status associated with file-name-1 to
      *> be updated to one of the values in 9.1.13, I-O status"; 14.9.27.4 GR25: "If
      *> the execution of the OPEN statement is unsuccessful, the file is
      *> not affected". A status is the only outcome the standard offers —
      *> the runtime used to measure the EXTEND write-ordinal base from a
      *> second host handle on the path it had just opened for WRITE, and
      *> the host's refusal escaped the run unit as an unhandled exception.
      *> 9.1.15 3) is what makes the shape legal: "The sharing with all
      *> other mode allows concurrent access to a physical file through
      *> other file connectors specifying input, I-O, or extend mode".
      *>
      *> WHAT THE APPEND HAS TO DO. 14.9.27.4 GR15: "When the EXTEND phrase
      *> is specified, the OPEN statement positions the file immediately
      *> after the last logical record for that file." 14.9.51.4 GR18: "If
      *> there are records in the physical file, the first record written
      *> after the execution of the OPEN statement with the EXTEND phrase
      *> is the successor of the last record in the physical file"; GR19
      *> fixes the measurement point for the shared case: "the added
      *> records follow the records present in the physical file when it
      *> was opened". Hence AAAA then BBBB, in that order, everywhere.
      *> 14.9.51.4 GR29 a) — the relative extend's first released record is
      *> "assigned a record number that is one greater than the highest
      *> relative record number existing in the physical file", so R-K is
      *> 0002 over a file holding RRN 1. GR38 — the indexed extend's first
      *> released record "shall have a prime record key whose value is
      *> greater than the highest prime record key value existing in the
      *> physical file when it was opened", so K006 follows K002.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
      *> record sequential, FIXED width — the arm that never crashed, kept
      *> as the control that proves the harness is measuring the difference
           SELECT FF ASSIGN TO "pb713f2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-F.
      *> record sequential, RECORD VARYING — the confirmed sibling of the
      *> reported defect: its write-base probe was RecordFraming's
      *> whole-store read, refused for the identical share-mode reason
           SELECT FV ASSIGN TO "pb713v2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-V.
      *> LOCK MODE clause and no SHARING clause — 9.1.15's undetermined
      *> implementor default ("If no specification is made in either
      *> location, the implementor defines the sharing mode in which the
      *> file is opened")
           SELECT FK ASSIGN TO "pb713k2.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-K.
      *> SHARING WITH NO OTHER — 9.1.15 1), exclusive, record locks ignored
           SELECT FN ASSIGN TO "pb713n2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH NO OTHER
               FILE STATUS IS ST-N.
           SELECT FR ASSIGN TO "pb713r2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS R-K
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-R.
           SELECT FX ASSIGN TO "pb713x2.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS X-KEY
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-X.
      *> no file-control clause at all: the OPEN statement's own SHARING
      *> phrase is what makes the connector a 9.1.15 participant. READ
      *> ONLY, not ALL, because SR8 admits the ALL phrase only when "the
      *> LOCK MODE clause shall be specified in the file control entry for
      *> file-name-1", which this entry by construction has not got.
           SELECT FP ASSIGN TO "pb713p2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-P.
       DATA DIVISION.
       FILE SECTION.
       FD FF.
       01 F-REC PIC X(4).
       FD FV
           RECORD IS VARYING IN SIZE FROM 3 TO 8 CHARACTERS
               DEPENDING ON V-LEN.
       01 V-REC PIC X(8).
       FD FK.
       01 K-REC PIC X(4).
       FD FN.
       01 N-REC PIC X(4).
       FD FR.
       01 R-REC PIC X(4).
       FD FX.
       01 X-REC.
          05 X-KEY PIC X(4).
          05 X-VAL PIC X(3).
       FD FP.
       01 P-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 V-LEN PIC 9(4).
       01 R-K   PIC 9(4).
       01 ST-F  PIC XX.
       01 ST-V  PIC XX.
       01 ST-K  PIC XX.
       01 ST-N  PIC XX.
       01 ST-R  PIC XX.
       01 ST-X  PIC XX.
       01 ST-P  PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> ---- record sequential, fixed ----------------------------
           OPEN OUTPUT FF
           MOVE "AAAA" TO F-REC
           WRITE F-REC
           CLOSE FF
           OPEN EXTEND FF
           DISPLAY "F-EXT=" ST-F
           MOVE "BBBB" TO F-REC
           WRITE F-REC
           DISPLAY "F-W=" ST-F
           CLOSE FF
           OPEN INPUT FF
           READ FF AT END CONTINUE END-READ
           DISPLAY "F-1=" F-REC
           READ FF AT END CONTINUE END-READ
           DISPLAY "F-2=" F-REC
           CLOSE FF
      *> ---- record sequential, RECORD VARYING -------------------
           OPEN OUTPUT FV
           MOVE 4 TO V-LEN
           MOVE "AAAA" TO V-REC
           WRITE V-REC
           CLOSE FV
           OPEN EXTEND FV
           DISPLAY "V-EXT=" ST-V
           MOVE 5 TO V-LEN
           MOVE "BBBBB" TO V-REC
           WRITE V-REC
           DISPLAY "V-W=" ST-V
           CLOSE FV
           OPEN INPUT FV
           READ FV AT END CONTINUE END-READ
           DISPLAY "V-1=" V-LEN " " V-REC(1:V-LEN)
           READ FV AT END CONTINUE END-READ
           DISPLAY "V-2=" V-LEN " " V-REC(1:V-LEN)
           CLOSE FV
      *> ---- LOCK MODE only --------------------------------------
           OPEN OUTPUT FK
           MOVE "AAAA" TO K-REC
           WRITE K-REC
           CLOSE FK
           OPEN EXTEND FK
           DISPLAY "K-EXT=" ST-K
           MOVE "BBBB" TO K-REC
           WRITE K-REC
           DISPLAY "K-W=" ST-K
           CLOSE FK
           OPEN INPUT FK
           READ FK AT END CONTINUE END-READ
           DISPLAY "K-1=" K-REC
           READ FK AT END CONTINUE END-READ
           DISPLAY "K-2=" K-REC
           CLOSE FK
      *> ---- SHARING WITH NO OTHER -------------------------------
           OPEN OUTPUT FN
           MOVE "AAAA" TO N-REC
           WRITE N-REC
           CLOSE FN
           OPEN EXTEND FN
           DISPLAY "N-EXT=" ST-N
           MOVE "BBBB" TO N-REC
           WRITE N-REC
           DISPLAY "N-W=" ST-N
           CLOSE FN
           OPEN INPUT FN
           READ FN AT END CONTINUE END-READ
           DISPLAY "N-1=" N-REC
           READ FN AT END CONTINUE END-READ
           DISPLAY "N-2=" N-REC
           CLOSE FN
      *> ---- relative --------------------------------------------
           OPEN OUTPUT FR
           MOVE "AAAA" TO R-REC
           WRITE R-REC
           CLOSE FR
           OPEN EXTEND FR
           DISPLAY "R-EXT=" ST-R
           MOVE "BBBB" TO R-REC
           WRITE R-REC
           DISPLAY "R-W=" ST-R " R-K=" R-K
           CLOSE FR
           OPEN INPUT FR
           READ FR AT END CONTINUE END-READ
           DISPLAY "R-1=" R-K " " R-REC
           READ FR AT END CONTINUE END-READ
           DISPLAY "R-2=" R-K " " R-REC
           CLOSE FR
      *> ---- indexed ---------------------------------------------
           OPEN OUTPUT FX
           MOVE "K002" TO X-KEY
           MOVE "V02" TO X-VAL
           WRITE X-REC
           CLOSE FX
           OPEN EXTEND FX
           DISPLAY "X-EXT=" ST-X
           MOVE "K006" TO X-KEY
           MOVE "V06" TO X-VAL
           WRITE X-REC
           DISPLAY "X-W=" ST-X
           CLOSE FX
           OPEN INPUT FX
           READ FX AT END CONTINUE END-READ
           DISPLAY "X-1=" X-REC
           READ FX AT END CONTINUE END-READ
           DISPLAY "X-2=" X-REC
           CLOSE FX
      *> ---- the OPEN statement's own SHARING phrase --------------
           OPEN OUTPUT FP
           MOVE "AAAA" TO P-REC
           WRITE P-REC
           CLOSE FP
           OPEN EXTEND SHARING WITH READ ONLY FP
           DISPLAY "P-EXT=" ST-P
           MOVE "BBBB" TO P-REC
           WRITE P-REC
           DISPLAY "P-W=" ST-P
           CLOSE FP
           OPEN INPUT FP
           READ FP AT END CONTINUE END-READ
           DISPLAY "P-1=" P-REC
           READ FP AT END CONTINUE END-READ
           DISPLAY "P-2=" P-REC
           CLOSE FP
           STOP RUN.
