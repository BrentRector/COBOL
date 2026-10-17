*>--- conformance-case
*> id: pb449-set-mixed-program-pointer-receivers
*> rules: [SR-14.9.39.3-21]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR21 - "Identifier-7 shall reference a data item of category program-pointer.
      *> Identifier-8 shall be of category program-pointer." Format 9's receiving brace is { identifier-7 } ...,
      *> so a program-pointer written among the receivers puts the statement under Format 9 and every OTHER
      *> receiver is in violation of the first sentence.
      *> ⛔ Both operands here are identifiers and both are numeric except PP1, so Format 9 matched syntactically
      *> and was the ONLY format whose receiving operand may be a program-pointer - yet SR21 was violated twice
      *> over and never asked, because the re-route sniffed only receivers[0] and the sender. The program
      *> compiled with zero diagnostics and aborted at run time with "arithmetic into a non-fixed-point target
      *> 'PP1'". kb/Work PB449; the data-pointer twin is pb449-set-mixed-pointer-receivers.
      *> Rejected from 2002: USAGE PROGRAM-POINTER and Format 9 are COBOL-2002 introductions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB449N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP1 USAGE PROGRAM-POINTER.
       01 WS-N PIC 9(4).
       01 WS-M PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET WS-N PP1 TO WS-M
           STOP RUN.
