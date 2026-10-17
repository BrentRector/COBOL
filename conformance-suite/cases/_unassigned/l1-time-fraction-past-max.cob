*>--- conformance-case
*> id: l1-time-fraction-past-max
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.3.3.2 — "The implementor defines the maximum number of digit positions that may
      *> be specified in the decimal fraction portion of the seconds subfield of a time format;
      *> that maximum shall be greater than or equal to nine." COBOL.NET's documented maximum is
      *> 18 (Annex A.1 item 202; docs/CONFORMANCE.md DOC-A.1-202), which satisfies the floor. A
      *> fraction of 19 digit positions therefore exceeds the maximum, so the operand is NOT a
      *> time format at all — and §15.41.3 r2 requires that "The content of argument-1 shall be a
      *> time format". The violation is a syntax rule on a literal argument, so it is diagnosed
      *> at COMPILE time (COBOLNET1631) rather than fabricating a value at run time.
      *> This is the upper half of the pair whose lower half is
      *> conformance:cases/A.1-Implementor-defined-language-element-list/l1_time_fraction_max_digits (18 accepted); together they pin the
      *> documented maximum at exactly 18.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TFR02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SEC PIC 9(5)V9(9) VALUE 45296.5.
       01 R   PIC X(28).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-TIME
               ("hh:mm:ss.sssssssssssssssssss", SEC) TO R
           DISPLAY "R=[" R "]"
           STOP RUN.
