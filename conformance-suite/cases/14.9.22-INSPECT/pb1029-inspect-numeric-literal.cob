*>--- conformance-case
*> id: pb1029-inspect-numeric-literal
*> rules: [SR-14.9.22.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1029 - ISO 14.9.22.3 SR3: "Each literal shall be an alphanumeric, boolean, or
      *> national literal." A numeric literal as the TALLYING FOR ALL operand was bound as an operand
      *> error node WITHOUT a diagnostic: the program compiled clean and aborted the run unit when the
      *> INSPECT was reached. Expected: COBOLNET1757 (statement-operand-rule) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1029NIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(10) VALUE "A1A1".
       01 WS-N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           INSPECT WS-A TALLYING WS-N FOR ALL 1
           DISPLAY WS-N
           STOP RUN.
