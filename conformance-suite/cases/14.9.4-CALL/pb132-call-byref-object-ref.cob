*>--- conformance-case
*> id: pb132-call-byref-object-ref
*> rules: [SR-14.9.4.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR10's THIRD kind: a data item of class OBJECT shall not pass BY REFERENCE in a
      *> Format-1 CALL (the pointer and strongly-typed kinds are pinned by their own fixtures; one switch).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132N13.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBASE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OREF USAGE OBJECT REFERENCE CBASE.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB" USING OREF
           STOP RUN.
       IDENTIFICATION DIVISION.
       CLASS-ID. CBASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS CBASE.
