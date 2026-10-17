*>--- conformance-case
*> id: pb397-exit-not-alone
*> rules: [SR-14.9.14.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB397 - ISO/IEC 1989:2023 14.9.14.3 SR1: "The EXIT statement shall appear in a sentence by itself that
*> shall be the only sentence in the paragraph or in a section without paragraphs." The rule has no edition
*> qualifier, so all four editions refuse. Here the EXIT is a sentence by itself but MAIN-PARA holds four
*> sentences; the program compiled clean and ran to completion before the rule was asked of anything.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB397NEG1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "A".
           EXIT.
           DISPLAY "B".
           STOP RUN.
