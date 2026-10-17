*>--- conformance-case
*> id: pb714_sort_giving_open_status
*> rules: [GR-14.9.40.4-15]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB714SGO.
      *> kb/Work PB714 — SORT's implicit GIVING OPEN.
      *>
      *> ISO 14.9.40.4 GR15 a): "The processing of the file is initiated.
      *> The initiation is performed as if an OPEN statement with the OUTPUT
      *> and SHARING WITH NO OTHER phrases had been executed.  This
      *> initiation is performed after the execution of any input
      *> procedure."  9.1.15 1) is what the phrase buys: "The sharing with
      *> no other mode specifies exclusive access to a physical file."
      *>
      *> The exclusive half of that mode — refusing OTHER connectors while
      *> the GIVING file is open — is not reachable from a single-threaded
      *> COBOL program, because no statement of the program's runs between
      *> the implicit OPEN and the implicit CLOSE; CobolFileLockTests
      *> pins it at the registry, where the emitted call lands.  What IS
      *> reachable, and what this golden pins, is the other half: the
      *> as-if OPEN's own I-O status.  9.1.13.1 — "The value of the I-O
      *> status is set during the execution of a CLOSE, DELETE, OPEN, READ,
      *> REWRITE, START, UNLOCK or WRITE statement and prior to the
      *> execution of ... any applicable exception processing statements" —
      *> with 12.4.5.8.4 GR1 puts it in the FILE STATUS item before the USE
      *> procedure GR15's closing paragraph invokes ("If a nonfatal
      *> exception condition exists for file-name-3 as a result of the
      *> implicit OPEN during file initiation, and there is an applicable
      *> USE procedure that completes normally, or no applicable USE
      *> procedure is available, the SORT statement continues").  D-ST is
      *> seeded "ZZ", so an implicit OPEN that stored no status shows as
      *> "ZZ".
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SRC ASSIGN TO "pb714sgos.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS S-ST.
           SELECT F-DST ASSIGN TO "pb714sgod.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS D-ST.
           SELECT F-OTH ASSIGN TO "pb714sgod.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS O-ST.
           SELECT SRT-FILE ASSIGN TO "pb714sgot.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD F-SRC.
       01 S-REC    PIC X(5).
       FD F-DST.
       01 D-REC    PIC X(5).
       FD F-OTH.
       01 O-REC    PIC X(5).
       SD SRT-FILE.
       01 SRT-REC  PIC X(5).
       WORKING-STORAGE SECTION.
       01 S-ST     PIC XX.
       01 D-ST     PIC XX.
       01 O-ST     PIC XX.
       01 N        PIC 9 VALUE 0.
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DD-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F-DST.
       DD-PARA.
           DISPLAY "USE-DST=" D-ST.
       END DECLARATIVES.
       MAIN SECTION.
       SEED.
           MOVE "ZZ" TO D-ST.
           OPEN OUTPUT F-SRC.
           MOVE "CCCCC" TO S-REC.
           WRITE S-REC.
           MOVE "AAAAA" TO S-REC.
           WRITE S-REC.
           CLOSE F-SRC.
           OPEN OUTPUT F-OTH.
           MOVE "OLDIE" TO O-REC.
           WRITE O-REC.
           CLOSE F-OTH.
      *> LEG 1 — a second connector holds the GIVING file's physical file
      *> open across the SORT.  Every "Open request" row of Table 19 whose
      *> open mode is OUTPUT is Unsuccessful open in all five columns —
      *> 9.1.13.9 1) e), "An attempt is made to open a physical file in the
      *> output mode and the physical file is currently open by another file
      *> connector" — so the implicit OPEN OUTPUT is refused '61'.  '61' is
      *> nonfatal, so the SORT continues (GR15, above) and its first as-if
      *> WRITE meets a connector that is not open: 14.9.51.4 => '48'.  '48'
      *> is FATAL (9.1.13.1: "any that begin with the digit 3, 4, or 7"),
      *> and SORT has no rule of its own for a failed implicit WRITE other
      *> than the boundary one, so 9.1.13.1's own disposition governs:
      *> "control is transferred to the end of the statement that produced
      *> the fatal exception condition" - the SORT is terminated after that
      *> one USE invocation (kb/Work PB993; 14.6.13.1.3 2): "If the
      *> executed statement is a MERGE or SORT statement, then the rules
      *> for those statements apply").  No further WRITE, no as-if CLOSE:
      *> the last status stored is the WRITE's '48'.
       LEG-1.
           OPEN INPUT F-OTH.
           SORT SRT-FILE ON ASCENDING KEY SRT-REC
               USING F-SRC GIVING F-DST.
           CLOSE F-OTH.
           DISPLAY "L1-DST=" D-ST.
      *> 14.9.27.4 GR25 — "If the execution of the OPEN statement is
      *> unsuccessful, the file is not affected": the refused implicit OPEN
      *> OUTPUT truncated nothing, so the pre-existing record survives.
           PERFORM COUNT-DST.
           DISPLAY "L1-COUNT=" N.
      *> LEG 2 — nothing else is open, so the same SORT runs: OPEN OUTPUT
      *> truncates the GIVING file (14.9.27.4 GR18) and the sorted records
      *> replace it (GR15 b, "The sorted logical records are returned and
      *> written onto the file").
       LEG-2.
           MOVE "ZZ" TO D-ST.
           SORT SRT-FILE ON ASCENDING KEY SRT-REC
               USING F-SRC GIVING F-DST.
           DISPLAY "L2-DST=" D-ST.
           PERFORM COUNT-DST.
           DISPLAY "L2-COUNT=" N.
           STOP RUN.
       COUNT-DST.
           MOVE 0 TO N.
           MOVE "N" TO EOF-FLAG.
           OPEN INPUT F-DST.
           PERFORM UNTIL EOF-FLAG = "Y"
               READ F-DST
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END ADD 1 TO N
                       DISPLAY "REC=" D-REC
               END-READ
           END-PERFORM.
           CLOSE F-DST.
