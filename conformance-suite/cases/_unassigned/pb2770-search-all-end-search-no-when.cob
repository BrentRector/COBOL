*>--- conformance-case
*> id: pb2770-search-all-end-search-no-when
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2770: SEARCH ALL with no WHEN phrase. ISO §14.9.37.2 Format 2 (all) writes its
      *> `WHEN { data-name-1 ... | condition-name-1 }` phrase unbracketed, so the WHEN phrase is required
      *> and the statement is refused at END-SEARCH. END-SEARCH is SEARCH's explicit scope terminator (ISO §14.5.3.2)
      *> and its SEARCH statement is open here, so the refusal is the ordinary syntax error. It used to read
      *> COBOL0306 "END-SEARCH appears without a matching SEARCH statement": the hint built the rule name
      *> searchStatement from the word's spelling and never saw searchAllStatement on the parse stack.
      *> Control: the same program with `WHEN T-E (IX) = "C" DISPLAY "FOUND"` before END-SEARCH compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2770NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T VALUE "ABCDE".
          05 T-E PIC X OCCURS 5 ASCENDING KEY T-E INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL T-E END-SEARCH.
           STOP RUN.
       END PROGRAM P2770NEG.
