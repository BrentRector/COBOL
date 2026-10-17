      *>--- conformance-case
      *> id: l1c21_method_name_signature
      *> rules: [GR-9.3.5.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.3.5.3 item 1 — the method NAME is part of the signature
      *> "The method resolution signature consists of: 1) The method
      *> name"
      *>   cite.py --check 9.3.5.3 "The method name" -> OK  §9.3.5.3 1)
      *>   (Parametric polymorphism)
      *> The mandatory rule that consumes the signature, §8.4.6.5: "The
      *> methods declared in an object class definition shall have
      *> unique method resolution signatures within that object class
      *> definition."
      *>   cite.py --check 8.4.6.5 -> OK  §8.4.6.5   (Scope of
      *>   method-names)
      *> L1C21Q's factory declares M1 and M2 whose signatures agree in
      *> EVERY component except item 1 (same calling convention, no
      *> DECIMAL-POINT/CURRENCY/LOCALE clause, one USING parameter
      *> described PIC 9, no returning item). Because the name is a
      *> component, the two signatures are distinct: the class is legal
      *> under §8.4.6.5, and method resolution selects by the name the
      *> INVOKE supplies (§14.9.23.3 SR3: with object-class-name-1 the
      *> method is named by literal-1, "the name of a method defined in
      *> the factory interface of object-class-name-1"; cite.py --check
      *> 14.9.23.3 -> OK  §14.9.23.3 3)). (No overloading — optional
      *> parametric polymorphism, not claimed — is involved: the names
      *> differ.)
      *> Expected output, derived:
      *>   "M2 GOT 2" — literal "M2" resolves to M2 (not the first
      *>                declared M1), which displays its argument 2.
      *>   "M1 GOT 1" — literal "M1" resolves to M1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C21Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N     PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 2 TO N
           INVOKE L1C21Q "M2" USING N
           MOVE 1 TO N
           INVOKE L1C21Q "M1" USING N
           STOP RUN.
       END PROGRAM L1C21M.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C21Q.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS-X PIC 9.
       PROCEDURE DIVISION USING LS-X.
       MAIN-PARA.
           DISPLAY "M1 GOT " LS-X.
       END METHOD M1.
       METHOD-ID. M2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS-X PIC 9.
       PROCEDURE DIVISION USING LS-X.
       MAIN-PARA.
           DISPLAY "M2 GOT " LS-X.
       END METHOD M2.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS L1C21Q.
