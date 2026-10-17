*>--- conformance-case
*> id: pb398-range-alphabet-class-mismatch
*> rules: [SR-14.9.13.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.13.3 SR3, sentence 2: "If literal-3 or identifier-3 is of class national, alphabet-name-1
      *> shall reference an alphabet that defines a national collating sequence; otherwise, alphabet-name-1
      *> shall reference an alphabet that defines an alphanumeric collating sequence." AL is declared by a plain
      *> ALPHABET clause, so it defines the ALPHANUMERIC sequence (12.3.6 SR1/SR2 make the two domains
      *> disjoint) and cannot order a national range. Reject-at omits 85 because class national does not exist
      *> there, so the program is refused for a different rule at that edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB398NEG5.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NB PIC N VALUE N"B".
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-NB
               WHEN N"A" THRU N"C" IN AL DISPLAY "IN"
               WHEN OTHER                DISPLAY "OUT"
           END-EVALUATE
           STOP RUN.
