
import serial
import time
import pyautogui

# Arduino settings
PORT = "COM6"
BAUD = 115200

# Control settings
HOLD_TIME = 3.0
COOLDOWN = 3.0

pyautogui.FAILSAFE = True
pyautogui.PAUSE = 0

def detect_command(value):
    """
    Map raw Arduino signal to game commands.
    These ranges are for prototype testing only.
    """
    if 1 <= value <= 50:
        return "a"

    elif 21 <= value <= 60:
        return "d"

    elif 41 <= value <= 80:
        return "d"

    elif 61 <= value <= 90:
        return "w"

    elif value >= 81:
        return "jump"

    return None


def execute_command(command):
    if command == "jump":
        print("JUMP + A (3 seconds)")

        pyautogui.keyDown("a")
        try:
            pyautogui.press("space")
            time.sleep(HOLD_TIME)
        finally:
            pyautogui.keyUp("a")

    else:
        print(f"HOLDING {command.upper()} FOR 3 SEC")

        pyautogui.keyDown(command)
        try:
            time.sleep(HOLD_TIME)
        finally:
            pyautogui.keyUp(command)


def main():
    print("NeuroDive Signal Controller")
    print("Connecting Arduino...")

    ser = serial.Serial(PORT, BAUD, timeout=1)

    try:
        time.sleep(2)

        print("Focus your game window in 5 seconds...")
        time.sleep(5)

        print("Controller ACTIVE")
        print("Press Ctrl+C to stop.\n")

        last_action = -float("inf")

        while True:
            line = ser.readline().decode(
                "utf-8", errors="ignore"
            ).strip()

            try:
                value = int(float(line))
            except ValueError:
                continue

            if not 0 <= value <= 1023:
                continue

            command = detect_command(value)

            now = time.monotonic()

            if command is not None:
                if now - last_action >= HOLD_TIME + COOLDOWN:
                    print(f"Signal: {value} -> {command}")

                    last_action = now
                    execute_command(command)

                    # Discard old serial samples accumulated
                    # while a key was being held.
                    ser.reset_input_buffer()

    except KeyboardInterrupt:
        print("\nController stopped.")

    finally:
        pyautogui.keyUp("w")
        pyautogui.keyUp("a")
        pyautogui.keyUp("s")
        pyautogui.keyUp("d")
        ser.close()


if __name__ == "__main__":
    main()
