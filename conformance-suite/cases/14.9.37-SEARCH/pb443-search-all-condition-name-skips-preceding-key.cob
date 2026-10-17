*>--- conformance-case
*> id: pb443-search-all-condition-name-skips-preceding-key
*> rules: [SR-14.9.37.3-11]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB443 - ISO 14.9.37.3 SR11: "When a data-name in the KEY phrase ... is referenced or when a
*> condition-name associated with a data-name in the KEY phrase ... is referenced, all preceding
*> data-names in that KEY phrase or their associated condition-names shall also be referenced." The KEY
*> phrase is ASCENDING K1 K2, so K1 is the more significant key (13.18.38.4 GR3) and a WHEN that names
*> only K2's condition-name skips it. The screen used to pick a level-88 by BASE WORD with the tie broken
*> by declaration order, ignoring the OF qualifier the condition BINDER honours, so it credited this WHEN
*> with referencing K1 and reported nothing; 8.4.2.2 Format 2 decides which 88 a reference names, and it
*> is now decided in the one place (ConditionBinder.ConditionOf).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB443CN11.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05  E OCCURS 4 TIMES
               ASCENDING KEY IS K1 K2 INDEXED BY IX.
               10  K1 PIC 99.
                   88  CN VALUE 03.
               10  K2 PIC 99.
                   88  CN VALUE 07.
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL E
               AT END CONTINUE
               WHEN CN OF K2 (IX) CONTINUE
           END-SEARCH.
           STOP RUN.
