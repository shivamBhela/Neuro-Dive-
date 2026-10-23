import serial
import numpy as np
import time
import config

PORT = config.PORT
BAUD = config.BAUD_RATE
RECORD_TIME = 300

ser = serial.Serial(PORT, BAUD, timeout=0.1)
time.sleep(2)

data = []
start_time = time.time()

print("Recording MOVE_RIGHT for 10 minutes...")

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












np.save("move_right.npy", np.array(data))

print("✅ move_right saved")
print("Samples:", len(data))
