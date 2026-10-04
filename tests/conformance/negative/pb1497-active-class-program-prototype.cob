      *> reject-at: 2002 2014 2023
      *> kb/Work PB1497 -- the other half of the SR16 placement: a PROGRAM prototype is not a method definition.
      *> ISO 13.18.60.3 SR16 (cite.py --check 13.18.60.3 "The ACTIVE-CLASS phrase may be specified only in a
      *> factory definition, an instance definition, or the linkage or local-storage section of a method
      *> definition" -> OK 13.18.60.3 16)) admits an interface's method PROTOTYPE (10.6.1 NOTE: a
      *> method-definition in an interface-definition defines a method prototype) and nothing else outside a
      *> class body, so ACTIVE-CLASS in a program prototype's linkage section is still COBOLNET1924. The
      *> positive half is conformance:2002/pb1497_interface_active_class_prototype.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1497NP IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING A.
       END PROGRAM PB1497NP.
