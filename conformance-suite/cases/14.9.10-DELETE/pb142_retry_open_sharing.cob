*>--- conformance-case
*> id: pb142_retry_open_sharing
*> rules: [GR-14.9.10.4-6, GR-14.9.27.4-24, DOC-A.1-109]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB142ROS.
      *> kb/Work PB142 - the OPEN arm of the RETRY conflict-status class
      *> rule. ISO 14.9.27.4 GR24 routes an OPEN sharing conflict's status
      *> through 14.7.9, whose GR4a and closing paragraph both land "the
      *> appropriate value ... according to the rules for 9.1.13".
      *> 9.1.13.9 item 1 defines that value as '61' and defines NO deadlock
      *> value for a file sharing conflict, so EVERY retry form answers 61.
      *> The compiler used to manufacture '52' (9.1.13.8's RECORD-conflict
      *> deadlock) on the SECONDS/FOREVER arm; nothing in the suite wrote
      *> OPEN with a RETRY phrase, so that arm was silently wrong.
      *>
      *> The OPN35* legs guard the rule's OTHER edge: a status that is not
      *> a conflict at all. 14.7.9.3 GR4 opens "if the I/O operation is
      *> unsuccessful on the first attempt because of a file sharing
      *> conflict condition or a record operation conflict condition", so
      *> an absent file's 35 is the statement's own answer and RETRY must
      *> not touch it. It used to answer 52 as well.
      *>
      *> The DELIBERATE asymmetry -- a RECORD operation conflict under
      *> FOREVER KEEPS 52, because 9.1.13.8 item 2 is a record-conflict
      *> value whose detection conditions the implementor defines (A.1
      *> item 109), where 9.1.13.9 defines none -- is pinned end-to-end by
      *> file_sharing_seq (READD5=52) and file_sharing_mutate (REWQFV=52),
      *> and cell by cell by the unit drift test
      *> CobolFileLockTests.RetryLoop_LandsTheConflictsOwnStatus_ByClass.
      *> Those must STAY GREEN: this fix is class-scoped, not a blanket
      *> removal of 52.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-A ASSIGN TO "pb142ros.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC
               FILE STATUS IS A-ST.
           SELECT F-B ASSIGN TO "pb142ros.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC
               FILE STATUS IS B-ST.
           SELECT F-M ASSIGN TO "pb142rosm.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC
               FILE STATUS IS M-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-A.
       01 A-REC PIC X(5).
       FD F-B.
       01 B-REC PIC X(5).
       FD F-M.
       01 M-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 A-ST PIC XX.
       01 B-ST PIC XX.
       01 M-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F-A.
           MOVE "HELLO" TO A-REC. WRITE A-REC.
      *> F-A holds the physical file open and Table-19 registered. Every
      *> F-B open below asks for SHARING WITH NO OTHER, which is 9.1.13.9
      *> item 1 sub-case b) -- a conflict that no retry can clear, since
      *> the holder is a connector of this same run unit.
           OPEN INPUT SHARING WITH NO OTHER F-B.
           DISPLAY "OPNNONE=" B-ST.
           OPEN INPUT SHARING WITH NO OTHER RETRY 2 TIMES F-B.
           DISPLAY "OPNTIMES=" B-ST.
           OPEN INPUT SHARING WITH NO OTHER RETRY FOREVER F-B.
           DISPLAY "OPNFOREVER=" B-ST.
           OPEN INPUT SHARING WITH NO OTHER RETRY FOR 0 SECONDS F-B.
           DISPLAY "OPNSEC0=" B-ST.
           OPEN INPUT SHARING WITH NO OTHER RETRY FOR 30 SECONDS F-B.
           DISPLAY "OPNSEC30=" B-ST.
           CLOSE F-A.
      *> The NOT-A-CONFLICT leg: F-M's physical file was never created, so
      *> OPEN INPUT is 35 under every retry form (14.7.9.3 GR4).
           OPEN INPUT F-M. DISPLAY "OPN35NONE=" M-ST.
           OPEN INPUT RETRY 2 TIMES F-M. DISPLAY "OPN35TIMES=" M-ST.
           OPEN INPUT RETRY FOREVER F-M. DISPLAY "OPN35FOREVER=" M-ST.
           OPEN INPUT RETRY FOR 30 SECONDS F-M. DISPLAY "OPN35SEC=" M-ST.
           STOP RUN.
