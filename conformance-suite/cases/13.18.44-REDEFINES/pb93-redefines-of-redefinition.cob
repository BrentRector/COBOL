*>--- conformance-case
*> id: pb93-redefines-of-redefinition
*> rules: [SR-13.18.44.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.44.3 SR7: "Multiple redefinitions of the same storage area shall each specify as data-name-2 the
      *> data-name of the entry that originally defined the area" - C REDEFINES B, where B is itself a redefinition
      *> of A, is a chain ISO forbids (error strict; the anchor-chased chain is a --permissive leniency). kb/Work PB93.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB93N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC.
          05 A PIC X(2) VALUE "ab".
          05 B REDEFINES A PIC X(2).
          05 C REDEFINES B PIC X(2).
       PROCEDURE DIVISION.
           DISPLAY C.
           STOP RUN.
