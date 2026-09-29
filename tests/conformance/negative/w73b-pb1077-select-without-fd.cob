      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1077 - ISO 12.4.5.2 SR3: "For each file-name specified
      *> in a SELECT clause, there shall be a file description entry or a
      *> sort-merge file description entry in the file section of the
      *> factory, function, object, or program in which the SELECT clause
      *> is specified." F2 has neither. Before the fix this compiled clean
      *> and OPEN OUTPUT F2 aborted the run unit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73BSR3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "w73b_sr3a.dat".
           SELECT F2 ASSIGN TO "w73b_sr3b.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       PROCEDURE DIVISION.
           OPEN OUTPUT F2.
           CLOSE F2.
           STOP RUN.
