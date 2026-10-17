      *>--- conformance-case
      *> id: l1c11-file-section-entry-before-fd
      *> rules: [FMT-13.4.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.4.2 general format — every entry of the FILE SECTION
      *>   follows a file-description-entry or a
      *>   sort-merge-file-description-entry
      *>   cite.py --check 13.4.2 "file-description-entry"
      *>   -> OK  §13.4.2   (General format)
      *>   cite.py --check 13.4.2 "sort-merge-file-description-entry"
      *>   -> OK  §13.4.2   (General format)
      *> The format admits a record-description-entry only inside the
      *> group opened by an FD or SD; W-LOOSE is written directly
      *> after the FILE SECTION header, before any FD. Moving it after
      *> FD F-OUT makes the program legal, so the only defect is the
      *> position the format forbids. Pure general-format rule: the
      *> expected diagnostic is the parser's unexpected-token error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-OUT ASSIGN TO "L1C11N2.DAT".
       DATA DIVISION.
       FILE SECTION.
       01 W-LOOSE PIC X(5).
       FD F-OUT.
       01 OUT-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F-OUT.
           CLOSE F-OUT.
           STOP RUN.
