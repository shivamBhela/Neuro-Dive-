import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import config

import serial
import numpy as np
import matplotlib.pyplot as plt
from scipy.signal import butter, filtfilt, iirnotch
from collections import deque
import time

FS = 250
PLOT_WINDOW = 500

try:
    ser = serial.Serial(config.PORT, config.BAUD_RATE, timeout=0.1)
    time.sleep(2)
except Exception as e:
    print(f"❌ Could not connect to {config.PORT}: {e}")
    exit()

buffer = deque(maxlen=PLOT_WINDOW)

# -------- Filters --------
def notch_filter(data):
    b, a = iirnotch(50, 30, FS)  # 50 Hz notch for powerline noise
    return filtfilt(b, a, data)

def bandpass_filter(data):
    nyq = 0.5 * FS
    b, a = butter(4, [0.5/nyq, 45/nyq], btype='band')
    return filtfilt(b, a, data)

# -------- Plot Setup --------
plt.ion()
fig, ax = plt.subplots()
line, = ax.plot([], [], linewidth=1)
ax.set_title("Filtered LIVE EEG Signal")
ax.set_xlabel("Samples")
ax.set_ylabel("Amplitude")

print("Running Clean EEG Display (20 sec)...")

start_time = time.time()

try:
    while time.time() - start_time < 20:

        try:
            raw = ser.readline().decode('utf-8', errors='ignore').strip()
            if raw:
                val = float(raw)
                buffer.append(val)
        except ValueError:
            pass

        if len(buffer) < 200: # Need enough data to filter
            continue

        data = np.array(buffer)

        # Apply filters
        data = notch_filter(data)
        data = bandpass_filter(data)

        line.set_data(range(len(data)), data)

        ax.set_xlim(0, len(data))
        # Add dynamic y-limits to handle different amplitudes gracefully
        ax.set_ylim(np.min(data)-20, np.max(data)+20)

        plt.draw()
        plt.pause(0.001)

except KeyboardInterrupt:
    print("Stopped by user.")
finally:
    ser.close()
    plt.ioff()
    plt.show()