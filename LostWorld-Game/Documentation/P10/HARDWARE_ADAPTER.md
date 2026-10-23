> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P10 — Unity-side processed hardware adapter

## External protocol

Unity is a TCP **server**, listening by default on **127.0.0.1:55010** while the adapter is enabled in Play Mode. The external processor connects as a TCP client and sends one command object per line:

```json
{"command":"JUMP"}
```

Append a newline byte (`\n`) after every object. CRLF also works. Supported values are exactly **LEFT, RIGHT, JUMP, GRAB, ACTIVATE**, in uppercase. The envelope must contain only the single `command` string property. Spaces/tabs around JSON tokens are allowed; duplicate/extra fields, unknown/lowercase values, numbers, null, escaped/non-ASCII values and malformed JSON are rejected.

Use UTF-8 without a BOM; the permitted envelope is ASCII. Maximum frame size is 256 bytes before the newline (including any CR). TCP chunks may split or combine messages: framing assembles complete lines. Oversized/non-ASCII frames are discarded until the next newline, allowing subsequent valid messages to recover. An unterminated frame is rejected when the connection closes.

This is a one-way command stream, with no acknowledgment protocol or request IDs. `Forwarded` means published through the P9 channel, not that gameplay accepted an action. Existing range/one-shot/route guards still decide acceptance. Repeated complete messages are distinct commands; the processor should send intentional discrete commands.

## Connect the hardware team's output

Use Assets/Prefabs/Hardware/P10/ProcessedHardwareAdapter.prefab, assign the existing P9 CommandChannel, and set the numeric bind address and port in its Inspector before Play. For another computer, use this Unity machine's LAN address (or 0.0.0.0 to bind its interfaces), then have the processor connect to the Unity machine's actual address. Any firewall permission is configured by the team; the project does not change firewall settings. This prototype TCP endpoint has no authentication or encryption, so use a trusted local network or an authenticated tunnel.

Only one active client is serviced at a time. Close the demo's local client with F11 before connecting real hardware. Connected means an accepted TCP peer, not device calibration, signal quality, or clinical status. Socket errors/disconnects return to listening for a reconnect; bind failures appear in the status panel. Stop/re-enable the adapter to retry a failed listener.

Minimal external Python example (run on the processed-output side, not inside Unity):

```python
import socket
with socket.create_connection(("127.0.0.1", 55010)) as connection:
    connection.sendall(b'{"command":"JUMP"}\n')
```

## Separation and safeguards

- TcpCommandTransport / IProcessedMessageTransport: TCP framing on a background thread, with no Unity or gameplay calls. Pending queue capacity is 64; overflow is dropped and counted.
- ProcessedCommandParser: strict clean-command validation only. No raw EEG/EMG/hand/exoskeleton/biosignal processing.
- ProcessedHardwareAdapter: consumes up to 16 queued frames each Unity frame, parses them and calls the unchanged CommandChannel.Send. Packets queued for over one second are discarded/counted to avoid stale replay after a paused/stalled frame.
- HardwareAdapterStatus: change-driven Connected/Disconnected, latest valid received command, rejection/drop counts and result/error display. Invalid payloads never replace the last valid command or enter P9.
- P10LocalTestClient: test-only real loopback TCP client; its samples pass through transport, parser, adapter and P9.

`ReceiveMessage` is the main-thread parser/channel entry point for a future alternative transport. Use only processed command envelopes. Transport code never calls it from its worker thread. Disable/quit closes listener/client sockets, clears queued data and stops the worker. Re-enabling starts a fresh adapter session and resets its receive counters. No network callback touches player transforms.

P9 keyboard input remains enabled as independent fallback. Keyboard and network sources can both publish; disable the HardwareCommandSimulator component when exclusively testing hardware. Fallback does not fabricate a Connected status. JUMP stays an event for future Person 1 integration and has no movement implementation.

## Local demo

Open **Tools > Lost Worlds > P10 > Play Hardware Adapter Demo**, then click Game view.

| Control | Action through actual TCP adapter |
| --- | --- |
| F4 | Connect local test client |
| F5 / F6 | LEFT / RIGHT |
| F7 | JUMP |
| F8 / F9 | GRAB / ACTIVATE |
| F10 | Send INVALID command; rejection count increases |
| F11 | Disconnect local test client |

Sending a sample auto-connects if needed. F1/F3 select P9's interaction/route test stations; F2 is retired. Use F1 with F8/F9 for interactions, F3 with F5/F6 for route choices. The original keyboard A/D/arrows, Space, G/R and E remain fallback controls. P8 Page Up/Page Down/Backspace remain effort test controls.

Stop and reopen via the menu for fresh one-shot interactions/relics/routes. Setup Hardware Adapter Demo creates missing assets and preserves existing ones. The separate P10 demo copies only the dedicated P9 test scene; no realm scenes are merged or modified.

## Manual verification

Use **Tools > Lost Worlds > P10 > Run Adapter Checks** for automated checks. Confirm all five commands, invalid rejection, connected/disconnected changes and keyboard fallback. Observe P7 success feedback separately from hardware receipt status. Space/F7 must not move anything. Inspect 720p/1080p layout and Console output.

Actual rehabilitation hardware has not been connected: the verified endpoint is the Unity-side TCP contract and localhost client. Hardware-team compatibility, network deployment and jump/player integration remain manual integration tasks. No P11 work is included.

Unity 6000.6.1f1 compilation and 65 P10 checks passed. P4/P5/P6/P7/P8/P9 regressions passed 22/31/34/66/57/54 checks. All 466 pre-existing Unity files remain unchanged. Demo and prefab generation is complete.
