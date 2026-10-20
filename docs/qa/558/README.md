# Localized armistice dates: bounded EN/HE WebGL QA for #558

The info panel now uses the existing selected-language calendar formatter and isolates a Hebrew date inside an English fallback row with Unicode RLI/PDI. Product tested: `fa13bfda7c25fe5ad95a29a86738e92e184ffbae`, based on upstream `2d3e13c098e147493ab034ab2ff20f0b04765fab`. This evidence branch does not change product source beyond that product commit. Captures: 20 October 2026, Unity 6000.3.25f1 WebGL, fresh isolated headless Chrome 155.0.8059.80 profiles, 1280x720, DPR 1. They are unmodified PNGs.

| Observation | Capture |
|---|---|
| Initial localized-only 5127440f: Hebrew date has the wrong visual day/year/month order; **failed visual acceptance**, retained for comparison | [HE before isolation](region-dates-he-armistice-date.png) |
| Final fa13bfda: Hebrew date reads 24 בפברואר 1949 in the correct RTL order, with no visible isolation glyph | [HE after isolation](region-dates-v2-he-armistice-date.png) |
| Final fa13bfda: English still reads 24 February 1949 | [EN unchanged](region-dates-v2-en-armistice-date.png) |

Agreement and zone names retain their existing English fallback; this does not certify all Hebrew names/prose or complete #558.

## Reproduction

Use the existing `?smoke=10&navlog&camlog&lang=en` driver, then a separate `he` profile. Wait for the actual `SMOKE OK 10 turns 1949-03-15` console event. Hide the pending card. With the map visible, 21 real wheel-in inputs at (640,401), deltaY=-200 each, set the camera distance to 13130. Click the minimap at (1143,616), then the map at (640,401) to select El Auja's zone. Capture. The precise 29-action sequences and console-audited results are in [commands.json](commands.json). The test driver reaches the required March 1949 date; this is not a manual title-to-turn-10 playthrough. No `Runtime.evaluate`, `Runtime.callFunctionOn` or game-state injection was used. All 102 private player files were rehashed against their manifest before either session.

Both final sessions had zero failed input/capture predicates, zero JavaScript exceptions, zero error-level messages and orderly closure. Each emitted six `WebGL: INVALID_ENUM: getInternalformatParameter: invalid internalformat` rendering warnings, retained in the audit; these were not warning-free browser runs. Exact hashes and product attribution: [manifest.json](manifest.json). The earlier 512 PNG is not relabelled as a successful final test.

The product PR records the separate red-first text regressions, full repository check, unfiltered native EditMode totals/XML hash and all-eight-golden plus smoke 5 gates on the final product SHA. Native and player results belong to the product SHA, not to this documentation commit. No new translation, historical check, human/native-reader sign-off, screen-reader/Safari/physical-device acceptance or release approval is claimed. Existing date strings and source/review flags are unchanged. The isolation mechanism follows [Unicode UAX9](https://www.unicode.org/reports/tr9/); engine behavior was verified by the actual screenshots rather than inferred from the standard.

## Provenance and licence

Screenshots: **CC BY-SA 4.0**, credit **Statecraft: Israel contributors**, following D13 and the existing screenshot/map-bake ruling. [Licence](../../../LICENSES/CC-BY-SA-4.0.txt). Existing map models: **Tal Heuberger**, CC0. No event photograph, title painting, Cabinet portrait or video is included in this published subset. Initial card captures remain local. Existing [NOTICE](../../../NOTICE), asset records and font terms retain their terms. Terrain: produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved. The private player is not published as a release build. Capture provenance checked by the agent on 20 October 2026; no human signature is added.

AI assistance: Codex wrote the focused fix/tests, drove actual browser input, inspected the images, audited raw CDP errors/methods/hashes and wrote this evidence report.

Rules check:
1. Exact product/source SHA, original images, input, output and exit codes verified.
2. Confidence 10/10 for the bounded date observation; broader #558 remains incomplete.
3. Mixed-direction/date-order, English drift, attribution/privacy and licensing checked.
4. Evidence-only fork branch; no rule, source signature, release gate or personal licence-checkbox change.
