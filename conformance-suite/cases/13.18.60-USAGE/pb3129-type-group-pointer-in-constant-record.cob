*>--- conformance-case
*> id: pb3129-type-group-pointer-in-constant-record
*> rules: [SR-13.18.60.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3129 review R1-1 - ISO 13.18.60.3 SR4: the INDEX, MESSAGE-TAG, OBJECT REFERENCE, POINTER,
      *> FUNCTION-POINTER, and PROGRAM-POINTER phrases "shall not be specified in a data item described with
      *> the CONSTANT RECORD clause, or in any item subordinate to a data item described with the CONSTANT
      *> RECORD clause". 13.18.57.4 GR1 codes the description of the type ST "in place of the TYPE clause",
      *> so the level-1 structured constant CR holds ST's POINTER member SP. ST is a legal STRONG type
      *> declaration (13.18.60.3 SR14 admits a pointer subordinate to one) and a strongly-typed item at
      *> level 1 is legal (13.18.57.3 SR6), so only the CONSTANT RECORD placement breaks a rule. The error
      *> names CR, the entry that carries the TYPE clause. Before the fix this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3129CG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST IS TYPEDEF STRONG.
          05 SX PIC X(3).
          05 SP USAGE POINTER.
       01 CR CONSTANT RECORD TYPE ST.
       PROCEDURE DIVISION.
           DISPLAY SX OF CR
           STOP RUN.
