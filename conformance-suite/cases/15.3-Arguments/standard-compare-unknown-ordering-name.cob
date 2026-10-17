*>--- conformance-case
*> id: standard-compare-unknown-ordering-name
*> rules: [AR-15.3-12, AR-15.85.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.85.3 rule 5: "Ordering-name-1, if specified, shall be associated with a cultural ordering table
      *> in the ORDER TABLE clause of the SPECIAL-NAMES paragraph." 15.3 argument type 12 says the same from the
      *> argument side: "An ordering-name defined in the SPECIAL-NAMES paragraph shall be specified." And
      *> 12.3.7.3 SR9 makes STANDARD-COMPARE the only place such a name may appear at all.
      *>
      *> The general format (15.85.2) is
      *>     FUNCTION STANDARD-COMPARE ( argument-1 argument-2 [ ordering-name-1 ] [ argument-4 ] )
      *> - two bracketed optionals in a fixed order, no choice indicators, so 5.2.6 makes them POSITIONAL. With
      *> four arguments written both are present, so the third position IS ordering-name-1 and there is no other
      *> reading available: NOSUCH is not associated with any ORDER TABLE clause (this unit declares OT1 and
      *> nothing else), so the reference is a syntax-rule violation, COBOLNET1663.
      *>
      *> The four-argument form is used deliberately. In the THREE-argument form the third position is either
      *> ordering-name-1 or argument-4, and 15.3 argument type 6 admits "an integer data item" - so a bare word
      *> there that names no ordering table is a legal argument-4 reference to a data-name, and rejecting it as a
      *> bad ordering-name would refuse conforming source.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SCUNKORD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ORDER TABLE OT1 IS "ISO 14651_2020_TABLE1".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION STANDARD-COMPARE("a" "b" NOSUCH 2) TO WS-R.
           DISPLAY "R=[" WS-R "]".
           STOP RUN.
