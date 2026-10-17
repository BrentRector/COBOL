      *>--- conformance-case
      *> id: pb330-open-multi-file-imp1
      *> rules: [SR-14.9.27.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.27.3 SR3 - "An OPEN statement that specifies
      *> file-name-1 more than once shall not be specified in
      *> imperative-statement-1 of an exception-checking PERFORM
      *> statement." file-name-1 is the FORMAT TERM, repeated by the
      *> ellipsis; 14.9.27.4 GR20 calls exactly that form "more than one
      *> file-name is specified in an OPEN statement". F1 and F2 are two
      *> DIFFERENT file-names, and the statement is still banned: the
      *> family (CLOSE 14.9.6.3 SR3, DELETE FILE 14.9.10.3 SR4) bans the
      *> multi-operand statement, because an unsuccessful implicit OPEN
      *> transfers control to the WHEN phrase and abandons the rest.
      *> Until kb/Work PB330 only a REPEATED name was rejected.
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023, so below 2023 the construct gate rejects the whole
      *> format with a different diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB330NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb330n1.txt".
           SELECT F2 ASSIGN TO "pb330n2.txt".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R-F1 PIC X(5).
       FD F2.
       01 R-F2 PIC X(5).
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               OPEN OUTPUT F1 F2
           WHEN EXCEPTION F1
               CONTINUE
           END-PERFORM.
           STOP RUN.
