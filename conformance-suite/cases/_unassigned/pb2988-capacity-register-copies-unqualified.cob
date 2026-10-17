      *>--- conformance-case
      *> id: pb2988-capacity-register-copies-unqualified
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2988 - ISO 13.18.38.3 SR30: "If qualifiers are
      *>   required for uniqueness, it shall be treated as though
      *>   implicitly defined at the same level as the entry containing
      *>   the OCCURS clause" -> OK 13.18.38.3 30). A and B each
      *>   reproduce the register DC of ST, so the bare DC names two
      *>   registers: 8.4.2.2.3 SR1 "uniqueness shall be established
      *>   through a sequence of qualifiers that precludes any ambiguity
      *>   of reference" -> OK 8.4.2.2.3 1). Refused COBOLNET1639 (the
      *>   one ambiguity verdict), never resolved to A's register.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2988N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST TYPEDEF.
          05 D PIC X OCCURS DYNAMIC CAPACITY IN DC.
       01 A TYPE ST.
       01 B TYPE ST.
       PROCEDURE DIVISION.
           SET DC TO 2
           STOP RUN.
