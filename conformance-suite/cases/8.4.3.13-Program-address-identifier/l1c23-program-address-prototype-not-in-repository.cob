*>--- conformance-case
*> id: l1c23-program-address-prototype-not-in-repository
*> rules: [SR-8.4.3.13.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.4.3.13.3 3) — program-prototype-name-1 must be specified in the REPOSITORY paragraph
      *> Rule: "Program-prototype-name-1 shall be a program prototype specified in the REPOSITORY
      *>   paragraph."   cite.py: OK  §8.4.3.13.3 3)  (Syntax rules)
      *> L1C23G has no REPOSITORY paragraph.  L1C23H is a real outermost program of the same
      *> compilation unit, so the bare word is a program-name, but it is not a program prototype
      *> specified in the REPOSITORY paragraph (SR3), and it is not a data item either (so the
      *> identifier-1 arm cannot take it).  The same word WITH the REPOSITORY entry is the positive
      *> arm 4 of conformance:cases/8.4.3.13-Program-address-identifier/pb549_program_address_identifier.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C23G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET PP TO ADDRESS OF PROGRAM L1C23H
           STOP RUN.
       END PROGRAM L1C23G.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C23H.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM L1C23H.
