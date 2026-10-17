*>--- conformance-case
*> id: pb829-assign-target-list
*> rules: [FMT-12.4.5.1, DOC-A.1-71]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB829 (finisher) - ISO 12.4.5.1 prints `ASSIGN [TO] {device-name-1 | literal-1} ...`, so
      *> a TO phrase with two literals is inside the general format and must PARSE; ISO 12.4.5.2 SR5
      *> ("The meaning and rules for the allowable specification of device-name-1 and the value of
      *> literal-1 are defined by the implementor") is what refuses it.  COBOL.NET's determination
      *> (docs/CONFORMANCE.md section 7, DOC-A.1-71) allows one operand naming the file, or a device
      *> class (DISK or PRINTER) followed by one operand naming the file - two file names are neither,
      *> so this is COBOLNET2256 BY NAME.  It used to be COBOL0308, a parse error at the second literal.
      *> Rejected at every edition: the list and SR5's latitude are the same in all four.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB829AT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb829at1.dat" "pb829at2.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
