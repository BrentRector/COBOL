*>--- conformance-case
*> id: pb389-only-receiver-self-sender
*> rules: [SR-8.4.3.8.3-7, SR-14.9.39.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 12 c)1.: with identifier-3 described with an
      *> object-class-name and identifier-4 "the predefined object reference SELF", "the data
      *> item referenced by identifier-3 shall not be described with the ONLY phrase."  SELF
      *> is the object that the containing method was invoked on (§8.4.3.8), whose class may
      *> be a SUBCLASS of the class the method is written in — exactly what an ONLY receiver
      *> may not hold (§13.18.60.4 GR22 d)2.b.).  c)2.'s subclass leg WITHOUT ONLY is
      *> exercised positively in tests/conformance/2002/pb389_object_reference_descriptor.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB389N8.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB389N8.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB389N8C.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 CO USAGE OBJECT REFERENCE PB389N8C ONLY.
       PROCEDURE DIVISION.
       MAIN.
           SET CO TO SELF.
       END METHOD MK.
       END OBJECT.
       END CLASS PB389N8C.
