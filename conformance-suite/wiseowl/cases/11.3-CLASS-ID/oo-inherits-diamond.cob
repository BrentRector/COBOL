*>--- conformance-case
*> id: oo-inherits-diamond
*> rules: [GR-11.3.4-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO 11.3.4 GR4's OWN subject - the DIAMOND: "If the same class is inherited more than once, then only
*> one copy of the data for that class is added to object-class-name-1." The rule's own NOTE says the
*> repeat can only be INDIRECT ("While the same class cannot be directly inherited more than once, a class
*> can be indirectly inherited multiple times"), which needs some class to carry two DIRECT bases - and
*> that is exactly what Annex A.4.10 item 1 declines, so the single-copy question never arises.
       IDENTIFICATION DIVISION.
       CLASS-ID. DIAMD INHERITS FROM DIAMB DIAMC.
       END CLASS DIAMD.
       IDENTIFICATION DIVISION.
       CLASS-ID. DIAMB INHERITS FROM DIAMA.
       END CLASS DIAMB.
       IDENTIFICATION DIVISION.
       CLASS-ID. DIAMC INHERITS FROM DIAMA.
       END CLASS DIAMC.
       IDENTIFICATION DIVISION.
       CLASS-ID. DIAMA.
       END CLASS DIAMA.
