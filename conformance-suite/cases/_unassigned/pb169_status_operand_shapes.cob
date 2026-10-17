*>--- conformance-case
*> id: pb169_status_operand_shapes
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB169 - the STOP RUN / GOBACK termination-status operand is
      *> {identifier-1 | literal-1} (ISO 14.9.42.2), NOT arithmetic-expression-1,
      *> so 8.8.1.1 never governed it. SR2 admits "an integer data item or a data
      *> item with usage display or usage national"; SR3's conditional ("If
      *> literal-1 IS numeric, it shall be an integer") presupposes the
      *> non-numeric form; SR4 bars only a zero-length literal.
      *> Before this landing ALL THREE of the shapes below drew COBOLNET0844
      *> quoting 8.8.1.1 - legal COBOL rejected under a rule the programmer had
      *> not broken. The EXIT-CODE values are asserted by
      *> StopGobackExitCodeTests (a .out golden compares stdout only).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB169STATUS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-DISPLAY PIC X(3) VALUE "007".
       01 WS-INT     PIC 9(3) VALUE 42.
       01 WS-NAT     PIC N(3) VALUE N"012".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "DISPLAY=" WS-DISPLAY
           DISPLAY "INT=" WS-INT
           DISPLAY "NAT=" WS-NAT
           STOP RUN WITH ERROR STATUS "ABEND".
