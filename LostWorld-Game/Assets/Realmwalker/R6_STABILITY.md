# R6 transition stability fix

Evidence: project `Logs/Editor-prev.log` records a native crash, with both managed and native stacks in `ObjectDispatcher.DispatchTypeChangesAndClear`, `WorldProcessor.Update` and `GPUResidentDrawer.PostPostLateUpdate`. Windows/Unity crash dumps and bug reporter launches also exist. This is not evidence of an ordinary scene-loading hang or a managed gameplay exception. The exact underlying engine defect is not established.

The active PC URP asset had GPU Resident Drawer enabled. Only that flag is now disabled as a targeted workaround for the recorded crash path. SRP Batcher, shaders, lighting, realm geometry and scene loading logic are unchanged. Performance on the demo computer should be checked after this rendering-path change.

Audio is a separate confirmed defect: all four realm scenes lacked AudioListeners, while only the optional controller-free build demo created one. IntegrationSession now creates one persistent listener, follows the realm camera (player fallback during initialization), and disables competing scene listeners on scene load. The optional demo uses the same owner instead of creating another listener. No camera was added.

Run **Tools > Lost Worlds > R6 > Validate Transition Stability**. This performs three Ruins-to-Cave cycles through the existing unlock/P11 transition API, checks listener continuity, then runs the full journey, tokens, finale, Finish and Replay regression. Direct scene loading is used only to set up repeated Ruins cases, not for the transition under test.

Manual confirmation: restart Unity to ensure the old GPU Resident Drawer instance is gone, run **R5 > Play Full Journey**, cross Ruins into Crystal Cave several times through Replay, check Console and Hierarchy (one SessionAudioListener under the persistent session), then finish and replay. Listen for ambience and confirm camera framing. An isolated automated test cannot prove the interactive Editor crash is eliminated on every run. No Windows build or Git push.

Automated result: 209 checks passed, including three repeated Ruins-to-Cave cycles, then the complete four-realm journey, 36-token persistence, trophy/three-star summary, Finish and Replay. Exactly one enabled listener was asserted during repeated transitions and traversal. R6 scene-reference preflight passed. No C# compilation errors/warnings or runtime warnings/errors were recorded in the successful run. Log: Validation/R6Stability.log. Actual fix files match the tested copy by SHA-256.
