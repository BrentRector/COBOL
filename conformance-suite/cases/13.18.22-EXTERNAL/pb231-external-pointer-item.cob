      *>--- conformance-case
      *> id: pb231-external-pointer-item
      *> rules: [SR-13.18.22.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.22.3 SR4: "The EXTERNAL clause shall not be
      *> specified for a data item of class object or pointer."
      *> kb/Work PB231 (the pointer third): this shape USED to be
      *> rejected - by COBOLNET1695's EXTERNAL twin, i.e. "recognized
      *> but not yet implemented", which is a different verdict about a
      *> different thing. Opening the byte-window carriage gate to
      *> pointer-class leaves would have made a NONCONFORMING program
      *> compile clean: an under-rejection created by a fix. So the rule
      *> that actually bars it is screened in the same change set, and
      *> this fixture is what keeps it screened.
      *> THE RULE NAMES THE ITEM'S OWN CLASS. 13.18.60.4 GR23 makes a
      *> USAGE POINTER entry "a data-pointer data item", and 8.5.2 puts
      *> it in class pointer - so EP is barred. A strongly-typed
      *> EXTERNAL group holding a pointer MEMBER is class <type-name>,
      *> not pointer (8.5.2.1), so SR4 does not bar it (13.18.22.3 SR5
      *> asks only that its type declaration also be external); that
      *> legal shape runs in the positive corpus, so this fixture cannot
      *> be satisfied by refusing pointers under EXTERNAL wholesale.
      *> The EXTERNAL clause is a COBOL-85 element, but USAGE POINTER
      *> is a COBOL-2002 introduction, so at --std 85 the program draws
      *> the edition gate (COBOLNET0900) instead and the reject-at list
      *> names the three editions where SR4 is the reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB231EXTP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EP USAGE POINTER EXTERNAL.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
