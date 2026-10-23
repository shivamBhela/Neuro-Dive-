import serial
import numpy as np
import time
import argparse
import sys
import matplotlib.pyplot as plt

import config

def record_action(port, baud_rate, action_name, duration=600):
    """Record EEG data for a specific action"""
    
    try:
        ser = serial.Serial(port, baud_rate, timeout=0.1)  # small timeout for non-blocking read
        print(f"🔌 Connected to {port} at {baud_rate} baud")
    except Exception as e:
        print(f"❌ Error opening serial port: {e}")
        return
    
    time.sleep(2) # Give connection time to stabilize
    
    data = []
    start_time = time.time()
    
    print(f"\n🎙️ Recording for {duration} seconds for action: '{action_name}'...")
    print("Please ensure:")
    print("1. Electrode impedances are low")
    print("2. Subject is minimizing movements and blinks")
    print("3. Environment is electrically quiet")
    print("🛑 Press Ctrl+C to stop early\n")
    
    try:
        while time.time() - start_time < duration:
            if ser.in_waiting:
                line = ser.readline().decode('utf-8', errors='ignore').strip()
                if line:
                    try:
                        # CRITICAL FIX: Use float() instead of isdigit() so negative or decimal values are not lost
                        value = float(line)
                        data.append(value)
                    except ValueError:
                        pass
    except KeyboardInterrupt:
        print("\n🛑 Recording stopped early by user")
    
    ser.close()
    
    if len(data) < 100:
        print(f"⚠️ Warning: Only {len(data)} samples collected. Check your hardware connection.")
        return
        
    data_array = np.array(data)
    
    # Save raw data
    filename = f"{action_name}.npy"
    np.save(filename, data_array)
    print(f"✅ Saved {len(data_array)} raw samples to {filename}")
    
    # Optional signal quality check alert
    if np.std(data_array) < 1:
        print("⚠️ Warning: Signal variance is very low. Check electrode connections.")
    
    # Plot a sample of the data for visual confirmation
    plt.figure(figsize=(10, 4))
    sample_size = min(1000, len(data_array))
    plt.plot(data_array[:sample_size])
    plt.title(f"EEG Signal Sample for '{action_name}'")
    plt.xlabel("Sample Index")
    plt.ylabel("Amplitude")
    plot_filename = f"{action_name}_signal.png"
    plt.savefig(plot_filename)
    plt.close()
    print(f"✅ Saved signal plot preview as {plot_filename}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Record EEG data for a specific action.")
    parser.add_argument("--action", required=True, choices=config.ACTIONS, help="The action to record.")
    parser.add_argument("--port", default=config.PORT, help="Serial port (e.g., COM4)")
    parser.add_argument("--duration", type=int, default=600, help="Duration to record in seconds (default 600)")
    
    args = parser.parse_args()
    
    record_action(args.port, config.BAUD_RATE, args.action, args.duration)