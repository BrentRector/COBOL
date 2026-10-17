*>--- conformance-case
*> id: pb693-program-id-reserved-word
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB693: the PROGRAM-ID program-name slot (ISO 11.4.2) is one of the four
*> positions the 8.9 funnel screens as provably a user-defined-word use. The
*> reservation gate withdraws UNLOCK from cobolWord at 2002+, so programName carries
*> the `reservedGatedWord` re-admission under the INVERSE predicate: the paragraph
*> still PARSES and the funnel answers with the targeted COBOLNET0901 naming 8.9,
*> instead of a raw COBOL0001 that never says why. 8.3.2.1 rule 1: "Reserved words
*> shall not be used as user-defined words or system-names". At 85, where 8.9 does
*> not reserve UNLOCK, the same source is a conforming program - the 85 acceptance
*> lane is tests/conformance/85/pb693_reserved_words_as_data_names.cob.
IDENTIFICATION DIVISION.
PROGRAM-ID. UNLOCK.
PROCEDURE DIVISION.
MAIN.
    DISPLAY "X".
    STOP RUN.
