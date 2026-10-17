*>--- conformance-case
*> id: pb488-cond-var-class-pointer
*> rules: [SR-13.16.3-24, SR-13.18.60.3-11]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 e) / 13.18.60.3 syntax rule 11 - a data item of the class pointer
*> shall not be a conditional variable.
*>
*> kb/Work PB488: the binder accepted this and the emitter then wrote `if ((P == NULLL))` into the generated
*> C#, so the user's diagnostic was a RAW ROSLYN ERROR - `error CS0103: The name 'NULLL' does not exist in
*> the current context` - about an identifier that appears nowhere in their program. USAGE POINTER is a
*> COBOL-2002 addition, so 85 rejects it at its own introduction gate instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-CLASS-POINTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       88 P-NULL VALUE NULL.
       PROCEDURE DIVISION.
           IF P-NULL DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
