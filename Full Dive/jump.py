import serial
import numpy as np
import time
import config

PORT = config.PORT
BAUD = config.BAUD_RATE
RECORD_TIME = 600

ser = serial.Serial(PORT, BAUD, timeout=0.1)
time.sleep(2)

data = []
start_time = time.time()

print("Recording JUMP for 10 minutes...")

while True:
    if time.time() - start_time >= RECORD_TIME:
        break

    line = ser.readline().decode('utf-8', errors='ignore').strip()
    if line:
        try:
            data.append(float(line))
        except ValueError:
            pass

ser.close()
np.save("jump.npy", np.array(data))

print("✅ jump saved")
print("Samples:", len(data))
