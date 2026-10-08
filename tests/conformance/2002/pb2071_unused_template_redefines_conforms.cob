      *> kb/Work PB2071 - COMPILE-ONLY positive: REDEFINES clauses written in UNUSED TYPEDEF templates that conform, so
      *> the template screen added for PB2071 must keep them clean. ISO 13.18.44.3 SR14 (cite.py OK) bars a data-name-2
      *> that is "a strongly-typed group item, or an item subordinate to a strongly-typed group item", and ISO 8.5.3.1
      *> (cite.py OK) makes a group strongly typed when it "is described with a TYPE clause that references a type
      *> declaration specifying the STRONG phrase". A template's level-1 entry carries TYPEDEF, not TYPE, so UX, X and
      *> the group X2 are subordinate to no strongly-typed group item in their own templates (ISO 13.18.58.4 GR2: a type
      *> declaration has no storage). The weak template's REDEFINES obeys every rule of ISO 13.18.44.3 as written. A TYPE
      *> subject of the STRONG templates would be refused at the subject (negative pb1282-redefines-in-strong-typedef).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2071TPLOK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF STRONG.
          05 UX PIC X(4).
          05 UY REDEFINES UX PIC X(4).
       01 U2 IS TYPEDEF STRONG.
          05 X PIC X(4).
          05 H REDEFINES X.
             10 HA PIC X(4).
       01 U3 IS TYPEDEF STRONG.
          05 X2.
             10 XA PIC X(4).
          05 H2 REDEFINES X2 PIC X(4).
       01 W IS TYPEDEF.
          05 WX PIC X(4).
          05 WY REDEFINES WX PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "OK".
           STOP RUN.
