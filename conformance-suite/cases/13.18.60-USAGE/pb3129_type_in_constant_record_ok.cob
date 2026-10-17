*>--- conformance-case
*> id: pb3129_type_in_constant_record_ok
*> rules: [SR-13.18.60.3-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3129 review R1-1 - what a TYPE entry may still compose
      *> inside a CONSTANT RECORD, and where a TYPE USAGE INDEX entry may stand.
      *> 13.18.60.3 SR4 bars the INDEX, MESSAGE-TAG, OBJECT REFERENCE, POINTER,
      *> FUNCTION-POINTER, and PROGRAM-POINTER phrases "in any item subordinate
      *> to a data item described with the CONSTANT RECORD clause", and
      *> 13.18.57.4 GR1 makes a TYPE entry "as though the data description
      *> identified by type-name-1 had been coded in place of the TYPE clause".
      *> So CA (TYPE XT, PIC X(3)) and CG (TYPE GT, no barred phrase) are legal
      *> in the structured constant CR, while the same TYPE of an INDEX type is
      *> not (negative pb3129-type-index-in-constant-record). SR14's list has no
      *> INDEX phrase, so GI (TYPE IT, USAGE INDEX) at level 05 of the ordinary
      *> group G is legal: the screen asks SR4 and SR14 apart.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3129CR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XT IS TYPEDEF PIC X(3).
       01 IT IS TYPEDEF USAGE INDEX.
       01 GT IS TYPEDEF.
          05 GX PIC X(2).
          05 GN PIC 9(2).
       01 CR CONSTANT RECORD.
          05 CA TYPE XT VALUE "ABC".
          05 CG TYPE GT.
          05 CB PIC X VALUE "Z".
       01 G.
          05 GI TYPE IT.
          05 GY PIC X.
       PROCEDURE DIVISION.
           DISPLAY "CA=" CA OF CR " CB=" CB OF CR
           MOVE "Q" TO GY
           DISPLAY "GY=" GY
           STOP RUN.
