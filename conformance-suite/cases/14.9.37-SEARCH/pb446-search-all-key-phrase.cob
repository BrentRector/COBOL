*>--- conformance-case
*> id: pb446-search-all-key-phrase
*> rules: [FMT-14.9.37.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.37.2 Format 2 (rendered, PDF page 750): SEARCH ALL identifier-1 [ AT END ... ] WHEN ...
*> - there is no KEY phrase in either SEARCH format; the key is declared by the OCCURS clause (§14.9.37.3 SR7).
*> The grammar used to admit a home-grown `KEY IS data-name` phrase that NO binder read, so the phrase was
*> accepted and silently discarded. Refused at every edition. COBOLNET2269 (kb/Work PB446).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB446SAKEY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E OCCURS 5 ASCENDING KEY K INDEXED BY IX.
              10 K PIC 9(2).
       PROCEDURE DIVISION.
           SEARCH ALL E KEY IS K AT END DISPLAY "NONE"
              WHEN K (IX) = 03 DISPLAY "HIT"
           END-SEARCH
           STOP RUN.
