*>--- conformance-case
*> id: pb655-alter-dataname-85
*> rules: [SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 8.9 / 8.3.2.1 1) - ALTER is reserved at COBOL-85 (reserved-words.json r85 true),
*> so declaring it as a data-name is refused there, and refused BY NAME: a gated word is
*> never a cobolWord, the declaration parses through reservedGatedWord, and because 85
*> RESERVES the word the token-level gate does not retype it - so the funnel answers
*> COBOLNET0901 ('ALTER' is a reserved word in COBOL-85) instead of a raw COBOL0001. The
*> positive twin is
*> 2002/pb655_dropped_words_user_words (kb/Work PB655).
 IDENTIFICATION DIVISION.
 PROGRAM-ID. PB655-ALTER-85.
 DATA DIVISION.
 WORKING-STORAGE SECTION.
 01 WS-G.
    05 ALTER PIC X(2) VALUE "AL".
 PROCEDURE DIVISION.
 MAIN-PARA.
     DISPLAY "UNREACHABLE"
     STOP RUN.
