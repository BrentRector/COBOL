*>--- conformance-case
*> id: pb488-cond-var-class-index
*> rules: [SR-13.16.3-24, SR-13.18.60.3-11]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 - "... e) A data item of the class index, message-tag, object, or
*> pointer", and the same prohibition at 13.18.60.3 syntax rule 11 - "An elementary data item of class index,
*> message-tag, object, or pointer shall not be a conditional variable."
*>
*> kb/Work PB488: the compiler had IMPLEMENTED this. `SET IX TO 1` then `IF IX-ONE` took the TRUE branch at
*> all four editions - a working nonconforming feature, which is the under-rejection users come to depend on.
*> Class index exists in COBOL-85 and the 85 exclusion list names "an index data item", so all four reject.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-CLASS-INDEX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IX USAGE INDEX.
       88 IX-ONE VALUE 1.
       PROCEDURE DIVISION.
           SET IX TO 1
           IF IX-ONE DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
