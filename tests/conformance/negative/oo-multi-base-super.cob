*> reject-at: 2002 2014 2023
*> ISO 8.4.3.8.3 SR5's OWN subject: "If the INHERITS clause of the containing class definition specifies
*> more than one object-class-name, object-class-name-1 shall be specified" - i.e. a bare SUPER is illegal
*> once a class has two bases. Annex A.4.10 item 1 declines the two-base clause, so the SR is unreachable.
*> The method body uses SUPER so the subject is actually present in the source.
*> The single-INHERITS "object-class-name-1 OF SUPER" prefix (SR4/SR6, GR4) is implemented (kb/Work PB1425:
*> 2002/pb1425_qualified_super, negative pb1425-super-qualifier-not-inherited); only SR5's multi-base subject is
*> unreachable.
       IDENTIFICATION DIVISION.
       CLASS-ID. MBSUP INHERITS FROM MBSBASEA MBSBASEB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GO-UP.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE SUPER "SHOW".
       END METHOD GO-UP.
       END OBJECT.
       END CLASS MBSUP.
       IDENTIFICATION DIVISION.
       CLASS-ID. MBSBASEA.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "A".
       END METHOD SHOW.
       END OBJECT.
       END CLASS MBSBASEA.
       IDENTIFICATION DIVISION.
       CLASS-ID. MBSBASEB.
       END CLASS MBSBASEB.
