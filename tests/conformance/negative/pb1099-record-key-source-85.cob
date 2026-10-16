      *> reject-at: 85
      *> kb/Work PB1099 Q1 - ANSI X3.23-1985 prints RECORD KEY IS data-name-1 and ALTERNATE RECORD KEY IS
      *> data-name-1 only; the second key form, record-key-name-1 SOURCE IS { data-name-2 } ... (ISO
      *> 12.4.5.12.2 / 12.4.5.6.2), is a post-1985 form on the provisional 2002 edge (VCR row 7.36, derived:
      *> Annex E lists no 2014->2023 change for it, and the 2002/2014 split is not derivable in-repo). Both
      *> clauses share the one grammar rule and the one gate, so each SOURCE phrase draws COBOLNET0900 at 85;
      *> the positive is conformance 2002/pb1099_record_key_source.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1099N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-IX ASSIGN TO "pb1099n.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS F-COMPOSITE SOURCE IS F-K1 F-K2
               ALTERNATE RECORD KEY IS F-ALT SOURCE IS F-D
                   WITH DUPLICATES.
       DATA DIVISION.
       FILE SECTION.
       FD F-IX.
       01 F-REC.
          05 F-K1 PIC X(2).
          05 F-K2 PIC X(2).
          05 F-D  PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F-IX
           CLOSE F-IX
           STOP RUN.
