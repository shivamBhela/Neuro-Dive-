import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import config
from features import extract_features

import serial
import numpy as np
import pickle
import time
from scipy import signal
from collections import deque

try:
    with open(os.path.join(os.path.dirname(__file__), "..", "eeg_model.pkl"), "rb") as f:
        model = pickle.load(f)
    with open(os.path.join(os.path.dirname(__file__), "..", "scaler.pkl"), "rb") as f:
        scaler = pickle.load(f)
except FileNotFoundError:
    print("❌ Model files not found! Please run train_model.py first.")
    exit()

try:
    ser = serial.Serial(config.PORT, config.BAUD_RATE, timeout=0.1)
    time.sleep(2)
except Exception as e:
    print(f"❌ Could not connect to {config.PORT}: {e}")
    exit()

buffer = deque(maxlen=config.BUFFER_SIZE)

print("🧠 Live Dynamic Prediction Started\n")

try:
    while True:
        try:
            line = ser.readline().decode('utf-8', errors='ignore').strip()
            if not line:
                continue
            value = float(line)
            buffer.append(value)
        except ValueError:
            continue

        if len(buffer) < config.BUFFER_SIZE:
            continue

        data = signal.detrend(np.array(buffer))

        features = extract_features(data, fs=config.SAMPLE_RATE)
        features_scaled = scaler.transform([features])

        probs = model.predict_proba(features_scaled)[0]
        action = model.classes_[np.argmax(probs)]
        confidence = np.max(probs)

        print(
            f"Mean:{features[0]:.2f} | "
            f"Std:{features[1]:.2f} | "
            f"Alpha:{features[12]:.2f} | "
            f"Beta:{features[13]:.2f} | "
            f"Action:{action} | "
            f"Conf:{confidence:.2f}"
        )

except KeyboardInterrupt:
    print("\n🛑 Stopped")
finally:
    ser.close()