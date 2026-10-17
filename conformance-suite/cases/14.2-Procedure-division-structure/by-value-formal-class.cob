*>--- conformance-case
*> id: by-value-formal-class
*> rules: [SR-14.2.2-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.2.2 SR2 - a data-name in a BY VALUE phrase of the procedure division header shall
      *> be of class numeric, message-tag, object, or pointer: a PIC X (class alphanumeric) formal
      *> is rejected (COBOLNET1553).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BVFC-P10UV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC X(4).
       PROCEDURE DIVISION USING BY VALUE LX.
       MAIN.
           STOP RUN.
