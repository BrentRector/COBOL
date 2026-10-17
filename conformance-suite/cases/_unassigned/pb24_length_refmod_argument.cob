*>--- conformance-case
*> id: pb24_length_refmod_argument
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> A REFERENCE-MODIFIED ARGUMENT IS LEGAL FOR FUNCTION LENGTH, AND ITS LENGTH IS THE MODIFIED LENGTH.
      *> 15.50.3 rule 1 admits "a data item of any class or category" as argument-1, and 8.4.3.3.3 makes a
      *> reference-modified item a data item — so every line below is conforming source. 15.50.4 rules 2/3 then
      *> make the returned value the length of THAT item, not of the item it was modified from.
      *>
      *> ⛔ THIS GOLDEN EXISTS BECAUSE ALL THREE FORMS COMPILED CLEAN AND THREW AT RUN TIME (fix-queue PB24).
      *> BindLengthFold's RefModPlace arm returned a BoundExprError, which renders as a NotImplemented guard:
      *>     Unhandled exception. NotImplementedCobolFeatureException: FUNCTION LENGTH of a
      *>     reference-modified argument (runtime length, 15.50.4)          -> exit 127
      *> That is the PB7/DA7 WRONG-STAGE family — a verdict delivered at run time that belongs at compile time,
      *> except here there is no verdict to deliver at all: the source is legal and the answer is computable.
      *>
      *> ⚠ THE FIX ADDED NO MACHINERY, AND THE THREE FORMS ARE WHY IT DID NOT NEED ANY. IntrinsicRenderer's
      *> Length arm renders its argument through the ONE string channel, and a reference-modified place renders
      *> as the SUBSTRING — so the runtime length over that image is already 15.50.4's character-position count.
      *> The literal form, the runtime-bounds form and the omitted-length form therefore all fall out together,
      *> which is why all three are pinned here rather than just the one the defect was reported against.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB24LENRM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NAME PIC X(20) VALUE "HELLO WORLD".
       01 I  PIC 9(2) VALUE 3.
       01 L  PIC 9(2) VALUE 6.
       01 R  PIC 9(4).
       PROCEDURE DIVISION.
      *> The control: the unmodified item still folds to its declared width at compile time.
           COMPUTE R = FUNCTION LENGTH(WS-NAME)
           DISPLAY "WHOLE=" R
      *> Literal bounds — the form the defect was reported against.
           COMPUTE R = FUNCTION LENGTH(WS-NAME(1:5))
           DISPLAY "LIT=" R
      *> RUNTIME bounds: neither position nor length is known at compile time (8.4.3.3.4 item 5b/5c).
           COMPUTE R = FUNCTION LENGTH(WS-NAME(I:L))
           DISPLAY "RUNTIME=" R
      *> The omitted-length "to the end" form: 20 - 15 + 1 = 6.
           COMPUTE R = FUNCTION LENGTH(WS-NAME(15:))
           DISPLAY "TOEND=" R
      *> ⚠ A ref-modified length must not leak the ORIGINAL width — the defect this replaces would have, had it
      *> folded rather than thrown. One position is the smallest answer that is still a data item.
           COMPUTE R = FUNCTION LENGTH(WS-NAME(20:1))
           DISPLAY "ONE=" R
           STOP RUN.
