## Entry 1395 — 2026-08-29 17:52 PDT — PB164 wave 2: the FLOAT IEEE byte-form pin lands WHOLE — the endianness axis honored end to end, the float SORT key algebraic instead of staged, and the citation split between FORMAT and BYTE ORDER

The wave-2 implementation (drafted last session, probed green in the dirty tree) admitted the float family
to the record image: NumericByteForm.Ieee32/Ieee64, the bit-reinterpreting FormatImageFloat/ParseImageFloat
lanes (distinctly named — FormatImage overloads on float made every integer call site CS0121-ambiguous),
the codec's float arms, HasImageByteForm shedding its interim !IsFloat conjunct, and the profile-emission
filter's IsFloat:false — one more drifted copy of the retired union, caught because a float leaf's group
referenced a _P_n the emitter never declared. 2023/pb164_float_group_image round-trips COMP-1/COMP-2
bit-exact through an alphanumeric intermediary and writes through CALL BY REFERENCE.

This session closed the wave under the owner's directive: MAKE THE LONG-TERM SUPPORTABILITY CHANGE. Two
recorded plans got superseded by better shapes the directive demanded:

THE ENDIANNESS FINDING became a feature, not a citation patch. The fleet was right that my GR14/GR15
citations pinned the wrong thing — those rules pin the interchange FORMAT; byte order is §13.18.60.4 GR19
via §11.9.8 for the STANDARD usages. But the resume note's own parenthetical ("SR3 implies HIGH-ORDER-RIGHT
default!") was ALSO wrong — re-deriving §11.9.8 showed SR1 makes the no-clause default the implementor's
REQUIRED-documented choice (A.1 item 48), and SR3 only fires when the clause says HIGH-ORDER-RIGHT. So the
landing: our documented default is HIGH-ORDER-LEFT (CONFORMANCE.md row 48), and a specified
FLOAT-BINARY DEFAULT IS HIGH-ORDER-RIGHT is HONORED — PicInfo.FloatItem(usage, Options.FloatBinaryEndianness)
is the ONE application of the implied-phrase rule (standard usages only, GR19c; COMP-1/-2/FLOAT-SHORT/
-LONG/-EXTENDED stay GR13/GR21 implementor big-endian), riding ProfileInitializer into
NumProfile.FloatLittleEndian, the one switch the lanes read. OptionsModel.FloatBinaryEndianness had been
bound with ZERO consumers; now it has exactly one, in the right place — and when the per-item
endianness-phrase lands in the grammar (it is MISSING, rejecting legal 2014+ source — registered PB174),
it merges into the same factory argument. 2023/pb164_float_hor_image proves the plumbing with FUNCTION ORD
byte reads, because a round-trip golden is SELF-INVERSE and cannot see byte order. Inventory FMT-11.9.8.2 +
SR-11.9.8.3-1/-2/-3 flip to CONFORMS: GAP 3663 -> 3659.

THE SORT FLOAT KEY landed WHOLE instead of the recorded stage-loud. The fleet's catch was real — a COMP-2
key, newly bindable, compared as CHARACTERS (big-endian IEEE bytes: every negative after every positive) —
but the recorded decision's premise ("the NumericKey column cannot be flipped on") dissolved on contact
with the code: CobolSort.KeyColumns.Build now dispatches on the profile's ByteForm, an Ieee key decodes
through ParseImageFloat into a double[] column, and comparison is algebraic per §14.9.40.4 GR8 ->
§8.8.4.2.4. SortBinder drops IsFloat:false. The complete feature was SMALLER than the stage would have
been (descriptor + negative fixture + residue), which is the supportability directive in one sentence.
2023/pb164_float_sort_key releases negatives on both sides of zero out of order and pins the order.

The rest of the closing checklist: FloatImageLaneTests pins the lanes at BYTE level against the standard
(3F C0 00 00 for 1.5f; C0 02 00 00 00 00 00 00 for -2.25; the little-endian reversals as literal bytes;
the wave-1 unsigned ulong lane's bytes) — the fleet's self-inverse-golden finding discharged. The
drift table re-derived (Float..FloatBinary64 -> Ieee32/Ieee64; FloatBinary128/decimals stay None behind
COBOLNET1564) with a new theory pinning that the endianness axis reaches ONLY standard-usage profiles.
ONE-WIDTH enumerates the float family; ElementaryByteWidth's duplicated 4/8 literals delegate to
PicInfo.StorageWidth like their binary siblings. The stale-wording sweep hit eleven comment sites plus two
user-facing strings — every 'float/COMP-5/INDEX' island formulation now blames USAGE INDEX alone, and
OoConformance's two hand-rolled island strings (stale twice over — exactly why hand-rolled copies are the
defect) now call TierCIsland.Reason.

Wave-local gate GREEN: Conformance 1739, Unit 5027, characterization 33; the corpus population lists all
four pb164 goldens by name, and both new goldens matched spec-derived expectations on their first run —
the ORD byte reads WOULD have printed [64 1] had the flag been ignored. Inventory reference gate 10/10.
The island lock sits at its final boundary: USAGE INDEX, pending the owner decision. Remaining in PB164:
the mixed-usage REDEFINES codec, the INDEX decision, the variable-length-group display format. Battery
#35 accrues (PB157 + PB164 w1 + w2). Owner directive noted mid-session: 97% of weekly quota — pausing
after this landing; the queued INDEX owner question waits for the next session.

