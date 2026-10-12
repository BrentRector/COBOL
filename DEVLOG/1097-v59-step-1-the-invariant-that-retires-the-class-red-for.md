## Entry 1097 — 2026-07-28 23:40 PDT — V59 step 1: the invariant that retires the class, red for exactly the defect

The maintenance tie-breaker says the prize is not fixing the instance. So V59 starts with the test that makes the
whole class impossible, before any of the representation work.

**The invariant.** An item that participates in the record/group character image must occupy THE SAME number of
positions there as it occupies in storage: `DataItem.ImageWidth` and `DataItem.ByteWidth` are two views of one
fact, never two answers. Every exclusion is principled rather than convenient — NATIONAL is excluded because it
is deliberately 2 bytes per character position, which the test ASSERTS (exactly 2x) rather than skips; BOOLEAN
because its unit is a boolean position, not a character position (§15.50.4 r1); and anything not
`IsImageCapable` because it never reaches an image at all (float / COMP-5 / INDEX, the loud Tier-C island) and so
cannot contradict one.

**It is red for exactly the defect, which is the part worth checking.** 39 of 83 cases fail, and the pass/fail
split is not arbitrary: the passing BINARY/PACKED rows are precisely those where the pinned width and the digit
count COINCIDE (BINARY `PIC 9`, PACKED `PIC 9` and `PIC 99`). Every other row fails by exactly the difference
between `StorageWidth` and `Pic.Digits`. A test that went red for a sloppier reason would have been worth less
than no test.

**What I did NOT do, and why it is recorded as a Skip rather than left out.** The rest of V59 — a storage-form
discriminator on `NumProfile` (the existing `NumericTruncation` cannot serve: DISPLAY and BINARY are both
`DigitCount`), the radix-2/BCD codec, re-basing the image width, the 129 `ImageWidth` call sites, the
file-length support diagnostic, the §4.2.16 documentation and byte-level goldens — is more than could land
cleanly in the time left, and a half-applied storage-model refactor is worse than none.

The two failing facts carry an explicit `Skip` naming V59, the plan item and the exact defect. That is NOT a
tolerated divergence and not a new deferral: the owner decided this change today, it is plan §0 NEXT 1, and the
Skip text says so. The alternative — keeping the test out of the tree until the fix lands — is how this defect
survived two phases in the first place. It turns green the moment the image is re-based on `StorageWidth`, and
until then it is a labelled, unmissable statement of what is wrong.

Gates: greenfield Unit 596 passed / 2 skipped / 598 · characterization 33/33.

