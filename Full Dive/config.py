# Configuration settings for Full Dive BCI

# Serial communication settings
PORT = "COM6"
BAUD_RATE = 115200

# EEG sampling rate (Hz) — must match your hardware
SAMPLE_RATE = 250

# Data recording and processing window settings
BUFFER_SIZE = 128
WINDOW_SIZE = 128

# Valid actions for the BCI
ACTIONS = ["move_forward", "move_backward", "move_left", "move_right", "jump", "rest"]

# Prediction threshold (0.0 to 1.0)
CONFIDENCE_THRESHOLD = 0.55
