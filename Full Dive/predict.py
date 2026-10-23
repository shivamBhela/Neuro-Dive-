
import serial
import time
import numpy as np

PORT = "COM6"
BAUD = 115200

ser = serial.Serial(PORT, BAUD, timeout=2)
time.sleep(2)

values = []

print("Reading live Arduino signal...")
print("Press Ctrl+C to stop.\n")

try:
    while True:
        line = ser.readline().decode(
            "utf-8", errors="ignore"
        ).strip()

        try:
            value = float(line)
            values.append(value)

            print(f"EEG: {value:.0f}")

            if len(values) >= 250:
                print("\n--- 1 SECOND SUMMARY ---")
                print("Min:", min(values))
                print("Max:", max(values))
                print("Mean:", round(np.mean(values), 2))
                print("Std:", round(np.std(values), 2))
                print("------------------------\n")

                values.clear()

        except ValueError:
            pass

except KeyboardInterrupt:
    print("\nStopped.")

finally:
    ser.close()
