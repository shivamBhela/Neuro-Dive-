NeuroDive — EEG + EMG Interactive Control Prototype
What are we building?
NeuroDive is a prototype exploring a hybrid brain–muscle interface for interaction with a computer game and a servo-driven robotic hand. The project uses an EEG acquisition path for game controls and an EMG acquisition path for robotic motor controls.
This is an experimental engineering prototype, not a validated medical device or proven thought-reading system.
System overview
EEG → game-control path
```text
EEG electrodes → BioAmp EXG Pill → Arduino UNO (A0)
    → USB serial (115200 baud) → Python → keyboard events → game
```
EMG → robotic-hand path
```text
EMG sensor → ESP32 (GPIO34) → I²C (GPIO21/22)
    → PCA9685 (0x40) → servo channels CH0–CH5
```
The number of servo channels addressed by the latest sketches is six (CH0–CH5); the number of physical motors actually installed must be checked against the build.
Progress and evidence
Milestone	Current status	What is actually established
Capture live EEG-path data	Done — raw acquisition	Arduino-to-Python serial data was observed live on COM6, including numerical readings. Whether the values represent usable physiological EEG has not been verified.
Capture live EMG data	Reported done by project owner	The EMG architecture and ESP32 ADC acquisition code are in place. A successful live EMG acquisition log is not present in the shared test results.
Control keyboard using EEG-path signal	Done — prototype mapping	The keyboard W/A/S/D/SPACE test worked, and a raw-ADC-threshold controller was created. Independent verification of reliable EEG-derived multi-class commands is pending.
Control motors using EMG	Reported done by project owner	PCA9685-driven servo motion was reported working, and EMG-gated control code has been created. Confirmed flex/relax-to-servo test logs have not been provided here.
Integrate hybrid EEG + EMG model	Reported done by project owner; end-to-end verification pending	EEG and EMG control pathways have been developed. A synchronized, demonstrated combined model or complete integration test is not documented in the shared results.
EEG implementation and observed results
Hardware: Arduino UNO and BioAmp EXG Pill, with BioAmp output routed to Arduino A0.
Arduino serial rate: 115200 baud.
Arduino sampling target: 250 samples/second (4 ms interval).
Python serial port in the demonstrated test: COM6.
Python successfully read 50 numeric values from Arduino.
Observed signal summary: minimum 0, maximum 87, mean 31.4, standard deviation 38.81.
Typical readings repeated in a pattern similar to `0, 0, 0, 85, 80`. This pattern is not enough to establish valid EEG; possible interference, clipping, wiring, and acquisition problems remain unresolved.
Arduino A0 checks previously returned approximately 0 for grounded input and 675 when connected to 3.3 V, indicating the UNO ADC could respond to known input levels.
Keyboard control
A Python `pyautogui` test issued W, A, S, D, and SPACE; the project owner confirmed the keys were pressed.
A later prototype mapped ADC value ranges to controls and allowed 3-second directional key holds. A jump generated a SPACE tap while holding A.
These are signal-threshold mappings, not verified classifications of distinct mental intentions.
EMG and servo implementation
Controller: ESP32.
EMG analog input: GPIO34.
PCA9685 connection: SDA GPIO21, SCL GPIO22, address 0x40.
Servo channels targeted by the current code: CH0 through CH5.
Code supports calibration, baseline/deviation-based EMG envelope estimation, flex/relax thresholds, and servo position commands.
Earlier servo-only tests were reported to move motors. Later EMG control attempts were described as not working, followed by a final statement that EMG motor control is done; therefore successful EMG-driven operation is reported, not independently documented in a shared log.
Typical positional servo control is issued in microseconds (e.g. 1500–1800 µs for a conservative sweep). A positional servo must not be assumed to support 360° rotation without checking its exact variant.
Repository/code components discussed
`config.py` — acquisition settings.
`record_data.py` — data recording.
`predict.py` — the version pasted in the conversation was a 50-sample serial diagnostic, not a trained-model inference script.
`features.py` — feature extraction for the EEG pipeline.
`train_model.py` — training code.
`eeg_model.pkl`, `scaler.pkl` — saved model-related assets mentioned in the project.
`signal_control.py` — prototype serial-to-keyboard control script developed during testing.
ESP32 Arduino sketches — PCA9685 servo tests and EMG-triggered servo control.
The existence of trained-model files does not, by itself, demonstrate that the latest runtime executes model inference or that a hybrid EEG/EMG classifier has been evaluated.
What is confirmed vs. what still needs validation?
Confirmed from observed output or direct feedback:
Arduino UNO sends numeric serial data that Python receives.
A standalone Python test successfully pressed W/A/S/D/SPACE.
Motor movement under PCA9685 test sketches was reported.
Pending documented end-to-end verification:
Clean, physiologically meaningful EEG signal acquisition.
Reliable EMG flex-versus-rest discrimination and logged motor response.
Correct game movement for each EEG-derived command, rather than test key events.
Concurrent EEG and EMG operation with a measured hybrid integration result.
Servo power integrity, electrical safety, travel limits, and repeatability.
Electrical and operating safety
The ESP32 ADC input must remain within its supported voltage range; do not apply negative voltage or 5 V directly to GPIO34.
An EMG module supplied by ±9 V rails does not automatically provide an ESP32-safe signal; verify its output voltage first.
Supply servos from a properly rated dedicated power source, not the ESP32 supply pins. High-torque servos can draw substantial current when moved together.
Keep an accessible physical motor-power cutoff and test without wearing a motorized exoskeleton.
Do not attach electrodes to a person through an unverified USB-connected DIY measurement setup. Proper patient isolation/electrical safety is required.
Project summary
NeuroDive has demonstrated important prototype building blocks: serial biosignal-path acquisition, software keyboard actuation, and PCA9685 servo movement. The project owner reports live EMG acquisition, EMG-controlled motors, and hybrid integration as completed.
