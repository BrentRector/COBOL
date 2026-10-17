*>--- conformance-case
*> id: l1-display-program-pointer-operand
*> rules: [SR-13.18.60.3-8, SR-14.9.11.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.3 SR1 — "Identifier-1 shall not reference a data item of class message-tag, object,
      *> or pointer."
      *> (cite.py --check 14.9.11.3 "Identifier-1 shall not reference a data item of class message-tag,
      *> object, or pointer." -> OK, §14.9.11.3 rule 1.)
      *>
      *> DERIVED BEFORE MEASURING. §8.4.3.13.4 GR1 makes a program-address-identifier "of class pointer and
      *> category program-pointer", and §13.18.60 GR24 makes USAGE PROGRAM-POINTER a program-pointer data
      *> item. Category program-pointer is therefore CLASS POINTER, so SR1 excludes it exactly as it
      *> excludes USAGE POINTER — this is the category a fix that special-cased "USAGE POINTER" would have
      *> left printing a CLR carrier (feedback_model_the_rule_shape_not_one_case). REJECTED at 2002+
      *> (usage-program-pointer-2002; at 85 the usage gate rejects the data entry under its own rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DSPPGM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY WS-PP
           STOP RUN.
