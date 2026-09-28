      *> reject-at: 2002 2014 2023
      *> kb/Work PB939 / PB1427 (wave-69 finisher) - VALUE NULL on data
      *> items of classes that admit a VALUE clause, in both writing
      *> positions: the data-item entry and the condition-name entry.
      *> Every format of the VALUE clause writes literal-n (13.18.63.2),
      *> and NULL is not a literal: it is an IDENTIFIER (8.4.3.1.2
      *> Format 8), which 8.4.3.10.3 SR1 confines to an INITIALIZE or
      *> SET sending operand, a prototype CALL / function activation /
      *> method invocation argument, or a pointer-or-object-reference
      *> relation condition - no VALUE clause, at any class.
      *> COBOLNET2576 at each NULL.
      *> MEASURED BEFORE: compiled clean at every edition; N ran as
      *> 0000 and A held four U+0000 characters (LOW-VALUE).
      *> The predefined NULL is a COBOL-2002 addition; 2002+ pinned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W69YVNUL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE NULL.
       01 A PIC X(4) VALUE NULL.
       01 C PIC X.
          88 C-NULL VALUE NULL.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "N=[" N "]"
           STOP RUN.
