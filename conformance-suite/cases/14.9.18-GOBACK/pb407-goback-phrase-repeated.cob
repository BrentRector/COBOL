*>--- conformance-case
*> id: pb407-goback-phrase-repeated
*> rules: [FMT-14.9.18.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB407. ISO 5.2.6.4: "Choice indicators are a pair of bars, |, that enclose a portion of a
      *> general format. When enclosed by braces, one or more of the alternatives contained within the choice
      *> indicators shall be specified, but any single alternative may be specified only once", and the
      *> bracketed form admits ZERO or more on the same terms. 14.9.18.2's tail bracket carries those bars,
      *> so BOTH phrases in EITHER order is legal - and each of them TWICE is not. Rejecting the doubled
      *> phrase is what keeps the relaxation from being an over-acceptance.
      *> 2023 only, because the status phrase written twice here is a COBOL-2023 introduction
      *> (Annex E.3.3 item 32) and below that edition the program is refused for the edition instead.
      *> POSITIVE CONTROL: conformance-suite/cases/14.9.14-EXIT/pb407_goback_general_format.cob - both phrases, each once,
      *> in either order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB407.
       PROCEDURE DIVISION.
       MAIN-PARA.
           GOBACK WITH ERROR STATUS 5 WITH NORMAL STATUS 6.
