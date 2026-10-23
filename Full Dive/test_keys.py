import time
import pyautogui

print("Focus the game window within 5 seconds.")
time.sleep(5)

for key in ["w", "a", "s", "d", "space"]:
    print("Pressing:", key)
    pyautogui.keyDown(key)
    time.sleep(0.3)
    pyautogui.keyUp(key)
    time.sleep(1)