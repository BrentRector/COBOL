      *>--- conformance-case
      *> id: pb336_optional_absent_read_46_85
      *> rules: [GR-9.1.13.4-1, GR-14.9.30.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.30.4 GR21 first sentence -- "For a sequential READ statement, if the
      *> previous READ or START statement for the file connector was unsuccessful, then
      *> the READ statement is unsuccessful and the I-O status is set to '46'" -- against
      *> 9.1.13.4 item 1 c), which scopes '10' to a sequential READ "attempted FOR THE
      *> FIRST TIME on a file described as optional and the physical file is not present".
      *> The '10' arm is itself an unsuccessful READ, so GR21 governs every READ after it.
      *> kb/Work PB336. This is the 1985 twin of tests/conformance/2023/pb336_optional_
      *> absent_read_46.cob: the rule is version-invariant (no edition marker on 14.9.30.4
      *> GR21, on 9.1.13.4 or on 9.1.13.5, and Annex E lists no change to any of them), and
      *> the runtime connectors carry no edition gating, so the OLDEST supported edition is
      *> where a regression would show first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB33685.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL SQA ASSIGN TO "pb33685a.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT SQP ASSIGN TO "pb33685p.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS PS.
           SELECT OPTIONAL RLA ASSIGN TO "pb33685r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS RS.
           SELECT OPTIONAL IXA ASSIGN TO "pb33685x.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XK
               FILE STATUS IS XS.
       DATA DIVISION.
       FILE SECTION.
       FD SQA.
       01 SQA-REC PIC X(10).
       FD SQP.
       01 SQP-REC PIC X(10).
       FD RLA.
       01 RLA-REC PIC X(10).
       FD IXA.
       01 IXA-REC.
          05 XK PIC X(4).
          05 FILLER PIC X(6).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 PS PIC XX.
       01 RS PIC XX.
       01 XS PIC XX.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
      *> (1) SEQUENTIAL organization, absent OPTIONAL: 05 / 10 / 46 / 46.
           OPEN INPUT SQA
           DISPLAY "SEQ-ABSENT OPEN=" FS
           READ SQA AT END CONTINUE END-READ
           DISPLAY "SEQ-ABSENT R1=" FS
           READ SQA AT END CONTINUE END-READ
           DISPLAY "SEQ-ABSENT R2=" FS
           READ SQA AT END CONTINUE END-READ
           DISPLAY "SEQ-ABSENT R3=" FS
           CLOSE SQA
      *> (2) THE CONTRAST ARM, in the same guard chain: a PRESENT one-record file walks
      *> 00 / 10 / 46 -- ordinary end-of-file, then GR21. It already did; the absent
      *> OPTIONAL arm above is the one that escaped GR21, so both are pinned here.
           OPEN OUTPUT SQP
           MOVE "ONERECORD" TO SQP-REC
           WRITE SQP-REC
           CLOSE SQP
           OPEN INPUT SQP
           DISPLAY "SEQ-PRESENT OPEN=" PS
           READ SQP AT END CONTINUE END-READ
           DISPLAY "SEQ-PRESENT R1=" PS
           READ SQP AT END CONTINUE END-READ
           DISPLAY "SEQ-PRESENT R2=" PS
           READ SQP AT END CONTINUE END-READ
           DISPLAY "SEQ-PRESENT R3=" PS
           CLOSE SQP
      *> (3) RELATIVE, absent OPTIONAL: the same 05 / 10 / 46.
           OPEN INPUT RLA
           DISPLAY "REL-ABSENT OPEN=" RS
           READ RLA NEXT AT END CONTINUE END-READ
           DISPLAY "REL-ABSENT R1=" RS
           READ RLA NEXT AT END CONTINUE END-READ
           DISPLAY "REL-ABSENT R2=" RS
           CLOSE RLA
      *> (4) INDEXED, absent OPTIONAL: the same 05 / 10 / 46.
           OPEN INPUT IXA
           DISPLAY "IDX-ABSENT OPEN=" XS
           READ IXA NEXT AT END CONTINUE END-READ
           DISPLAY "IDX-ABSENT R1=" XS
           READ IXA NEXT AT END CONTINUE END-READ
           DISPLAY "IDX-ABSENT R2=" XS
           CLOSE IXA
      *> (5) GR21's OTHER antecedent -- an unsuccessful START (9.1.13.5 item 3 b) gives
      *> '23' on an absent optional file) also makes the NEXT sequential READ '46'
      *> (9.1.13.7 item 6 a), not the first-time '10'.
           OPEN INPUT RLA
           MOVE 1 TO RK
           START RLA KEY IS EQUAL TO RK INVALID KEY CONTINUE END-START
           DISPLAY "REL-START=" RS
           READ RLA NEXT AT END CONTINUE END-READ
           DISPLAY "REL-AFTER-START=" RS
           CLOSE RLA
           OPEN INPUT IXA
           MOVE "AAAA" TO XK
           START IXA KEY IS EQUAL TO XK INVALID KEY CONTINUE END-START
           DISPLAY "IDX-START=" XS
           READ IXA NEXT AT END CONTINUE END-READ
           DISPLAY "IDX-AFTER-START=" XS
           CLOSE IXA
      *> (6) A RANDOM READ is 9.1.13.5 item 3 b)'s '23' with NO "first time" qualifier
      *> and never GR21's '46' ("For a sequential READ statement"); but it is still an
      *> unsuccessful READ, so the following SEQUENTIAL read is '46' (9.1.13.7 item 6 b).
           OPEN INPUT RLA
           MOVE 1 TO RK
           READ RLA INVALID KEY CONTINUE END-READ
           DISPLAY "REL-RANDOM1=" RS
           READ RLA INVALID KEY CONTINUE END-READ
           DISPLAY "REL-RANDOM2=" RS
           READ RLA NEXT AT END CONTINUE END-READ
           DISPLAY "REL-AFTER-RANDOM=" RS
           CLOSE RLA
           OPEN INPUT IXA
           MOVE "AAAA" TO XK
           READ IXA INVALID KEY CONTINUE END-READ
           DISPLAY "IDX-RANDOM1=" XS
           READ IXA INVALID KEY CONTINUE END-READ
           DISPLAY "IDX-RANDOM2=" XS
           READ IXA NEXT AT END CONTINUE END-READ
           DISPLAY "IDX-AFTER-RANDOM=" XS
           CLOSE IXA
           STOP RUN.
