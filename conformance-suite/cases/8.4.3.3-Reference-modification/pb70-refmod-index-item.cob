*>--- conformance-case
*> id: pb70-refmod-index-item
*> rules: [SR-8.4.3.3.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.3.3 SR1 lists the items identifier-1 may reference; an index data item (class index,
      *> 13.18.60) is not among them. kb/Work PB70: COBOLNET1647 at bind (a run-time NotImplemented before).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB70NINDEX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IX USAGE INDEX.
       01 R PIC X(4).
       PROCEDURE DIVISION.
           MOVE IX(1:1) TO R.
           STOP RUN.
