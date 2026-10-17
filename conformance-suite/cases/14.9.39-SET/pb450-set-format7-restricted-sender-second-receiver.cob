*>--- conformance-case
*> id: pb450-set-format7-restricted-sender-second-receiver
*> rules: [SR-14.9.39.3-19]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB450 - ISO 14.9.39.3 SR19's THIRD sentence over the SECOND receiving operand of a Format-7
      *> statement whose identifier-6 is a PLAIN restricted data-pointer ITEM.
      *> "If identifier-6 references a restricted data-pointer, either identifier-5 shall reference a
      *> data-pointer restricted to the same type or data-name-1 shall be a typed item of the type to which
      *> identifier-6 is restricted."  (python scripts/spec/cite.py --check 14.9.39.3 "If identifier-6
      *> references a restricted data-pointer, either identifier-5 shall reference a data-pointer restricted to
      *> the same type or data-name-1 shall be a typed item of the type to which identifier-6 is restricted"
      *> -> OK 14.9.39.3 19))
      *> !! IT IS SR19, NOT SR20 - RENDERED, folio 736 / PDF p766. The sentence is an UNNUMBERED continuation
      *> paragraph of 19); 20) is FORMAT 8's "Identifier-12 shall reference a data item of category
      *> function-pointer ..." (cite.py --check 14.9.39.3 "identifier-12" -> OK 20)). Six code sites and two
      *> goldens carried the inherited SR20, including the user-visible COBOLNET0869 text.
      *> !! THE CELL THIS PINS COULD NOT BE WRITTEN BEFORE kb/Work PB450 half 2. identifier-6's restriction
      *> reached this screen only through an `ADDRESS OF x` sender (8.4.3.11.4 GR2, Annex D.9.2.2 source 2),
      *> because the two fixed productions split on the SENDER's spelling meant a plain pointer sender could
      *> never stand beside an ADDRESS OF receiver - MEASURED before the landing,
      *> `SET ADDRESS OF B1 P-PLAIN TO P-RESTRICTED` was `error COBOL0001: unexpected 'P-PLAIN'`.  Here the
      *> sender's restriction comes from D.9.2.2 source 1 instead - 13.18.60.4 GR23, "If type-name-1 is
      *> specified, this data item is a restricted data-pointer" (cite.py --check 13.18.60.4 -> OK 23)) - which
      *> is the arm the diagnostic's parenthetical now names.
      *> The FIRST receiving operand is a conforming `ADDRESS OF B1` over a TYPE TT based item, so the only
      *> thing this case can be rejected for is the SECOND one (feedback_green_gates_arent_evidence).
      *> Below 2002 the whole family draws the introduction gate instead (COBOLNET0900 on TYPEDEF), which is
      *> why reject-at names 2002 and up.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB450SR19C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-A PIC X(4).
       01 PTR-T TYPEDEF USAGE POINTER TO TT.
       01 R-INST TYPE TT.
       01 P-RESTRICTED TYPE PTR-T.
       01 P-PLAIN USAGE POINTER.
       01 B1 BASED TYPE TT.
       PROCEDURE DIVISION.
       MAIN-P.
           SET P-RESTRICTED TO ADDRESS OF R-INST
           SET ADDRESS OF B1 P-PLAIN TO P-RESTRICTED
           STOP RUN.
