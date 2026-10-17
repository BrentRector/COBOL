*>--- conformance-case
*> id: pb2548-template-key-unresolved
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2548 - an UNUSED TYPEDEF template. ISO 13.18.38.3 SR3 (cite.py OK): "The first specification of
      *> data-name-2 shall be the name of either the entry containing the OCCURS clause or an entry subordinate to the
      *> entry containing the OCCURS clause." NOSUCH is neither. The template has no storage (ISO 13.18.58.4 GR2) and no
      *> TYPE clause references it, but a syntax rule binds the source as written (ISO 4.3). Before kb/Work PB2548 the
      *> OCCURS KEY resolver walked the storage forest only, so this compiled clean (COBOLNET2353 when the template
      *> was used).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2548KEY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF.
          05 T OCCURS 4 ASCENDING KEY NOSUCH.
             10 TK PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "R01".
           STOP RUN.
