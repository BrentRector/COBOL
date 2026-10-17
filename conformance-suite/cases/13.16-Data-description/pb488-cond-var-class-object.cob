*>--- conformance-case
*> id: pb488-cond-var-class-object
*> rules: [SR-13.16.3-24, SR-13.18.60.3-11]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 syntax rule 24 e) / 13.18.60.3 syntax rule 11 - a data item of the class object
*> shall not be a conditional variable. The object-reference sibling of the pointer case, measured by
*> kb/Work PB488 to leak the same raw Roslyn error. USAGE OBJECT REFERENCE is a COBOL-2002 addition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-COND-VAR-CLASS-OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       88 O-NULL VALUE NULL.
       PROCEDURE DIVISION.
           IF O-NULL DISPLAY "T" ELSE DISPLAY "F" END-IF
           STOP RUN.
