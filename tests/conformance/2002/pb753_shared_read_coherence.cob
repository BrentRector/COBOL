       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB753R02.
      *> kb/Work PB753 at the COBOL-2002 FLOOR - a READ delivers the record the PHYSICAL
      *> FILE holds NOW, never an image a sibling connector has already
      *> REWRITTEN and that this connector merely has in a buffer.
      *>
      *> THE RULES.
      *> 14.9.35.4 GR4: "The successful execution of the REWRITE
      *> statement releases a logical record to the operating
      *> environment." So a '00' from the sibling's REWRITE is a promise
      *> that the record IS in the physical file.
      *> 14.9.51.4 GR12 says the same of WRITE, which is why the APPEND
      *> leg below is a control and not a second defect.
      *> 9.1.15 3): "The sharing with all other mode allows concurrent
      *> access to a physical file through other file connectors
      *> specifying input, I-O, or extend mode" - the shape is legal,
      *> explicitly, and 14.9.27.4 Table 19 makes ALL OTHER/I-O against
      *> ALL OTHER/INPUT a Normal open.
      *> 14.9.30.4 GR21 c): the record selected by a sequential READ is
      *> "the first existing record IN THE PHYSICAL FILE whose relative
      *> key number is greater than the file position indicator if NEXT
      *> is specified or implied", and d): "If a record is found
      *> according to the above rules, the record is made available in
      *> the record area associated with file-name-1". THE PHYSICAL
      *> FILE, at the READ - not a snapshot of what it said earlier.
      *>
      *> WHY 300 RECORDS. The reader's read-ahead is 1024 characters;
      *> 300 four-byte records is 1200 bytes, so the file cannot sit
      *> inside one buffer and the staleness is a property of the
      *> mechanism rather than of the file size. Record 3 is inside the
      *> buffer the reader filled taking record 1, which is what makes
      *> the measurement about the buffer.
      *>
      *> EVERY EXPECTED VALUE. Each leg seeds 300 "OLD" records through
      *> an exclusive connector, opens a reader INPUT and takes record 1
      *> (=OLD), opens a sibling I-O connector, reads to record 3,
      *> REWRITEs it to NEW (GR4 releases it) and closes; the reader
      *> then takes records 2 and 3. Record 2 was not rewritten, so it
      *> is OLD; record 3 WAS, so GR21 c)/d) require NEW. Every status
      *> is '00' - 9.1.13.2 item 1, successful completion - and no
      *> status anywhere reports staleness, which is why a wrong answer
      *> here is silent.
      *>
      *> THE N- LEG (formerly the CLAUSE-LESS LEG). RE-DERIVED BY kb/Work
      *> PB322: it carried no SHARING and no LOCK MODE clause anywhere,
      *> because kb/Work PB740 had made Table 19 admit such a pair on one
      *> physical file while 9.1.15's implementor default was undetermined.
      *> The default is now determined (docs/CONFORMANCE.md DOC-A.1-77 and
      *> DOC-A.1-131): OPEN INPUT is SHARING WITH READ ONLY and OPEN I-O
      *> is SHARING WITH NO OTHER, and Table 19 refuses that pair '61'
      *> (see 2002/pb322_default_sharing_by_open_mode). NR and NW below
      *> therefore declare SHARING WITH ALL OTHER with the LOCK MODE clause
      *> 14.9.27.3 SR8 couples to it, and the leg keeps the stricter
      *> SEQUENCE it was built for:
      *>
      *> THE N- LEG USES A DIFFERENT SEQUENCE THAN THE F-/V-/L- LEGS. It
      *> re-fills its read-ahead AFTER the sibling's OPEN (N-R2) and reads
      *> on while the sibling is STILL OPEN (N-R3, N-R4), so no OPEN or
      *> CLOSE of the sibling sits between the fill and the read and
      *> nothing but the coherence rule can deliver the rewritten record.
      *> The sibling REWRITEs ordinal 4 here, so N-R3 is OLD and N-R4 is
      *> NEW.
      *>
      *> THE APPEND CONTROL (A-) is the leg that was already correct and
      *> shall stay correct: a reader can only have buffered what
      *> existed, so a record a sibling adds after the reader has
      *> consumed the file is delivered (14.9.51.4 GR12 released it).
      *>
      *> THE KEYED CONTROLS (R-, X-) were already correct for the reason
      *> that names the fix: RELATIVE and INDEXED connectors over one
      *> host path share ONE record store (kb/Work PB143), so a
      *> sibling's REWRITE is visible the instant it happens. 14.9.35.4
      *> GR4 is the same rule for them; only the medium differs.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "pb753f2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT FR ASSIGN TO "pb753f2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-R.
           SELECT FW ASSIGN TO "pb753f2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-W.
           SELECT VS ASSIGN TO "pb753v2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT VR ASSIGN TO "pb753v2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-R.
           SELECT VW ASSIGN TO "pb753v2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-W.
           SELECT NS ASSIGN TO "pb753n2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT NR ASSIGN TO "pb753n2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-R.
           SELECT NW ASSIGN TO "pb753n2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-W.
           SELECT APS ASSIGN TO "pb753a2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT APR ASSIGN TO "pb753a2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-R.
           SELECT APW ASSIGN TO "pb753a2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-W.
           SELECT RS ASSIGN TO "pb753r2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK-S
               FILE STATUS IS ST-S.
           SELECT RR ASSIGN TO "pb753r2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK-R
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-R.
           SELECT RW ASSIGN TO "pb753r2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK-W
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-W.
           SELECT XS ASSIGN TO "pb753x2.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XS-KEY
               FILE STATUS IS ST-S.
           SELECT XR ASSIGN TO "pb753x2.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XR-KEY
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-R.
           SELECT XW ASSIGN TO "pb753x2.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XW-KEY
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-W.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 FS-REC PIC X(4).
       FD FR.
       01 FR-REC PIC X(4).
       FD FW.
       01 FW-REC PIC X(4).
       FD NS.
       01 NS-REC PIC X(4).
       FD NR.
       01 NR-REC PIC X(4).
       FD NW.
       01 NW-REC PIC X(4).
       FD APS.
       01 APS-REC PIC X(4).
       FD APR.
       01 APR-REC PIC X(4).
       FD APW.
       01 APW-REC PIC X(4).
       FD VS RECORD IS VARYING IN SIZE FROM 4 TO 8 CHARACTERS.
       01 VS-REC PIC X(8).
       FD VR RECORD IS VARYING IN SIZE FROM 4 TO 8 CHARACTERS.
       01 VR-REC PIC X(8).
       FD VW RECORD IS VARYING IN SIZE FROM 4 TO 8 CHARACTERS.
       01 VW-REC PIC X(8).
       FD RS.
       01 RS-REC PIC X(4).
       FD RR.
       01 RR-REC PIC X(4).
       FD RW.
       01 RW-REC PIC X(4).
       FD XS.
       01 XS-REC.
          05 XS-KEY PIC X(4).
          05 XS-VAL PIC X(4).
       FD XR.
       01 XR-REC.
          05 XR-KEY PIC X(4).
          05 XR-VAL PIC X(4).
       FD XW.
       01 XW-REC.
          05 XW-KEY PIC X(4).
          05 XW-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST-S PIC XX.
       01 ST-R PIC XX.
       01 ST-W PIC XX.
       01 RK-S PIC 9(4).
       01 RK-R PIC 9(4).
       01 RK-W PIC 9(4).
       01 I    PIC 9(4).
       01 KEYX PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> F
           OPEN OUTPUT FS
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 300
             MOVE "OLD " TO FS-REC
             WRITE FS-REC
           END-PERFORM
           CLOSE FS
           OPEN INPUT FR
           READ FR AT END CONTINUE END-READ
           DISPLAY "F-R1=" FR-REC
           OPEN I-O FW
           DISPLAY "F-IO=" ST-W
           READ FW AT END CONTINUE END-READ
           READ FW AT END CONTINUE END-READ
           READ FW AT END CONTINUE END-READ
           MOVE "NEW " TO FW-REC
           REWRITE FW-REC
           DISPLAY "F-RW=" ST-W
           CLOSE FW
           READ FR AT END CONTINUE END-READ
           DISPLAY "F-R2=" FR-REC
           READ FR AT END CONTINUE END-READ
           DISPLAY "F-R3=" FR-REC
           CLOSE FR
      *> V
           OPEN OUTPUT VS
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 300
             MOVE "OLD " TO VS-REC
             WRITE VS-REC
           END-PERFORM
           CLOSE VS
           OPEN INPUT VR
           READ VR AT END CONTINUE END-READ
           DISPLAY "V-R1=" VR-REC
           OPEN I-O VW
           DISPLAY "V-IO=" ST-W
           READ VW AT END CONTINUE END-READ
           READ VW AT END CONTINUE END-READ
           READ VW AT END CONTINUE END-READ
           MOVE "NEW " TO VW-REC
           REWRITE VW-REC
           DISPLAY "V-RW=" ST-W
           CLOSE VW
           READ VR AT END CONTINUE END-READ
           DISPLAY "V-R2=" VR-REC
           READ VR AT END CONTINUE END-READ
           DISPLAY "V-R3=" VR-REC
           CLOSE VR
      *> N
           OPEN OUTPUT NS
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 300
             MOVE "OLD " TO NS-REC
             WRITE NS-REC
           END-PERFORM
           CLOSE NS
           OPEN INPUT NR
           READ NR AT END CONTINUE END-READ
           DISPLAY "N-R1=" NR-REC
           OPEN I-O NW
           DISPLAY "N-IO=" ST-W
           READ NR AT END CONTINUE END-READ
           DISPLAY "N-R2=" NR-REC
           READ NW AT END CONTINUE END-READ
           READ NW AT END CONTINUE END-READ
           READ NW AT END CONTINUE END-READ
           READ NW AT END CONTINUE END-READ
           MOVE "NEW " TO NW-REC
           REWRITE NW-REC
           DISPLAY "N-RW=" ST-W
           READ NR AT END CONTINUE END-READ
           DISPLAY "N-R3=" NR-REC
           READ NR AT END CONTINUE END-READ
           DISPLAY "N-R4=" NR-REC
           CLOSE NW
           CLOSE NR
      *> A - the control: a record a sibling ADDS is delivered
           OPEN OUTPUT APS
           MOVE "OLD " TO APS-REC
           WRITE APS-REC
           CLOSE APS
           OPEN INPUT APR
           READ APR AT END CONTINUE END-READ
           DISPLAY "A-R1=" APR-REC
           OPEN EXTEND APW
           MOVE "APPD" TO APW-REC
           WRITE APW-REC
           DISPLAY "A-W=" ST-W
           CLOSE APW
           READ APR AT END CONTINUE END-READ
           DISPLAY "A-R2=" APR-REC
           CLOSE APR
      *> R - RELATIVE, already correct (one shared store)
           OPEN OUTPUT RS
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 300
             MOVE "OLD " TO RS-REC
             WRITE RS-REC
           END-PERFORM
           CLOSE RS
           OPEN INPUT RR
           READ RR AT END CONTINUE END-READ
           DISPLAY "R-R1=" RR-REC
           OPEN I-O RW
           READ RW AT END CONTINUE END-READ
           READ RW AT END CONTINUE END-READ
           READ RW AT END CONTINUE END-READ
           MOVE "NEW " TO RW-REC
           REWRITE RW-REC
           DISPLAY "R-RW=" ST-W
           CLOSE RW
           READ RR AT END CONTINUE END-READ
           DISPLAY "R-R2=" RR-REC
           READ RR AT END CONTINUE END-READ
           DISPLAY "R-R3=" RR-REC
           CLOSE RR
      *> X - INDEXED, already correct (one shared store)
           OPEN OUTPUT XS
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 300
             MOVE I TO KEYX
             MOVE KEYX TO XS-KEY
             MOVE "OLD " TO XS-VAL
             WRITE XS-REC
           END-PERFORM
           CLOSE XS
           OPEN INPUT XR
           READ XR AT END CONTINUE END-READ
           DISPLAY "X-R1=" XR-REC
           OPEN I-O XW
           READ XW AT END CONTINUE END-READ
           READ XW AT END CONTINUE END-READ
           READ XW AT END CONTINUE END-READ
           MOVE "NEW " TO XW-VAL
           REWRITE XW-REC
           DISPLAY "X-RW=" ST-W
           CLOSE XW
           READ XR AT END CONTINUE END-READ
           DISPLAY "X-R2=" XR-REC
           READ XR AT END CONTINUE END-READ
           DISPLAY "X-R3=" XR-REC
           CLOSE XR
           STOP RUN.
