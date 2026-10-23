# 🧠 Full Dive BCI: Neural Command System

[![Python 3.8+](https://img.shields.io/badge/python-3.8+-blue.svg)](https://www.python.org/downloads/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Scikit-Learn](https://img.shields.io/badge/Model-Stacking_Ensemble-orange.svg)](https://scikit-learn.org/)

**Full Dive BCI** is a high-performance Brain-Computer Interface (BCI) system designed to translate neurological signals (EEG/EMG) into real-time digital commands. Using a sophisticated **Hybrid Stacking Ensemble** machine learning architecture, it maps brainwave patterns to keyboard inputs, enabling hands-free control of applications and games.

---

## 🚀 System Architecture

The project follows a structured pipeline: **Data Acquisition** ➔ **Feature Engineering** ➔ **Ensemble Learning** ➔ **Real-time Inference**.

### 1. Data Acquisition (`record_data.py`)
- **Hardware Interface:** Connects via Serial (default `COM4` at `115200` baud).
- **Sampling:** Captures raw signal data at **250Hz**.
- **Dataset Generation:** Saves labeled movement data as `.npy` files for training.

### 2. Feature Engineering (`features.py`)
A robust 26-dimensional feature vector is extracted from every 128-sample window:
- **Time Domain (6):** Mean, Std Dev, Variance, Min, Max, Median.
- **Frequency Domain (4):** FFT Mean, FFT Std, FFT Max, Peak Frequency.
- **Band Powers (5):** Delta (0.5-4Hz), Theta (4-8Hz), Alpha (8-13Hz), Beta (13-30Hz), Gamma (30-45Hz).
- **Hjorth Parameters (3):** Activity, Mobility, Complexity.
- **Signal Complexity (2):** Zero Crossing Rate, Sample Entropy.
- **Cognitive Ratios (6):** Alpha/Beta (Relaxation), Theta/Beta (Focus), etc.

### 3. Machine Learning Model (`train_model.py`)
The system utilizes a **Hybrid Stacking Ensemble Model** (`eeg_model.pkl`) for maximum classification accuracy.

#### **Level 0: Base Learners**
| Model | Rationale |
| :--- | :--- |
| **Random Forest** | Captures non-linear relationships and high-dimensional feature importance. |
| **SVM (RBF Kernel)** | Excellent for high-dimensional classification boundaries. |
| **Gradient Boosting** | Sequentially corrects errors from previous iterations. |
| **Logistic Regression** | Provides a strong linear baseline. |

#### **Level 1: Meta-Learner**
- **Logistic Regression:** Acts as the "judge," combining the probability outputs of all base learners to produce the final, high-confidence prediction.

### 4. Real-time Prediction (`predict.py`)
- **Live Inference:** Processes incoming serial data streams in real-time.
- **Confidence Filtering:** Only executes actions above a configured `CONFIDENCE_THRESHOLD` (default 0.55).
- **Input Emulation:** Maps neural signatures to physical key presses (`W`, `A`, `S`, `D`, `Space`) using the `keyboard` library.

---

## 📂 Project Structure

```text
├── config.py           # Centralized hardware & window settings
├── features.py         # Signal processing & feature extraction logic
├── record_data.py      # Utilities for recording training samples
├── train_model.py      # Training script for the Stacking Ensemble
├── predict.py          # The live "Full Dive" engine (Serial -> Keyboard)
├── eeg_model.pkl       # The trained neural command model
├── scaler.pkl          # Feature scaling parameters (StandardScaler)
└── [action].npy        # Raw training data (rest, move_forward, etc.)
```

---

## 🛠️ Setup & Usage

### 1. Installation
```bash
pip install numpy scikit-learn scipy pyserial keyboard
```

### 2. Training your Neural Profile
1. **Record Data:** Run the specific action scripts (e.g., `move_forward.py`) or use `record_data.py`.
2. **Train Model:** 
   ```bash
   python train_model.py
   ```
   This will evaluate multiple models and save the **Stacking Ensemble** as the champion.

### 3. Entering "Full Dive"
Ensure your hardware is connected to the port specified in `config.py`, then run:
```bash
python predict.py
```

---

## ⚙️ Configuration (`config.py`)
| Variable | Description | Default |
| :--- | :--- | :--- |
| `PORT` | Serial port for EEG/EMG hardware | `COM4` |
| `SAMPLE_RATE` | Native sampling frequency of hardware | `250 Hz` |
| `WINDOW_SIZE` | Signal window for feature extraction | `128 samples` |
| `CONFIDENCE_THRESHOLD` | Sensitivity of the trigger | `0.55 (55%)` |

---

> [!TIP]
> For best results, ensure the EEG/EMG sensors have a high signal-to-noise ratio and consistent placement during both recording and live prediction.

---
*Developed for the Full Dive Research Initiative.*
