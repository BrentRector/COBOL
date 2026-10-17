*>--- conformance-case
*> id: pb70-refmod-binary-item
*> rules: [SR-8.4.3.3.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.3.3 SR1: a numeric item may be identifier-1 only "of usage display or national"; a BINARY
      *> (COMP) item has no character positions to modify. kb/Work PB70: this was a run-time NotImplemented on
      *> a sending ref-mod and a SILENT no-op on a receiving one; COBOLNET1647 at bind now.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB70NBINARY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CB PIC 9(4) COMP.
       01 R PIC X(4).
       PROCEDURE DIVISION.
           MOVE R TO CB(1:2).
           STOP RUN.
