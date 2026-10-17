*>--- conformance-case
*> id: pb866-float-edited-for-form
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB866 - the FOR form IS barred on a floating-point edited item: 13.18.40.3 SR12's
      *> FOR-phrase rules - "Extended editing sign control symbols shall not be specified for a
      *> floating-point edited item". (The IS form is legal: tests/conformance/2023/
      *> pb866_float_edited_is_form.) reject-at names 2023 only because the EDITING phrase is itself
      *> a COBOL-2023 introduction.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB866FLOATEDITEDFORFORM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FF PIC T9.9(3)E+99 EDITING T FOR NEGATIVE IS "-".
       PROCEDURE DIVISION.
           DISPLAY "X".
           STOP RUN.
