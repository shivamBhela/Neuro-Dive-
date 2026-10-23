import serial
import numpy as np
import pickle
import time
import keyboard
from scipy import signal
from collections import deque

import config                              
from features import extract_features

print("Loading model and scaler...")
try:
    with open("eeg_model.pkl", "rb") as f:
        model = pickle.load(f)

    with open("scaler.pkl", "rb") as f:
        scaler = pickle.load(f)
    print("✅ Model Loaded")
except FileNotFoundError:
    print("❌ Could not find eeg_model.pkl or scaler.pkl. Run train_model.py first.")
    exit()

try:
    ser = serial.Serial(config.PORT, config.BAUD_RATE, timeout=0.1)
    time.sleep(2)
    print(f"🔌 Connected to {config.PORT}")
except Exception as e:
    print(f"❌ Could not connect to {config.PORT}: {e}")
    exit()

buffer = deque(maxlen=config.BUFFER_SIZE)

print(f"🧠 Live Prediction Started. Press Ctrl+C to Stop.")
print(f"   Confidence Threshold: {config.CONFIDENCE_THRESHOLD * 100}%")

try:
    while True:
        try:
            line = ser.readline().decode('utf-8', errors='ignore').strip()
            if not line:
                continue
            val = float(line)
        except ValueError:
            continue

        buffer.append(val)

        if len(buffer) < config.BUFFER_SIZE:
            continue

        window = signal.detrend(np.array(buffer))

        features = extract_features(window, fs=config.SAMPLE_RATE)
        features_scaled = scaler.transform([features])

        probs = model.predict_proba(features_scaled)[0]
        max_idx = np.argmax(probs)
        action = model.classes_[max_idx]
        confidence = probs[max_idx]

        if confidence >= config.CONFIDENCE_THRESHOLD:
            print(f"🎮 Action: {action} (Conf: {confidence:.2f})")

            if action == "move_forward":
                keyboard.press_and_release("w")
            elif action == "move_backward":
                keyboard.press_and_release("s")
            elif action == "move_left":
                keyboard.press_and_release("a")
            elif action == "move_right":
                keyboard.press_and_release("d")
            elif action == "jump":
                keyboard.press_and_release("space")

            time.sleep(0.3)
            buffer.clear()

except KeyboardInterrupt:
    print("\n🛑 Stopped by user.")
finally:
    ser.close()