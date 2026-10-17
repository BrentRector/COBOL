*>--- conformance-case
*> id: pb399-evaluate-range-class-mix
*> rules: [GR-8.3.3.2.4-2, SR-14.9.13.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.13.3 SR4: "The two operands in a range-expression shall be of the same class and shall
      *> not be of class boolean, message-tag, object, or pointer."  `"A"` is an alphanumeric literal, so of
      *> the class and category alphanumeric (§8.3.3.2); `5` is a numeric literal, so of the class and
      *> category numeric (§8.3.3.3).  Two classes, one range-expression.
      *>
      *> Nothing between the parse and the emit used to ask what class a range's operands were: both ends
      *> went straight to the operand binder and then to `left >= lo AND left <= hi` (kb/Work PB399).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB399RCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE WS-A
               WHEN "A" THRU 5
                   DISPLAY "MATCHED"
               WHEN OTHER
                   DISPLAY "OTHER"
           END-EVALUATE.
           STOP RUN.
