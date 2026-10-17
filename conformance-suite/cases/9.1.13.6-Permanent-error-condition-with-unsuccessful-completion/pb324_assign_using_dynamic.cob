*>--- conformance-case
*> id: pb324_assign_using_dynamic
*> rules: [GR-9.1.13.6-2, SR-12.4.5.2-6, SR-12.4.5.2-7, GR-12.4.5.3-3,
*>    GR-12.4.5.3-4, GR-14.9.27.4-26, DOC-A.1-10, DOC-A.1-73]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §14.9.27.4 GR26 -> §12.4.5.3 GR3/GR4 - ASSIGN ... USING (dynamic file assignment, §9.1.21).
      *> GR3 b): "When the USING phrase of the ASSIGN clause is specified, the file connector referenced by
      *> file-name-1 is associated with a physical file identified by the content of the data item referenced by
      *> data-name-1 in the runtime element that executes the OPEN, SORT, or MERGE statement." GR3's lead sentence
      *> fixes the timing: "The association occurs at the time of execution of an OPEN, SORT, or MERGE statement
      *> that referenced file-name-1." So ONE connector reaches TWO physical files in one run unit, and the file
      *> named by the file-name (dynf.txt) is never written at all - the CHK connector proves it (kb/Work PB324).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB324DY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DYNF ASSIGN USING WS-NAME
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-ST.
           SELECT OPTIONAL CHK ASSIGN TO "dynf.txt"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-CK.
      *> GR3 b) applies "when the USING phrase of the ASSIGN clause is specified" with NO condition on the TO
      *> phrase, so where both are written the USING content wins and literal-1 is never used. §12.4.5.3 GR4
      *> leaves any consistency rule between the two to the implementor, and COBOL.NET defines none
      *> (docs/CONFORMANCE.md §7, DOC-A.1-10): an unrelated content is accepted, not diagnosed.
           SELECT MIX ASSIGN TO "pb324mix.dat" USING WS-NAME
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-MX.
           SELECT OPTIONAL CHK2 ASSIGN TO "pb324mix.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-CK.
       DATA DIVISION.
       FILE SECTION.
       FD  DYNF.
       01  DYN-REC PIC X(5).
       FD  CHK.
       01  CHK-REC PIC X(5).
       FD  MIX.
       01  MIX-REC PIC X(5).
       FD  CHK2.
       01  CHK2-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(20) VALUE SPACES.
       01  WS-ST   PIC XX.
       01  WS-CK   PIC XX.
       01  WS-MX   PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> An all-space content names no physical file, so the association cannot be made: §12.4.5.3 GR3's closing
      *> sentence makes the OPEN unsuccessful and §9.1.13.6 item 2 gives that failure its own status, '31'.
           OPEN OUTPUT DYNF.
           DISPLAY "BLANK=" WS-ST.
           MOVE "pb324a.dat" TO WS-NAME.
           OPEN OUTPUT DYNF.
           DISPLAY "OPENA=" WS-ST.
           MOVE "AAAAA" TO DYN-REC.
           WRITE DYN-REC.
           CLOSE DYNF.
           MOVE "pb324b.dat" TO WS-NAME.
           OPEN OUTPUT DYNF.
           DISPLAY "OPENB=" WS-ST.
           MOVE "BBBBB" TO DYN-REC.
           WRITE DYN-REC.
           CLOSE DYNF.
      *> Table 18, "INPUT (optional file) / File is unavailable": normal open, status '05'. dynf.txt - the path a
      *> file-name-derived assign target would have produced - was never created.
           OPEN INPUT CHK.
           DISPLAY "CHK=" WS-CK.
           CLOSE CHK.
      *> The association is re-made at EVERY OPEN, so the same connector reads back each file in turn.
           MOVE "pb324a.dat" TO WS-NAME.
           OPEN INPUT DYNF.
           READ DYNF.
           DISPLAY "READA=" DYN-REC.
           CLOSE DYNF.
           MOVE "pb324b.dat" TO WS-NAME.
           OPEN INPUT DYNF.
           READ DYNF.
           DISPLAY "READB=" DYN-REC.
           CLOSE DYNF.
      *> Both phrases written: WS-NAME still decides, and "pb324mix.dat" is never touched.
           MOVE "pb324e.dat" TO WS-NAME.
           OPEN OUTPUT MIX.
           DISPLAY "OPENM=" WS-MX.
           MOVE "EEEEE" TO MIX-REC.
           WRITE MIX-REC.
           CLOSE MIX.
           OPEN INPUT CHK2.
           DISPLAY "CHK2=" WS-CK.
           CLOSE CHK2.
           MOVE "pb324e.dat" TO WS-NAME.
           OPEN INPUT MIX.
           READ MIX.
           DISPLAY "READE=" MIX-REC.
           CLOSE MIX.
      *> DELETE FILE acts on the association the last OPEN established (§14.9.10.4 GR20 a) - pb324b.dat.
           DELETE FILE DYNF.
           DISPLAY "DELB=" WS-ST.
           STOP RUN.
