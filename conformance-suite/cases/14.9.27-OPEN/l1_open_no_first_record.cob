      *>--- conformance-case
      *> id: l1_open_no_first_record
      *> rules: [GR-14.9.27.4-9]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.27.4 GR9 — "Execution of the OPEN statement does not
      *> obtain or release the first record."
      *> Two observable halves, one per verb of the rule.
      *> OBTAIN. FA holds REC001, REC002. If the OPEN had obtained the
      *> first record, the first READ after it would deliver REC002.
      *> It delivers REC001, and the second READ delivers REC002, so
      *> the OPEN consumed nothing. The third READ is the at end
      *> condition, '10' (§9.1.13.4 item 1a) — the file really held two
      *> records, not three.
      *> RELEASE, on an EMPTY file. FB is opened OUTPUT and closed with
      *> no WRITE between. §14.9.27.4 GR18: "After the creation of the
      *> file, the file contains no records" — the OPEN released none
      *> from the record area — so the first READ after re-opening it
      *> is immediately at end, '10'.
      *> RELEASE, on a NON-empty file. FA is opened EXTEND and closed
      *> with no WRITE between. Had that OPEN released a record, FA
      *> would now hold three; it still holds exactly REC001, REC002
      *> and then at end.
      *> Every OPEN below is on an available file or creates one, so
      *> each reports '00' (Table 18 + §9.1.13.2 item 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OPN09.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "l1opn09a.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-A.
           SELECT FB ASSIGN TO "l1opn09b.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
       DATA DIVISION.
       FILE SECTION.
       FD FA.
       01 A-REC PIC X(6).
       FD FB.
       01 B-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01 ST-A PIC XX.
       01 ST-B PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FA
           MOVE "REC001" TO A-REC
           WRITE A-REC
           MOVE "REC002" TO A-REC
           WRITE A-REC
           CLOSE FA
      *> GR9, "obtain".
           OPEN INPUT FA
           DISPLAY "OPEN=" ST-A
           READ FA AT END CONTINUE END-READ
           DISPLAY "FIRST=" A-REC
           READ FA AT END CONTINUE END-READ
           DISPLAY "SECOND=" A-REC
           READ FA AT END CONTINUE END-READ
           DISPLAY "AFTER=" ST-A
           CLOSE FA
      *> GR9, "release", on an empty file.
           OPEN OUTPUT FB
           DISPLAY "BOPEN=" ST-B
           CLOSE FB
           OPEN INPUT FB
           DISPLAY "BIN=" ST-B
           READ FB AT END CONTINUE END-READ
           DISPLAY "BREAD=" ST-B
           CLOSE FB
      *> GR9, "release", on a non-empty file.
           OPEN EXTEND FA
           DISPLAY "EXT=" ST-A
           CLOSE FA
           OPEN INPUT FA
           READ FA AT END CONTINUE END-READ
           DISPLAY "E1=" A-REC
           READ FA AT END CONTINUE END-READ
           DISPLAY "E2=" A-REC
           READ FA AT END CONTINUE END-READ
           DISPLAY "E3=" ST-A
           CLOSE FA
           STOP RUN.
