      *>--- conformance-case
      *> id: l1c25-rewrite-lock-phrase-automatic
      *> rules: [SR-14.9.35.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.35.3 SR4 — WITH NO LOCK on a REWRITE of a file whose
      *> LOCK MODE clause specifies AUTOMATIC.
      *> "If automatic locking has been specified for the rewrite file,
      *> neither the WITH LOCK phrase nor the WITH NO LOCK phrase shall
      *> be specified."
      *> cite.py: OK  §14.9.35.3 4)  (Syntax rules)
      *> cite.py: OK  §12.4.5.9.4 4)  (General rules) - "If the
      *> AUTOMATIC phrase is specified, the lock mode is automatic."
      *> The LOCK MODE clause and the lock phrase are COBOL 2002
      *> facilities, so the rejection applies from 2002 on. The file is
      *> INDEXED with sequential access and a SHARING clause; the only
      *> defect is the lock phrase against automatic locking (the WITH
      *> LOCK spelling of the same violation is covered by the same
      *> check and is not repeated). No APPLY COMMIT clause (SR5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C25N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT X-FILE ASSIGN TO "L1C25N4.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS X-KEY
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC.
       DATA DIVISION.
       FILE SECTION.
       FD X-FILE.
       01 X-REC.
          05 X-KEY  PIC X(4).
          05 X-DATA PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O X-FILE.
           READ X-FILE.
           REWRITE X-REC WITH NO LOCK.
           CLOSE X-FILE.
           STOP RUN.
