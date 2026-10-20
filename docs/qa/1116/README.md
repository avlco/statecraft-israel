# Keyboard map selection: independent EN/HE browser QA for #1116

These unmodified 1280x720 PNGs record keyboard behavior from the existing private WebGL build of product commit `bd2abd9d0a297aa1f7d43d0b4a0bd81ae07645e6`, based on upstream `2d3e13c098e147493ab034ab2ff20f0b04765fab`. The implementation was merged in [PR #1134](https://github.com/eyalgolan/statecraft-israel/pull/1134); this evidence branch adds no product fix. Capture: 20 October 2026, Unity 6000.3.25f1 player, two fresh isolated headless Chrome 155.0.8059.80 profiles, DPR 1.

## Reproduction and observed behavior

Start at the real title screen (`?navlog&camlog&lang=en`, then a separate `he` profile), click New game and Hide on the initial card, then click the empty map at (640,401) to establish input focus. Press Tab nine times: the gold/dark map ring appears at the camera centre. Enter submits to `map-focus`. Hold ArrowUp for 1.2 seconds; press Space: the camera has panned north and Haifa is selected, with the ring still visible. Press Tab four times: the ring disappears and the worker-minus info button has focus. Enter submits to `city-workers-less`; the visible workers-in-work value changes from 42 to 41 and Attention from 4.0 to 3.0. The filled-jobs text remains 42; no whole-turn or economic outcome is claimed.

| Stage | EN | HE |
|---|---|---|
| Map keyboard focus | [image](keyboard-v2-en-map-focus.png) | [image](keyboard-v2-he-map-focus.png) |
| ArrowUp, then Space selects Haifa | [image](keyboard-v2-en-pan-space-selection.png) | [image](keyboard-v2-he-pan-space-selection.png) |
| Available info action focused | [image](keyboard-v2-en-info-action-focus.png) | [image](keyboard-v2-he-info-action-focus.png) |
| Enter activates info action | [image](keyboard-v2-en-info-action-submitted.png) | [image](keyboard-v2-he-info-action-submitted.png) |

Both sessions completed 28 actions with zero failed predicates and orderly browser closure. Raw CDP was audited: no `Runtime.evaluate` or `Runtime.callFunctionOn`, zero JavaScript exceptions and zero error-level browser/console messages. Each session emitted six `WebGL: INVALID_ENUM: getInternalformatParameter: invalid internalformat` warnings; these are retained, not called a warning-free run. EN exit 0 in 376.989 s; HE exit 0 in 265.530 s. [Commands and exact input sequence](commands.json), [image hashes and source comparison](manifest.json).

## Source and test attribution

All 102 private player files were rehashed before use. Eight relevant implementation/UI/test files are byte-identical between upstream `2d3e13c0` and the tested product `bd2abd9d`: MapFocusPresenter, ScreenNav, GameRoot, MapPointer, CameraController, Game.uxml, Statecraft.uss and MapFocusTests. The artifact also contains PR #1158's unused event-preview helpers and appended localisation keys; this is **not** an exact full-main build. The extra helpers are not connected to this tested UI path.

The product SHA's existing raw full EditMode XML records 1606 total, 1604 passed, zero failed, two Explicit skips, with zero C# compiler warnings in its original log. All nine map-focus/camera cases passed, including covering/start states, handler cleanup across restart/language changes, actual camera keys, click-through and contrast. Those are previous native results on `bd2abd9d`, not a new Editor run on this evidence commit and not physical-touch-device testing. No Editor or build ran for these captures. The same player's earlier all-eight-golden plus smoke-5 browser gates passed in both languages; they were not rerun for this input-only capture session.

The original PR #1134 contains the author's red-first evidence and the maintainer's integration checks. These are attributed primary review records: the original red/base-head raw logs are not present in this local archive. This supplemental browser audit therefore does not independently certify every original Done-when item. No new translation, native-speaker sign-off, screen-reader test, Safari test, human historical check or release approval is claimed. Issue #41 remains broader work.

## Provenance and licence

Screenshots: **CC BY-SA 4.0**, credit **Statecraft: Israel contributors**, following D13 and the existing screenshot licence ruling for visible map bakes. [Licence](../../../LICENSES/CC-BY-SA-4.0.txt). These are unmodified captures of the existing game, not newly generated artwork. Existing AI-generated map models are credited to **Tal Heuberger** under their existing CC0 records. No title painting, Cabinet portrait, event photograph or video is included in this published subset. The initial title/card captures and original browser streams are retained locally.

Terrain notice: produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved. Existing [NOTICE](../../../NOTICE), asset and font records retain their terms. The private player is not published or represented as a release-gated build. Provenance checked by the agent on 20 October 2026; no human signature is added.

AI assistance: Codex drove the isolated browser through real mouse/key CDP inputs, inspected the PNGs, audited raw event/error streams, compared source bytes and native XML, and wrote this report. No game state was injected.

Rules check:
1. Exact artifact/source SHAs, input, hashes, output and exit codes checked.
2. Claims bounded by direct evidence; original historical raw-log gaps remain explicit.
3. Stale build, privacy, licence and false all-green claims checked before publication.
4. Evidence-only fork branch; no product, CI, release, signature or licence-checkbox change.
