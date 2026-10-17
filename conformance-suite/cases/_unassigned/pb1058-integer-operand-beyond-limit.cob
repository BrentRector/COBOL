*>--- conformance-case
*> id: pb1058-integer-operand-beyond-limit
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> AN INTEGER-N BEYOND THE IMPLEMENTATION LIMIT (kb/Work PB1058). COBOL.NET
      *> binds the integer-n of a data description (a PAGE or LINAGE line count,
      *> a COLUMN, an OCCURS size) into its own model as a 32-bit integer, so a
      *> value above 2,147,483,647 is beyond this implementation's limit —
      *> ISO/IEC 1989:2023 §4.5: "Translation may be unsuccessful due to factors
      *> other than lack of conformance of a compilation group", and its NOTE
      *> names "the limits of an implementation" (docs/CONFORMANCE.md §3
      *> "Integer operands and host carriers"). Before PB1058 each of these took
      *> the COMPILER down with an unhandled System.OverflowException out of
      *> int.Parse; now each is COBOLNET2427 at every edition (the report writer
      *> and LINAGE are COBOL-85 syntax).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1058IL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1058-integer-operand-beyond-limit.txt".
           SELECT LST ASSIGN TO "pb1058-linage-beyond-limit.txt".
       DATA DIVISION.
       FILE SECTION.
       FD PRT REPORT IS RR.
       FD LST LINAGE IS 77777777777 LINES.
       01 LST-REC PIC X(10).
       REPORT SECTION.
       RD RR PAGE LIMIT IS 77777777777 LINES.
       01 TYPE IS DETAIL LINE NUMBER IS 1.
          05 COLUMN NUMBER IS 77777777777 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
           STOP RUN.
