*>--- conformance-case
*> id: pb389-only-receiver-active-class-sender
*> rules: [SR-14.9.39.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 12 b)1.: with identifier-3 described with an
      *> object-class-name and identifier-4 "an object reference described with an
      *> ACTIVE-CLASS phrase", "the data item referenced by identifier-3 shall not be
      *> described with the ONLY phrase."  §13.18.60.4 GR22 e) is why: an ACTIVE-CLASS
      *> reference holds an object "of the same class as the object that was used to invoke
      *> the method", which may be a SUBCLASS of the containing class, and GR22 d)2.b. lets
      *> an ONLY receiver hold only an instance of the named class exactly.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB389N6.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB389N6.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB389N6C.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 CO USAGE OBJECT REFERENCE PB389N6C ONLY.
       01 A  USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION.
       MAIN.
           SET A TO SELF.
           SET CO TO A.
       END METHOD MK.
       END OBJECT.
       END CLASS PB389N6C.
