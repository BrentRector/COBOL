*>--- conformance-case
*> id: pb570_group_no_sign
*> rules: [SR-13.18.40.3-31, GR-13.18.60.4-11]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §13.18.60.4 GR1 + GR11 — the USAGE clause's WITH NO SIGN phrase reaches an elementary
      *> item BY GROUP INHERITANCE exactly as it does when the entry writes the clause itself. GR1: "If the USAGE
      *> clause is specified or implied at a group level, it applies only to each elementary item in the group";
      *> GR11: "If the WITH NO SIGN phrase is specified the representation of the data item in the storage of the
      *> computer reserves no storage for representing any sign value ... the data item is always considered to
      *> have a zero, or positive value". The phrase is part of the clause, not a clause of its own — §13.18.60.2
      *> prints it inside one alternative, `PACKED-DECIMAL [ WITH NO SIGN ]` — so it travels wherever that clause
      *> travels: down the group (EG, and EG2 two levels down), through a TYPE reference to a TYPEDEF that wrote
      *> it (§13.18.57.4 GR1), and through the §13.18.49.4 GR3 SAME-AS ancestor transform (SA).
      *> Widths DERIVED FROM THE RULE, not measured: PACKED-DECIMAL packs two digits per byte with a trailing sign
      *> nibble, so plain packed 9(n) is n/2+1 bytes and NO SIGN, reserving no sign storage, is ceil(n/2).
      *>   9(4): plain 3, NO SIGN 2.   9(6): NO SIGN 3.
      *> H shows the "nearest enclosing clause" half of GR1: H writes a plain PACKED-DECIMAL clause, so HL takes
      *> H's clause — sign nibble included, 3 bytes — and not GN's.
      *> Before kb/Work PB570 the phrase was read from the ENTRY'S OWN usageClause and adjudicated against the
      *> entry's own PICTURE, which a group header has not got: every inherited item kept the SIGNED layout with
      *> no diagnostic, so EG measured 3 against EL's 2 for one and the same description.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB570NS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TT TYPEDEF USAGE PACKED-DECIMAL WITH NO SIGN.
           05  TX  PIC 9(4).
       01  GRP USAGE PACKED-DECIMAL WITH NO SIGN.
           05  EG   PIC 9(4).
           05  SUBG.
               10  EG2  PIC 9(6).
       01  GPLAIN USAGE PACKED-DECIMAL.
           05  EP   PIC 9(4).
       01  GN USAGE PACKED-DECIMAL WITH NO SIGN.
           05  H    USAGE PACKED-DECIMAL.
               10  HL   PIC 9(4).
       01  EL  PIC 9(4) PACKED-DECIMAL WITH NO SIGN.
       01  RT  TYPE TT.
       01  SA  SAME AS EG.
       PROCEDURE DIVISION.
       MAIN.
           MOVE -42 TO EG
           DISPLAY "EG=" FUNCTION BYTE-LENGTH(EG) " V=" EG
           DISPLAY "EG2=" FUNCTION BYTE-LENGTH(EG2)
           DISPLAY "EP=" FUNCTION BYTE-LENGTH(EP)
           DISPLAY "HL=" FUNCTION BYTE-LENGTH(HL)
           DISPLAY "EL=" FUNCTION BYTE-LENGTH(EL)
           DISPLAY "TX=" FUNCTION BYTE-LENGTH(TX OF RT)
           DISPLAY "SA=" FUNCTION BYTE-LENGTH(SA)
           STOP RUN.
