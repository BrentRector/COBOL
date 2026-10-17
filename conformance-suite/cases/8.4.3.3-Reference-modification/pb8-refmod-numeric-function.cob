*>--- conformance-case
*> id: pb8-refmod-numeric-function
*> rules: [SR-8.4.3.3.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.3.3 SR2: "If identifier-1 is a function-identifier, it shall reference an alphanumeric,
      *> boolean, or national function." PI (15.73) is a NUMERIC function, so its result has no character
      *> positions for 8.4.3.3.4 GR4 to number and it cannot be reference-modified. Before PB8 this shape was
      *> a COBOL0001 parse error, so the class question could not even be asked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB8NEGNUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(2).
       PROCEDURE DIVISION.
           MOVE FUNCTION PI (1:2) TO T
           STOP RUN.
