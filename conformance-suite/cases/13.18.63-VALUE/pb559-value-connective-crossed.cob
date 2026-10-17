*>--- conformance-case
*> id: pb559-value-connective-crossed
*> rules: [FMT-13.18.63.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB559 - ISO 1989:2023 13.18.63.2. Formats 2 (table), 3 (condition-name) and 4 (report-section)
*> each print their leading words as a TWO-LINE REQUIRED CHOICE, "VALUE IS" over "VALUES ARE", and 5.2.6.3
*> makes a brace choice exactly one of its alternatives - so VALUE pairs with IS and VALUES pairs with ARE.
*> "VALUES IS" is the cross-product of that choice and is printed by no format of the clause.
*> 13.18.63.3 SR39 states the identical pairing in WORDS for Format 5, the one format that detaches the
*> connective: "If the word VALUE is specified, the word IS may be specified. If the word VALUES is
*> specified, the word ARE may be specified." That is the standard confirming, where it had to be said
*> outside a figure, exactly what the other formats say with a brace.
*> The subject here is a level-88 entry, so the format is 3 (13.18.63.3 SR33), which reaches SR17's
*> VALUE/VALUES equivalence through SR24 - VALUES itself is legal here, and only its pairing with IS is not.
*> The legal spellings are pinned POSITIVELY by 2002/l1_value_values_equivalent_2002 and
*> 2023/l1_value_values_equivalent, which write all four printed heads and assert they agree.
*> MEASURED before the fix: rc 0 at every edition. EDITION-INVARIANT: the figures' brace is unchanged
*> across 1985/2002/2014/2023 and Annex E records no change; MEASURED rejected at all four.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB559A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CV PIC 9 VALUE 1.
   88 CN VALUES IS 1.
PROCEDURE DIVISION.
    IF CN DISPLAY "IN" END-IF.
    STOP RUN.
