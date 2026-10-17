*>--- conformance-case
*> id: pb878-move-binary-char-to-group
*> rules: [SR-14.9.25.3-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.25.3 SR8: "If identifier-1 references a data item described with usage binary-char,
*> binary-short, binary-long, or binary-double, identifier-2 shall reference a numeric or numeric-edited item."
*> A group item is neither (8.5.2.1 gives a group class and category alphanumeric, boolean or national), and SR8
*> carries no group exemption - 14.9.25.4 GR4's conversion-free group copy is Table 16's (SR10), and SR10
*> applies only "for all other cases not described in Syntax rules 8 and 9".
*> kb/Work PB878 (closing the SR8 arm kb/Work PB422 measured): the written MOVE skipped every GROUP receiver
*> before SR8 was asked, so this compiled clean and stored '065 '. MOVE now asks the one 14.9.25.3 chain
*> (MoveTable16.Validity) for every receiver, as MOVE CORRESPONDING's pairing filter already did. COBOLNET0819.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB878NBG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BC USAGE BINARY-CHAR VALUE 65.
       01 G.
          05 GX PIC X(4).
       PROCEDURE DIVISION.
           MOVE BC TO G
           DISPLAY "G=[" G "]"
           STOP RUN.
