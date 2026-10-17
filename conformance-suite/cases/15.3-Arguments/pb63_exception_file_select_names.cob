*>--- conformance-case
*> id: pb63_exception_file_select_names
*> rules: [AR-15.3-4, AR-15.28.3-1, RV-15.28.4-1, RV-15.28.4-2, AR-15.29.3-1, RV-15.29.4-1, RV-15.29.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB63 - FUNCTION EXCEPTION-FILE / EXCEPTION-FILE-N: the SELECT-spelled name and the "attempted access"
      *> rule. 15.28.4 r2 (the file-connector-name form): a) two alphanumeric spaces while the connector "has
      *> never been opened, attempted to be opened, or otherwise attempted to be accessed"; b) otherwise the
      *> two-character I-O status followed by "the file-name exactly as specified in the SELECT clause"
      *> (r1c/r2b). 9.1.13.1 names CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK and WRITE as the
      *> statements that set the I-O status, so a CLOSE on a never-opened connector (status 42) or a READ on
      *> one (47) IS an attempted access and r2b governs. 15.29.4 r1c/r2b are the national twins.
      *> ARG/ARGN: an EXTERNAL file SELECTed as MixedExtF, read to end-of-file: 10MixedExtF (11 positions,
      *>   the -N result measured through DISPLAY-OF and LENGTH). Before PB63 the display name was recovered
      *>   from the registry KEY (the uppercased external name: 10MIXEDEXTF).
      *> EFC/EFR/EFU: CLOSE Closed -> 42Closed; READ Readf -> 47Readf; Untouch is genuinely never accessed ->
      *>   two spaces (before PB63 only OPEN and DELETE FILE recorded an access, so EFC/EFR were two spaces).
      *> NOARG: the no-argument form after the READ at end-of-file under EC-I-O checking: 10MixedExtF (r1c).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB63EXCFILE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MixedExtF ASSIGN TO "pb63f1e.dat"
               ORGANIZATION IS LINE SEQUENTIAL FILE STATUS IS FS.
           SELECT Closed ASSIGN TO "pb63c.dat" FILE STATUS IS FS1.
           SELECT Readf ASSIGN TO "pb63r.dat" FILE STATUS IS FS2.
           SELECT Untouch ASSIGN TO "pb63u.dat" FILE STATUS IS FS3.
       DATA DIVISION.
       FILE SECTION.
       FD MixedExtF IS EXTERNAL.
       01 MREC PIC X(10).
       FD Closed.
       01 CREC PIC X(5).
       FD Readf.
       01 RREC PIC X(5).
       FD Untouch.
       01 UREC PIC X(5).
       WORKING-STORAGE SECTION.
       01 FS PIC XX EXTERNAL.
       01 FS1 PIC XX.
       01 FS2 PIC XX.
       01 FS3 PIC XX.
       01 L PIC 9(3).
       PROCEDURE DIVISION.
           OPEN OUTPUT MixedExtF
           MOVE "hello" TO MREC WRITE MREC
           CLOSE MixedExtF
           OPEN INPUT MixedExtF
           READ MixedExtF AT END CONTINUE END-READ
           READ MixedExtF AT END CONTINUE END-READ
           DISPLAY "FS=[" FS "]"
           DISPLAY "ARG=[" FUNCTION EXCEPTION-FILE(MixedExtF) "]"
           COMPUTE L = FUNCTION LENGTH(FUNCTION EXCEPTION-FILE-N(MixedExtF))
           DISPLAY "ARGN=[" FUNCTION DISPLAY-OF(FUNCTION EXCEPTION-FILE-N(MixedExtF)) "] " L
           CLOSE MixedExtF
           CLOSE Closed
           READ Readf AT END CONTINUE END-READ
           DISPLAY "FS1=[" FS1 "] FS2=[" FS2 "]"
           DISPLAY "EFC=[" FUNCTION EXCEPTION-FILE(Closed) "]"
           DISPLAY "EFR=[" FUNCTION EXCEPTION-FILE(Readf) "]"
           DISPLAY "EFU=[" FUNCTION EXCEPTION-FILE(Untouch) "]"
           DISPLAY "NFC=[" FUNCTION DISPLAY-OF(FUNCTION EXCEPTION-FILE-N(Closed)) "]"
           DISPLAY "NFU=[" FUNCTION DISPLAY-OF(FUNCTION EXCEPTION-FILE-N(Untouch)) "]"
           >>TURN EC-I-O CHECKING ON
           OPEN INPUT MixedExtF
           READ MixedExtF AT END CONTINUE END-READ
           READ MixedExtF AT END CONTINUE END-READ
           DISPLAY "NOARG=[" FUNCTION EXCEPTION-FILE "]"
           DISPLAY "NOARGN=[" FUNCTION DISPLAY-OF(FUNCTION EXCEPTION-FILE-N) "]"
           CLOSE MixedExtF
           STOP RUN.
       END PROGRAM PB63EXCFILE.
