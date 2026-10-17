*>--- conformance-case
*> id: l1-display-object-reference-operand
*> rules: [SR-14.9.11.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.3 SR1 — "Identifier-1 shall not reference a data item of class message-tag, object,
      *> or pointer."
      *> (cite.py --check 14.9.11.3 "Identifier-1 shall not reference a data item of class message-tag,
      *> object, or pointer." -> OK, §14.9.11.3 rule 1.)
      *>
      *> DERIVED BEFORE MEASURING. §8.5.2.1 Table 2 puts category object-reference in CLASS OBJECT, the
      *> second of the three classes SR1 names, so a USAGE OBJECT REFERENCE item is not a legal
      *> identifier-1 and this program shall be REJECTED at 2002+ (usage-object-reference-2002).
      *> Before kb/Work PB148 it compiled clean and printed the CLR object's ToString().
      *>
      *> THE THIRD CLASS SR1 NAMES IS VACUOUS HERE AND IS NOT COUNTED AS PASSING: class message-tag belongs
      *> to the message control system, which this compiler does not model at all (there is no Usage member
      *> to construct one from — IntrinsicArgumentRules states the deliberate absence), so no message-tag
      *> operand can be written. That arm is satisfied by ABSENCE, not by a check, and is recorded as such
      *> on the inventory row rather than claimed as tested.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DSPOBJ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-OB USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY WS-OB
           STOP RUN.
