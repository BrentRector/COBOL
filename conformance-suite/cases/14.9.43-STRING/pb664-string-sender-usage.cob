*>--- conformance-case
*> id: pb664-string-sender-usage
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.43.3 SR1: "All literals shall be described as alphanumeric, boolean, or national literals, and
      *> all identifiers, except identifier-4, shall be described implicitly or explicitly as usage display or
      *> national."  Identifier-4 is the WITH POINTER item, so identifier-1 - the SENDING operand - is covered
      *> by the same sentence that covers identifier-3.  A USAGE BINARY item is neither display nor national.
      *> This screen existed only at identifier-3, so the sending position reached OperandText's
      *> sending-image renderer and rendered whatever the carrier's .ToString() gave (kb/Work PB664).
      *> The rule is COBOL-85 and unchanged since, so every edition rejects.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB664NEGU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CMP PIC 9(4) COMP VALUE 1234.
       01 A66 PIC X(40).
       PROCEDURE DIVISION.
           MOVE SPACES TO A66
           STRING CMP DELIMITED BY SIZE INTO A66
           DISPLAY "A66=[" A66 "]"
           STOP RUN.
