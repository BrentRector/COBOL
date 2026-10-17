      *>--- conformance-case
      *> id: l1_open_optional_create
      *> rules: [GR-9.1.13.2-4, GR-9.1.13.4-1, GR-14.9.27.4-17]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.27.4 GR17 — "If the file is not present, and the
      *> EXTEND or I-O phrase is specified in the OPEN statement, and
      *> the OPTIONAL clause is specified in the file control entry for
      *> file-name-1, the OPEN statement creates the file. This
      *> creation takes place as if the following statements were
      *> executed in the order shown: OPEN OUTPUT file-name-1. CLOSE
      *> file-name-1. These statements are followed by execution of the
      *> OPEN statement specified in the source element and the I-O
      *> status value associated with file-name-1 is set to '05'."
      *> (The clause's last three characters are an OCR artifact in the
      *> transcription; §9.1.13.2 item 4a supplies the value outright —
      *> "If the open mode is I-O or extend, the physical file has been
      *> created".)
      *> The rule has SIX arms — three organizations x {EXTEND, I-O} —
      *> and all six are exercised, none inferred from another.
      *> Per arm, all three lines are stated by the rule:
      *>   -OPEN   '05' (§9.1.13.2 item 4a).
      *>   -REOPEN a plain OPEN INPUT of the same file-name now reports
      *>           '00'. §9.1.13.2 item 4a's '05' is defined by "the
      *>           physical file is not present"; a '00' here is only
      *>           possible because the earlier OPEN CREATED the file.
      *>           This is the line that separates GR17 from GR13,
      *>           where the INPUT phrase creates nothing.
      *>   -READ   '10'. The creation is "as if OPEN OUTPUT … CLOSE",
      *>           and §14.9.27.4 GR18 says that leaves a file which
      *>           "contains no records" — so the first sequential READ
      *>           is the at end condition (§9.1.13.4 item 1a).
      *> §14.9.27.3 SR2 confines EXTEND to sequential access mode, so
      *> the keyed connectors below are ACCESS SEQUENTIAL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OPN17.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL QE ASSIGN TO "l1opn17qe.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-QE.
           SELECT OPTIONAL QI ASSIGN TO "l1opn17qi.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-QI.
           SELECT OPTIONAL RE ASSIGN TO "l1opn17re.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-RE.
           SELECT OPTIONAL RI ASSIGN TO "l1opn17ri.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-RI.
           SELECT OPTIONAL XE ASSIGN TO "l1opn17xe.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XE-KEY
               FILE STATUS IS ST-XE.
           SELECT OPTIONAL XI ASSIGN TO "l1opn17xi.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XI-KEY
               FILE STATUS IS ST-XI.
       DATA DIVISION.
       FILE SECTION.
       FD QE.
       01 QE-REC PIC X(6).
       FD QI.
       01 QI-REC PIC X(6).
       FD RE.
       01 RE-REC PIC X(6).
       FD RI.
       01 RI-REC PIC X(6).
       FD XE.
       01 XE-REC.
          05 XE-KEY PIC X(3).
          05 XE-VAL PIC X(3).
       FD XI.
       01 XI-REC.
          05 XI-KEY PIC X(3).
          05 XI-VAL PIC X(3).
       WORKING-STORAGE SECTION.
       01 ST-QE PIC XX.
       01 ST-QI PIC XX.
       01 ST-RE PIC XX.
       01 ST-RI PIC XX.
       01 ST-XE PIC XX.
       01 ST-XI PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> ---- sequential organization, EXTEND ---------------------
           OPEN EXTEND QE
           DISPLAY "QE-OPEN=" ST-QE
           CLOSE QE
           OPEN INPUT QE
           DISPLAY "QE-REOPEN=" ST-QE
           READ QE AT END CONTINUE END-READ
           DISPLAY "QE-READ=" ST-QE
           CLOSE QE
      *> ---- sequential organization, I-O ------------------------
           OPEN I-O QI
           DISPLAY "QI-OPEN=" ST-QI
           CLOSE QI
           OPEN INPUT QI
           DISPLAY "QI-REOPEN=" ST-QI
           READ QI AT END CONTINUE END-READ
           DISPLAY "QI-READ=" ST-QI
           CLOSE QI
      *> ---- relative organization, EXTEND -----------------------
           OPEN EXTEND RE
           DISPLAY "RE-OPEN=" ST-RE
           CLOSE RE
           OPEN INPUT RE
           DISPLAY "RE-REOPEN=" ST-RE
           READ RE AT END CONTINUE END-READ
           DISPLAY "RE-READ=" ST-RE
           CLOSE RE
      *> ---- relative organization, I-O --------------------------
           OPEN I-O RI
           DISPLAY "RI-OPEN=" ST-RI
           CLOSE RI
           OPEN INPUT RI
           DISPLAY "RI-REOPEN=" ST-RI
           READ RI AT END CONTINUE END-READ
           DISPLAY "RI-READ=" ST-RI
           CLOSE RI
      *> ---- indexed organization, EXTEND ------------------------
           OPEN EXTEND XE
           DISPLAY "XE-OPEN=" ST-XE
           CLOSE XE
           OPEN INPUT XE
           DISPLAY "XE-REOPEN=" ST-XE
           READ XE AT END CONTINUE END-READ
           DISPLAY "XE-READ=" ST-XE
           CLOSE XE
      *> ---- indexed organization, I-O ---------------------------
           OPEN I-O XI
           DISPLAY "XI-OPEN=" ST-XI
           CLOSE XI
           OPEN INPUT XI
           DISPLAY "XI-REOPEN=" ST-XI
           READ XI AT END CONTINUE END-READ
           DISPLAY "XI-READ=" ST-XI
           CLOSE XI
           STOP RUN.
