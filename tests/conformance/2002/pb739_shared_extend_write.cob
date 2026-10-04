       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB739E02.
      *> kb/Work PB739 at the COBOL-2002 FLOOR - two file connectors
      *> extending ONE shared physical file, interleaved WRITE by WRITE.
      *>
      *> The full matrix is 2023/pb739_shared_extend_write; this is the
      *> subset the edition floor admits, and the split is a fact about
      *> the constructs rather than a convenience: ORGANIZATION IS LINE
      *> SEQUENTIAL is COBOL-2023 (12.4.5.10.3 GR2; kb/Work PB688) while
      *> SHARING and LOCK MODE are COBOL-2002. What survives here is
      *> record sequential FIXED width, record sequential RECORD VARYING,
      *> the LOCK MODE-clause-plus-OPEN-phrase spelling, the exclusive control, and
      *> the RELATIVE and INDEXED organizations.
      *>
      *> THE RULES. 14.9.51.4 GR19: "If two or more file connectors for a
      *> sequential file add records by sharing the physical file after
      *> opening it in extend mode, the added records follow the records
      *> present in the physical file when it was opened, but are
      *> otherwise in an undefined order." GR12: "The successful execution
      *> of a WRITE statement releases a logical record to the operating
      *> environment." Both added records shall be in the file; only their
      *> relative ORDER is undefined, and the order printed below is this
      *> compiler's determinate choice inside that latitude (GR12 releases
      *> each record at its own WRITE, so they land in statement order).
      *> 9.1.15 3) makes the shape legal and 14.9.27.4 Table 19 makes
      *> ALL OTHER/EXTEND against ALL OTHER/EXTEND a Normal open.
      *>
      *> RELATIVE. 14.9.51.4 GR29 a): the extend release is "one greater
      *> than the highest relative record number existing in the physical
      *> file", and "If the physical file is shared and the open mode is
      *> extend, the record numbers are not necessarily consecutive" -
      *> the number is read from the file AT THE RELEASE, so A gets 0002
      *> over a file holding RRN 1 and B gets 0003 once A has released.
      *> GR31 says the same from the other side.
      *>
      *> INDEXED. 14.9.51.4 GR38 measures the extend high key "when it was
      *> opened THROUGH THAT FILE CONNECTOR", so B - opened when the file
      *> held only K002 - may release K004 after A has released K006.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "pb739f2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT FA ASSIGN TO "pb739f2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-A.
           SELECT FB ASSIGN TO "pb739f2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-B.
           SELECT VS ASSIGN TO "pb739v2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT VA ASSIGN TO "pb739v2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-A.
           SELECT VB ASSIGN TO "pb739v2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-B.
      *> LOCK MODE clause and NO SHARING clause in the file control entry.
      *> RE-DERIVED BY kb/Work PB322: a LOCK MODE clause is not a sharing
      *> specification (9.1.15), so without more these two connectors would
      *> hold SHARING WITH NO OTHER (OPEN EXTEND's implementor default) and
      *> the second OPEN would be refused '61'. The OPENs below carry the
      *> SHARING WITH ALL OTHER phrase (14.9.27.4 GR23: the phrase gives the
      *> sharing mode when the entry has none; 14.9.27.3 SR8 is satisfied by
      *> the entry's LOCK MODE clause), so the append discipline still
      *> keys on the posture of a connector whose entry wrote no SHARING.
           SELECT KS ASSIGN TO "pb739k2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT KA ASSIGN TO "pb739k2.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-A.
           SELECT KB ASSIGN TO "pb739k2.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-B.
      *> the control: ONE exclusive connector, TWO writes
           SELECT NS ASSIGN TO "pb739n2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT NA ASSIGN TO "pb739n2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH NO OTHER
               FILE STATUS IS ST-A.
           SELECT RS ASSIGN TO "pb739r2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK-S
               FILE STATUS IS ST-S.
           SELECT RA ASSIGN TO "pb739r2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK-A
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-A.
           SELECT RB ASSIGN TO "pb739r2.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK-B
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-B.
           SELECT XS ASSIGN TO "pb739x2.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XS-KEY
               FILE STATUS IS ST-S.
           SELECT XA ASSIGN TO "pb739x2.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XA-KEY
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-A.
           SELECT XB ASSIGN TO "pb739x2.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XB-KEY
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS ST-B.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 FS-REC PIC X(4).
       FD FA.
       01 FA-REC PIC X(4).
       FD FB.
       01 FB-REC PIC X(4).
       FD VS RECORD IS VARYING IN SIZE FROM 3 TO 8
              DEPENDING ON V-LEN.
       01 VS-REC PIC X(8).
       FD VA RECORD IS VARYING IN SIZE FROM 3 TO 8
              DEPENDING ON V-LEN.
       01 VA-REC PIC X(8).
       FD VB RECORD IS VARYING IN SIZE FROM 3 TO 8
              DEPENDING ON V-LEN.
       01 VB-REC PIC X(8).
       FD KS.
       01 KS-REC PIC X(4).
       FD KA.
       01 KA-REC PIC X(4).
       FD KB.
       01 KB-REC PIC X(4).
       FD NS.
       01 NS-REC PIC X(4).
       FD NA.
       01 NA-REC PIC X(4).
       FD RS.
       01 RS-REC PIC X(4).
       FD RA.
       01 RA-REC PIC X(4).
       FD RB.
       01 RB-REC PIC X(4).
       FD XS.
       01 XS-REC.
          05 XS-KEY PIC X(4).
          05 XS-VAL PIC X(4).
       FD XA.
       01 XA-REC.
          05 XA-KEY PIC X(4).
          05 XA-VAL PIC X(4).
       FD XB.
       01 XB-REC.
          05 XB-KEY PIC X(4).
          05 XB-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST-S PIC XX.
       01 ST-A PIC XX.
       01 ST-B PIC XX.
       01 RK-S PIC 9(4).
       01 RK-A PIC 9(4).
       01 RK-B PIC 9(4).
       01 V-LEN PIC 9(4).
       01 N     PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM FIXED-CASE
           PERFORM VARY-CASE
           PERFORM LOCKMODE-CASE
           PERFORM EXCLUSIVE-CASE
           PERFORM RELATIVE-CASE
           PERFORM INDEXED-CASE
           STOP RUN.

       FIXED-CASE.
           OPEN OUTPUT FS
           MOVE "SEED" TO FS-REC
           WRITE FS-REC
           CLOSE FS
           OPEN EXTEND FA
           OPEN EXTEND FB
           DISPLAY "F-EXT A=" ST-A " B=" ST-B
           MOVE "AAAA" TO FA-REC
           WRITE FA-REC
           MOVE "BBBB" TO FB-REC
           WRITE FB-REC
           DISPLAY "F-W A=" ST-A " B=" ST-B
           CLOSE FA
           CLOSE FB
           MOVE 0 TO N
           OPEN INPUT FS
           PERFORM UNTIL 1 = 2
             READ FS AT END EXIT PERFORM END-READ
             ADD 1 TO N
             DISPLAY "F-R" N "=" FS-REC
           END-PERFORM
           CLOSE FS
           DISPLAY "F-COUNT=" N.

       VARY-CASE.
           OPEN OUTPUT VS
           MOVE "SEED" TO VS-REC
           MOVE 4 TO V-LEN
           WRITE VS-REC
           CLOSE VS
           OPEN EXTEND VA
           OPEN EXTEND VB
           DISPLAY "V-EXT A=" ST-A " B=" ST-B
           MOVE "AAAA" TO VA-REC
           MOVE 4 TO V-LEN
           WRITE VA-REC
           MOVE "BBBBB" TO VB-REC
           MOVE 5 TO V-LEN
           WRITE VB-REC
           DISPLAY "V-W A=" ST-A " B=" ST-B
           CLOSE VA
           CLOSE VB
           MOVE 0 TO N
           OPEN INPUT VS
           PERFORM UNTIL 1 = 2
             READ VS AT END EXIT PERFORM END-READ
             ADD 1 TO N
             DISPLAY "V-R" N "=" V-LEN " " VS-REC
           END-PERFORM
           CLOSE VS
           DISPLAY "V-COUNT=" N.

       LOCKMODE-CASE.
           OPEN OUTPUT KS
           MOVE "SEED" TO KS-REC
           WRITE KS-REC
           CLOSE KS
           OPEN EXTEND SHARING WITH ALL OTHER KA
           OPEN EXTEND SHARING WITH ALL OTHER KB
           DISPLAY "K-EXT A=" ST-A " B=" ST-B
           MOVE "AAAA" TO KA-REC
           WRITE KA-REC
           MOVE "BBBB" TO KB-REC
           WRITE KB-REC
           DISPLAY "K-W A=" ST-A " B=" ST-B
           CLOSE KA
           CLOSE KB
           MOVE 0 TO N
           OPEN INPUT KS
           PERFORM UNTIL 1 = 2
             READ KS AT END EXIT PERFORM END-READ
             ADD 1 TO N
             DISPLAY "K-R" N "=" KS-REC
           END-PERFORM
           CLOSE KS
           DISPLAY "K-COUNT=" N.

       EXCLUSIVE-CASE.
           OPEN OUTPUT NS
           MOVE "SEED" TO NS-REC
           WRITE NS-REC
           CLOSE NS
           OPEN EXTEND NA
           DISPLAY "N-EXT=" ST-A
           MOVE "CCCC" TO NA-REC
           WRITE NA-REC
           MOVE "DDDD" TO NA-REC
           WRITE NA-REC
           DISPLAY "N-W=" ST-A
           CLOSE NA
           MOVE 0 TO N
           OPEN INPUT NS
           PERFORM UNTIL 1 = 2
             READ NS AT END EXIT PERFORM END-READ
             ADD 1 TO N
             DISPLAY "N-R" N "=" NS-REC
           END-PERFORM
           CLOSE NS
           DISPLAY "N-COUNT=" N.

       RELATIVE-CASE.
           OPEN OUTPUT RS
           MOVE "SEED" TO RS-REC
           WRITE RS-REC
           CLOSE RS
           OPEN EXTEND RA
           OPEN EXTEND RB
           DISPLAY "R-EXT A=" ST-A " B=" ST-B
           MOVE "AAAA" TO RA-REC
           WRITE RA-REC
           MOVE "BBBB" TO RB-REC
           WRITE RB-REC
           DISPLAY "R-W A=" ST-A " B=" ST-B
           DISPLAY "R-KA=" RK-A " R-KB=" RK-B
           CLOSE RA
           CLOSE RB
           MOVE 0 TO N
           OPEN INPUT RS
           PERFORM UNTIL 1 = 2
             READ RS AT END EXIT PERFORM END-READ
             ADD 1 TO N
             DISPLAY "R-R" N "=" RK-S " " RS-REC
           END-PERFORM
           CLOSE RS
           DISPLAY "R-COUNT=" N.

       INDEXED-CASE.
           OPEN OUTPUT XS
           MOVE "K002" TO XS-KEY
           MOVE "V002" TO XS-VAL
           WRITE XS-REC
           CLOSE XS
           OPEN EXTEND XA
           OPEN EXTEND XB
           DISPLAY "X-EXT A=" ST-A " B=" ST-B
           MOVE "K006" TO XA-KEY
           MOVE "AAAA" TO XA-VAL
           WRITE XA-REC
           MOVE "K004" TO XB-KEY
           MOVE "BBBB" TO XB-VAL
           WRITE XB-REC
           DISPLAY "X-W A=" ST-A " B=" ST-B
           CLOSE XA
           CLOSE XB
           MOVE 0 TO N
           OPEN INPUT XS
           PERFORM UNTIL 1 = 2
             READ XS AT END EXIT PERFORM END-READ
             ADD 1 TO N
             DISPLAY "X-R" N "=" XS-REC
           END-PERFORM
           CLOSE XS
           DISPLAY "X-COUNT=" N.
