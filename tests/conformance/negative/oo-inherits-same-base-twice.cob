*> reject-at: 2002 2014 2023
*> ISO 11.3.3 SR7's OWN subject: "A given class name shall not appear more than once in an INHERITS
*> clause." The rule is asked on the written names BEFORE the declined Annex A.4.10 item 1 (multiple
*> inheritance), which counts DISTINCT names (kb/Work PB1020) - one class written twice is the SR7 violation,
*> not a use of multiple inheritance. oo-multi-base-inherits keeps two DISTINCT names (COBOLNET0849).
       IDENTIFICATION DIVISION.
       CLASS-ID. MBDUP INHERITS FROM MBBASEA MBBASEA.
       END CLASS MBDUP.
       IDENTIFICATION DIVISION.
       CLASS-ID. MBBASEA.
       END CLASS MBBASEA.
