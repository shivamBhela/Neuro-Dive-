import numpy as np
from scipy import signal as sci_signal

def _band_power(data, fs, band):
    """Calculate power in a specific frequency band using Welch's method."""
    freqs, psd = sci_signal.welch(data, fs=fs, nperseg=min(len(data), 64))
    idx = np.logical_and(freqs >= band[0], freqs <= band[1])
    if np.sum(idx) == 0:
        return 0.0
    # Use np.trapezoid (NumPy 2.0+) or fallback for older versions
    if hasattr(np, 'trapezoid'):
        return np.trapezoid(psd[idx], freqs[idx])
    else:
        return np.trapz(psd[idx], freqs[idx])


def _hjorth_parameters(data):
    """Calculate Hjorth Activity, Mobility, and Complexity."""
    # Activity = variance of the signal
    activity = np.var(data)

    # First derivative
    d1 = np.diff(data)
    # Second derivative
    d2 = np.diff(d1)

    var_d1 = np.var(d1)
    var_d2 = np.var(d2)

    # Mobility = sqrt(var(d1) / var(signal))
    mobility = np.sqrt(var_d1 / activity) if activity > 0 else 0.0

    # Complexity = mobility(d1) / mobility(signal)
    mobility_d1 = np.sqrt(var_d2 / var_d1) if var_d1 > 0 else 0.0
    complexity = mobility_d1 / mobility if mobility > 0 else 0.0

    return activity, mobility, complexity


def _zero_crossing_rate(data):
    """Count the number of times the signal crosses zero, normalized by length."""
    signs = np.sign(data)
    # Remove zeros to avoid ambiguity
    signs[signs == 0] = 1
    crossings = np.sum(np.abs(np.diff(signs)) > 0)
    return crossings / len(data)


def _sample_entropy(data, m=2, r_factor=0.2):
    """
    Approximate sample entropy — measures signal irregularity/complexity.
    Higher entropy = more complex/random signal.
    """
    N = len(data)
    r = r_factor * np.std(data)
    if r == 0:
        return 0.0

    def _count_matches(template_length):
        count = 0
        templates = np.array([data[i:i + template_length] for i in range(N - template_length)])
        for i in range(len(templates)):
            # Count pairs where max distance < r (excluding self-match)
            dist = np.max(np.abs(templates[i] - templates), axis=1)
            count += np.sum(dist < r) - 1  # subtract self-match
        return count

    A = _count_matches(m + 1)
    B = _count_matches(m)

    if B == 0 or A == 0:
        return 0.0
    return -np.log(A / B)


def extract_features(data, fs=250):
    """
    Extract a comprehensive set of EEG features from a signal window.
    
    This is the SINGLE SOURCE OF TRUTH for feature extraction.
    All training and prediction scripts must use this function.

    Features (26 total):
      Time Domain (6):     mean, std, var, min, max, median
      Frequency Domain (4): fft_mean, fft_std, fft_max, fft_peak_freq
      Band Powers (5):      delta, theta, alpha, beta, gamma
      Hjorth Parameters (3): activity, mobility, complexity
      Signal Shape (2):     zero_crossing_rate, sample_entropy
      Ratios (6):           alpha/beta, theta/beta, alpha/theta,
                            (alpha+theta)/(beta+gamma), delta/beta, theta/alpha
    """
    features = []

    # ===== TIME DOMAIN (6 features) =====
    features += [
        np.mean(data),
        np.std(data),
        np.var(data),
        np.min(data),
        np.max(data),
        np.median(data)
    ]

    # ===== FREQUENCY DOMAIN (4 features) =====
    fft_vals = np.fft.rfft(data)
    fft_abs = np.abs(fft_vals)

    features += [
        np.mean(fft_abs),
        np.std(fft_abs),
        np.max(fft_abs),
        np.argmax(fft_abs)  # index of peak frequency
    ]

    # ===== BAND POWERS (5 features) =====
    # Standard EEG frequency bands
    delta = _band_power(data, fs, (0.5, 4))    # Deep sleep, unconscious
    theta = _band_power(data, fs, (4, 8))      # Drowsy, meditative
    alpha = _band_power(data, fs, (8, 13))      # Relaxed, eyes closed
    beta  = _band_power(data, fs, (13, 30))     # Active thinking, focus
    gamma = _band_power(data, fs, (30, 45))     # Higher cognitive processing

    features += [delta, theta, alpha, beta, gamma]

    # ===== HJORTH PARAMETERS (3 features) =====
    activity, mobility, complexity = _hjorth_parameters(data)
    features += [activity, mobility, complexity]

    # ===== SIGNAL SHAPE (2 features) =====
    features.append(_zero_crossing_rate(data))
    features.append(_sample_entropy(data))

    # ===== BAND POWER RATIOS (6 features) =====
    # These ratios are strong discriminators for different mental states
    eps = 1e-10  # avoid division by zero
    features.append(alpha / (beta + eps))       # Relaxation vs focus
    features.append(theta / (beta + eps))       # Drowsiness vs alertness
    features.append(alpha / (theta + eps))      # Calm relaxation vs drowsy
    features.append((alpha + theta) / (beta + gamma + eps))  # Overall relaxation
    features.append(delta / (beta + eps))       # Deep state vs active
    features.append(theta / (alpha + eps))      # Engagement ratio

    return features
