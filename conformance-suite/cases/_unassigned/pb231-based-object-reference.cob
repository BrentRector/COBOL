      *>--- conformance-case
      *> id: pb231-based-object-reference
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.5.3 SR1: "The subject of the entry shall not be of
      *> class object."
      *> kb/Work PB231 (the pointer third): the ASYMMETRY this fixture
      *> pins is the standard's own. The BASED rule bars ONLY class
      *> object, so `01 P USAGE POINTER BASED.` - class pointer - is
      *> legal source and is exactly what the pointer third implements
      *> (conformance:cases/14.9.3-ALLOCATE/pb231_based_pointer_leaf), while
      *> 13.18.22.3 SR4 bars BOTH classes from the EXTERNAL clause
      *> (conformance:cases/13.18.22-EXTERNAL/pb231-external-pointer-item). Until the
      *> pointer third, one residue diagnostic (COBOLNET1695) rejected
      *> all three shapes for one wrong reason; each now draws the rule
      *> that is actually about it, and this fixture is what stops the
      *> object-reference one from becoming an under-rejection.
      *> 13.18.60.4 GR22: "A data item described with a USAGE OBJECT
      *> REFERENCE clause is called an object reference. An object
      *> reference is a data item of class object and category
      *> object-reference." 13.18.60.3 SR14 admits that usage "only for
      *> an elementary data item at level 1 or an elementary data item
      *> subordinate to a type declaration that includes the STRONG
      *> phrase", so the level-1 spelling below is the one that reaches
      *> the BASED clause at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB231BASO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE BASED.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
