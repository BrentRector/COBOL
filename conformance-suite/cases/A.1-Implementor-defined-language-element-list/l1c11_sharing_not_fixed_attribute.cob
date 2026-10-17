      *>--- conformance-case
      *> id: l1c11_sharing_not_fixed_attribute
      *> rules: [DOC-A.1-78]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.1.6 (A.1 item 78) — whether the ability to share a
      *>   physical file is a fixed file attribute: documented "No"
      *> Rule: "The implementor shall specify whether the ability to
      *>   share a physical file is a fixed file attribute."
      *>   cite.py --check 9.1.6 "The implementor shall specify whether
      *>   the ability to share a physical file is a fixed file
      *>   attribute" -> OK  §9.1.6   (Fixed file attributes)
      *>   cite.py --check 9.1.15 "The sharing with all other mode
      *>   allows concurrent access to a physical file through other
      *>   file connectors specifying input, I-O, or extend mode"
      *>   -> OK  §9.1.15 3)  (Sharing mode)
      *>   cite.py --check 9.1.15 "Associating this file connector with
      *>   the physical file will be unsuccessful if the physical file
      *>   is currently open through other file connectors"
      *>   -> OK  §9.1.15 1)  (Sharing mode)
      *>   cite.py --check 9.1.13.9 "I-O status = 61"
      *>   -> OK  §9.1.13.9 1)
      *>   cite.py --check 14.9.27.3 "the LOCK MODE clause shall be
      *>   specified in the file control entry for file-name-1"
      *>   -> OK  §14.9.27.3 8)  (why the ALL OTHER entries carry one)
      *> Documented choice (docs/CONFORMANCE.md DOC-A.1-78): NO. The
      *> sharing mode belongs to each file connector (its SHARING
      *> clause or the OPEN's SHARING phrase), is checked against the
      *> connectors open at that moment, and is gone at CLOSE; the
      *> physical file records nothing about sharing.
      *> F-NO: SHARING WITH NO OTHER; F-A1, F-A2: SHARING WITH ALL
      *> OTHER; all three name the same physical file.
      *> Derivation:
      *>   NO-CREATE=00    F-NO creates the file (OPEN OUTPUT), writes,
      *>                   closes. Were NO OTHER fixed in the file,
      *>                   the next two OPENs would fail.
      *>   A1=00 A2=00     both ALL OTHER connectors open it INPUT at
      *>                   once (rule 3).
      *>   NO-WHILE=61     NO OTHER while others have it open (rule 1).
      *>   NO-ALONE=00     after both CLOSE, NO OTHER opens alone.
      *>   A1-WHILE=61     an ALL OTHER OPEN while the NO OTHER
      *>                   connector has it open (rule 1).
      *>   A1-CREATE=00    F-A1 re-creates the file (OPEN OUTPUT),
      *>                   writes, closes.
      *>   NO-AFTER=00     F-NO still opens it: ALL OTHER was not kept
      *>                   by the file either.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11M.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-NO ASSIGN TO "L1C11M.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS NO-ST
               SHARING WITH NO OTHER.
           SELECT F-A1 ASSIGN TO "L1C11M.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A1-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-A2 ASSIGN TO "L1C11M.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A2-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD F-NO.
       01 NO-REC PIC X(5).
       FD F-A1.
       01 A1-REC PIC X(5).
       FD F-A2.
       01 A2-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 NO-ST PIC XX.
       01 A1-ST PIC XX.
       01 A2-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F-NO.
           MOVE "NO001" TO NO-REC.
           WRITE NO-REC.
           CLOSE F-NO.
           DISPLAY "NO-CREATE=" NO-ST.
           OPEN INPUT F-A1.
           OPEN INPUT F-A2.
           DISPLAY "A1=" A1-ST " A2=" A2-ST.
           OPEN INPUT F-NO.
           DISPLAY "NO-WHILE=" NO-ST.
           CLOSE F-A1 F-A2.
           OPEN INPUT F-NO.
           DISPLAY "NO-ALONE=" NO-ST.
           OPEN INPUT F-A1.
           DISPLAY "A1-WHILE=" A1-ST.
           CLOSE F-NO.
           OPEN OUTPUT F-A1.
           MOVE "AL001" TO A1-REC.
           WRITE A1-REC.
           CLOSE F-A1.
           DISPLAY "A1-CREATE=" A1-ST.
           OPEN INPUT F-NO.
           DISPLAY "NO-AFTER=" NO-ST.
           CLOSE F-NO.
           STOP RUN.
