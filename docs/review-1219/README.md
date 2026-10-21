# Hebrew map region details — review evidence for #1219 item 4

Before: `cae8ba80be33f5f2e30944bf5a3374d86f8afdbc`. After: `507348f9573b1e3bd44c9b65b7649f0939b0dfd8`.

The changed area is the region/armistice text in the **bottom-left info panel**, and the older layer caption shown
at the **upper left** of this fixture. These are original, unedited Unity pixels. Five selections/dates, two languages
and two target sizes give 20 matched pairs. All ten English pairs are byte-identical. The state/text files record the
same date, selected hex, language and resolution for each pair; the new Hebrew details keep each agreement's own date.

These are explicit **native UI fixtures**, using the shipped UI, styles, presenters and real map data. The fixture first
lets the real HUD choose its responsive layout, then hides unrelated elements and uses a flat background. The complete
info panel must be visible, stay inside the target frame, and contain its measured text. The legacy layer caption is
positioned at the top left for review. This is not ordinary gameplay, browser, actual-phone or map-camera acceptance.
Normal GameRoot uses its control-period caption where available, so the older caption is a separate fallback-path proof.

The standalone change keeps dates unchanged; the localized-date work belongs to #1167. New Hebrew wording remains
`agent-checked` for the native review in #564. No historical, sensitivity, rights or personal-licence approval is implied.

| Hebrew fixture | Before | After |
|---|---|---|
| subdistrict, 1920x1080 | [Original PNG](before/subdistrict-he-1920x1080.png) · [state/text](before/subdistrict-he-1920x1080.txt) | [Original PNG](after/subdistrict-he-1920x1080.png) · [state/text](after/subdistrict-he-1920x1080.txt) |
| subdistrict, 390x844 | [Original PNG](before/subdistrict-he-390x844.png) · [state/text](before/subdistrict-he-390x844.txt) | [Original PNG](after/subdistrict-he-390x844.png) · [state/text](after/subdistrict-he-390x844.txt) |
| auja, 1920x1080 | [Original PNG](before/auja-he-1920x1080.png) · [state/text](before/auja-he-1920x1080.txt) | [Original PNG](after/auja-he-1920x1080.png) · [state/text](after/auja-he-1920x1080.txt) |
| auja, 390x844 | [Original PNG](before/auja-he-390x844.png) · [state/text](before/auja-he-390x844.txt) | [Original PNG](after/auja-he-390x844.png) · [state/text](after/auja-he-390x844.txt) |
| lebanon, 1920x1080 | [Original PNG](before/lebanon-he-1920x1080.png) · [state/text](before/lebanon-he-1920x1080.txt) | [Original PNG](after/lebanon-he-1920x1080.png) · [state/text](after/lebanon-he-1920x1080.txt) |
| lebanon, 390x844 | [Original PNG](before/lebanon-he-390x844.png) · [state/text](before/lebanon-he-390x844.txt) | [Original PNG](after/lebanon-he-390x844.png) · [state/text](after/lebanon-he-390x844.txt) |
| jordan, 1920x1080 | [Original PNG](before/jordan-he-1920x1080.png) · [state/text](before/jordan-he-1920x1080.txt) | [Original PNG](after/jordan-he-1920x1080.png) · [state/text](after/jordan-he-1920x1080.txt) |
| jordan, 390x844 | [Original PNG](before/jordan-he-390x844.png) · [state/text](before/jordan-he-390x844.txt) | [Original PNG](after/jordan-he-390x844.png) · [state/text](after/jordan-he-390x844.txt) |
| modus-vivendi, 1920x1080 | [Original PNG](before/modus-vivendi-he-1920x1080.png) · [state/text](before/modus-vivendi-he-1920x1080.txt) | [Original PNG](after/modus-vivendi-he-1920x1080.png) · [state/text](after/modus-vivendi-he-1920x1080.txt) |
| modus-vivendi, 390x844 | [Original PNG](before/modus-vivendi-he-390x844.png) · [state/text](before/modus-vivendi-he-390x844.txt) | [Original PNG](after/modus-vivendi-he-390x844.png) · [state/text](after/modus-vivendi-he-390x844.txt) |

All English controls are in the same `before/` and `after/` folders (`*-en-*.png`).
[Hashes and native result counts](provenance.json) · [Exact capture fixture source](RegionHebrewVisualProof.cs).

The screenshots live only on this evidence branch, not in the product PR. The first private capture attempts (a loading
class hid the panel; then a fixture initialization error missed its responsive tier) were retained as failed evidence,
corrected and rechecked. Only the final visible/contained captures appear here.
