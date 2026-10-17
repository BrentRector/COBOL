*>--- conformance-case
*> id: pb704-order-as-data-name
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB704: ORDER became a lexer token so the SORT DUPLICATES phrase (14.9.40.2) and the
*> SPECIAL-NAMES ORDER TABLE clause (12.3.7.2) stop borrowing `cobolWord`. THIS is the other half of
*> that change and the reason the fix is a token and not an exemption inside the 8.9 funnel: 8.9 reserves
*> ORDER from 2002, so 8.3.2.1 rule 1 ("Reserved words shall not be used as user-defined words or
*> system-names") still bars the word from a NAME slot at 2002/2014/2023. The generated reservation gate
*> withdraws ORDER from cobolWord there and `reservedGatedWord` re-admits it in the declaration slot only,
*> so the entry PARSES and the funnel answers with the targeted COBOLNET0901 naming 8.9 instead of a raw
*> COBOL0001. At 85, where 8.9 does not reserve ORDER, the same declaration is conforming - that
*> acceptance lane is conformance-suite/cases/_unassigned/pb693_reserved_words_as_data_names.cob.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB704ORDNAME.
DATA DIVISION.
WORKING-STORAGE SECTION.
01  ORDER PIC X(3) VALUE "ORD".
PROCEDURE DIVISION.
MAIN.
    DISPLAY "1=" ORDER.
    STOP RUN.
