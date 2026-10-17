*>--- conformance-case
*> id: pb415-initialize-replacing-set-category-mismatch
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB415 — ISO 14.9.20.3 SR4: "For each of the categories data-pointer, function-pointer,
*> message-tag, object-reference, and program-pointer specified in the REPLACING phrase, a SET statement
*> with identifier-2 as the sending operand and an item of the specified category as the receiving operand
*> shall be valid." X1 is category alphanumeric, and no format of the SET statement (14.9.39) moves an
*> alphanumeric item into a data-pointer, so the required SET is not valid.
*> WHY IT IS A BIND-TIME REJECTION AND NOT A BACKEND FAILURE: 14.9.20.4 GR4 makes the implicit statement a
*> SET, which the emitter renders as the SET statement's own straight handle copy. Without this screen the
*> mismatch would reach Roslyn as a type error on GENERATED code, naming no COBOL rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB415NMIS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P1 USAGE POINTER.
       01 X1 PIC X(4) VALUE "abcd".
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE P1 REPLACING DATA-POINTER DATA BY X1.
           STOP RUN.
