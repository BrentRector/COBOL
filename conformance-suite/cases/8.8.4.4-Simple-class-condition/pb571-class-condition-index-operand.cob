*>--- conformance-case
*> id: pb571-class-condition-index-operand
*> rules: [SR-8.8.4.4.3-1, GR-13.18.60.4-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.4.3 SR1: "Identifier-1 shall not reference a data item of class index, message-tag,
      *> object, or pointer, nor a strongly-typed group, nor a variable-length group." The rule is
      *> version-invariant - the class condition and USAGE INDEX both exist at COBOL-85 - so every edition
      *> rejects.
      *>
      *> It was a WRONG ANSWER, not a missing refusal (kb/Work PB571). An index data item's storage profile
      *> carries category NUMERIC because the occurrence number IS a number, and the operand screen returned
      *> early unless the operand's category was boolean, so SR1 was never asked of anything: this program
      *> compiled clean and PRINTED "IX IS NUMERIC" - the precise contradiction of 13.18.60.4 GR10, "The
      *> class and category of an index data item are index".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB571NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IX USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN.
           SET IX TO 3
           IF IX IS NUMERIC
               DISPLAY "IX IS NUMERIC"
           END-IF
           STOP RUN.
