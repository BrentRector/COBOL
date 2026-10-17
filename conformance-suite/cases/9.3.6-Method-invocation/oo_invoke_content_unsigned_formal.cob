*>--- conformance-case
*> id: oo_invoke_content_unsigned_formal
*> rules: [GR-9.3.6-L2.3, GR-14.8.2.3.3-L2.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.8.2.3.3 rule 2a: "If the formal parameter is numeric, the conformance rules are the same as
      *> for a COMPUTE statement with the argument as the sending operand and the corresponding formal
      *> parameter as the receiving operand." COMPUTE's store is the MOVE statement's §14.9.25.4 GR6d2b: "When an unsigned numeric
      *> item is the receiving item, the ABSOLUTE VALUE of the sending value is used, and no operational sign
      *> is generated for the receiving item."
      *> So BY CONTENT of a signed −7 into an UNSIGNED PIC 9(4) COMP formal delivers 7, not −7. The argument
      *> and formal agree on digits and scale and differ ONLY in sign, which is precisely the case the
      *> conversion guard used to treat as an identity: it fell through to a plain native copy and −7 arrived
      *> verbatim (every fixed-point usage projects to C# long, so nothing else de-signed it).
      *> A NEGATIVE argument is required — a positive one passes by accident either way, which is why the
      *> second call passes +7: it is the control proving the new conversion does not disturb the case that
      *> already worked. Expected output is SEVEN twice; before the fix the first line was BAD=0007 — note the
      *> displayed image has NO sign (the formal is unsigned, so the image cannot show one) while the stored
      *> value was −7 and the comparison failed. The defect was invisible to a DISPLAY and visible only to a
      *> comparison, which is why the golden tests the value rather than the rendering.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOCONT1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CCONT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CCONT.
       01 S-NEG PIC S9(4) COMP VALUE -7.
       01 S-POS PIC S9(4) COMP VALUE +7.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CCONT "NEW" RETURNING O.
           INVOKE O "TAKE" USING BY CONTENT S-NEG.
           INVOKE O "TAKE" USING BY CONTENT S-POS.
           STOP RUN.
       END PROGRAM OOCONT1.

       IDENTIFICATION DIVISION.
       CLASS-ID. CCONT INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-U PIC 9(4) COMP.
       PROCEDURE DIVISION USING LK-U.
       MAIN.
           IF LK-U = 7
               DISPLAY "SEVEN"
           ELSE
               DISPLAY "BAD=" LK-U
           END-IF.
       END METHOD TAKE.
       END OBJECT.
       END CLASS CCONT.
