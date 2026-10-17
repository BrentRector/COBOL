*>--- conformance-case
*> id: pb456-set-object-ref-literal-sender
*> rules: [SR-14.9.39.3-8, SR-14.9.39.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR9 - "Identifier-4 shall be an object reference; the predefined object reference SUPER
      *> shall not be specified." A numeric literal is not an object reference, so SET U TO 5 is not a legal
      *> Format 5 - and it is not a legal Format 1 either, because SR1 makes identifier-1 "a data item of class
      *> index or an integer data item" and a universal object reference is neither. The program is refused under
      *> one of those two rules whichever way it is read; the format is selected from the RECEIVER, so it is SR9.
      *> ⛔ It used to compile with ZERO diagnostics and abort at run time (kb/Work PB456). Format 5 was reached
      *> by a semantic re-route whose precondition was "the sender is exactly one bare data reference"; a literal
      *> is not one, the re-route declined, and an object reference landed in the Format-1 arithmetic store with
      *> no operand rule consulted at all. The control that always worked - SET U TO WX over PIC X(4) - is the
      *> same illegal statement with a sender the re-route happened to recognize.
      *> Rejected from 2002: USAGE OBJECT REFERENCE and Format 5 are COBOL-2002 introductions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB456N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET U TO 5
           STOP RUN.
