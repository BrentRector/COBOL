*>--- conformance-case
*> id: pb206-value-oversize-elementary
*> rules: [SR-13.18.63.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR4, sentence 2: "Alphanumeric literals in the VALUE clause of an elementary item shall
      *> not exceed the size indicated by an explicit PICTURE clause."  E1's PICTURE indicates two character
      *> positions and "ABCD" is four.
      *> THE SIBLING kb/Work PB206's note did not claim, found by re-probing the mechanism on 1d949007: the
      *> ELEMENTARY sentence of the same rule was unenforced too, and silently truncated - `DISPLAY E1` printed
      *> AB.  Its national (SR5 sentence 2) and boolean (SR10 sentence 2) twins were both implemented, which is
      *> the two-arm-dispatch shape with the third arm missing; all three now come from ONE screen
      *> (DataBinder.ValidateValueCategory), measured against ONE size - the one its ValueSubject
      *> descriptor carries (kb/Work PB598).  THIS FIXTURE IS HALF THE DRIFT PROOF for that
      *> descriptor: sentence 2 names AN ELEMENTARY ITEM, so withholding the size from a
      *> CONDITION-NAME subject - which no sentence of SR4 names - must not withhold it here.
      *> The other half is 85/pb598_condition_name_value_size, which must stay green.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB206N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC X(2) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY E1
           STOP RUN.
