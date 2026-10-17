*>--- conformance-case
*> id: l1-display-pointer-operand
*> rules: [SR-13.18.60.3-9, SR-14.9.11.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.3 SR1 — "Identifier-1 shall not reference a data item of class message-tag, object,
      *> or pointer."
      *> (cite.py --check 14.9.11.3 "Identifier-1 shall not reference a data item of class message-tag,
      *> object, or pointer." -> OK, §14.9.11.3 rule 1.)
      *>
      *> DERIVED BEFORE MEASURING. "Class pointer" in this standard spans THREE categories, not one:
      *> §8.4.3.11.4 GR1 (data-pointer), §8.4.3.12.4 GR1 (function-pointer) and §8.4.3.13.4 GR1
      *> (program-pointer) each say "of class pointer and category …". A USAGE POINTER item is category
      *> data-pointer (§13.18.60 / §8.5.2.6), therefore class pointer, therefore not a legal identifier-1.
      *> So this program shall be REJECTED at every edition where USAGE POINTER exists (2002+; at 85 the
      *> usage-pointer-2002 edition gate rejects the data description entry instead, which is a different
      *> rule's diagnostic — hence reject-at names 2002 2014 2023).
      *>
      *> WHY THIS FIXTURE EXISTS. The rule was UNENFORCED: OperandText.FieldAsString's terminal
      *> `p.Item.Pic switch` enumerated Alphanumeric / NumericEdited / National / Boolean / Numeric / float
      *> and fell through PicCategory.Pointer to `_ => "…ToString()"`, so `DISPLAY WS-PTR` compiled clean
      *> and printed a CLR ManagedPointer carrier — a value with no COBOL meaning. kb/Work PB148 put the
      *> screen on the ONE classifier (IntrinsicArgumentRules.ClassOf) as a reusable operand-CLASS gate, so
      *> all three pointer categories and class object reject through the same COBOLNET1694, and the
      *> sibling fixtures l1-display-program-pointer-operand and l1-display-object-reference-operand pin
      *> the two categories a USAGE-POINTER-only special case would have left printing CLR text.
      *> The ADMIT side is conformance:cases/14.9.11-DISPLAY/l1_display_admitted_classes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DSPPTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-PTR USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY WS-PTR
           STOP RUN.
