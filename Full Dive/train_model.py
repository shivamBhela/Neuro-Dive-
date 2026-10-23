import numpy as np
import os
import pickle
from sklearn.ensemble import (
    RandomForestClassifier, 
    GradientBoostingClassifier,
    StackingClassifier,
    VotingClassifier
)
from sklearn.svm import SVC
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import accuracy_score, classification_report
from scipy import signal

import config
from features import extract_features

print("🔄 Loading data...")

X, y = [], []

for action in config.ACTIONS:
    filename = f"{action}.npy"

    if not os.path.exists(filename):
        print(f"❌ {filename} not found — skipping")
        continue

    data = np.load(filename)
    print(f"   Loaded {len(data)} samples from {filename}")

    for i in range(0, len(data) - config.WINDOW_SIZE, config.WINDOW_SIZE // 2):
        window = signal.detrend(data[i:i + config.WINDOW_SIZE])
        feat = extract_features(window, fs=config.SAMPLE_RATE)
        X.append(feat)
        y.append(action)

if len(X) == 0:
    print("❌ No data found! Record some .npy data first.")
    exit()

X = np.array(X)
y = np.array(y)

print(f"\n📊 Dataset: {len(X)} windows, {len(X[0])} features, {len(set(y))} classes")
for action in config.ACTIONS:
    count = np.sum(y == action)
    if count > 0:
        print(f"   {action}: {count} windows")

# ===== Feature Scaling ===== 
scaler = StandardScaler()
X_scaled = scaler.fit_transform(X)

X_train, X_test, y_train, y_test = train_test_split(
    X_scaled, y, test_size=0.2, random_state=42, stratify=y
)
# ================================================================
#  HYBRID STACKING ENSEMBLE   
# ================================================================
#
#  Architecture:
#  ┌─────────────────────────────────────────────────┐
#  │  Base Learners (Level 0)                        │
#  │  ┌──────────┐ ┌─────┐ ┌────┐ ┌──────────────┐  │
#  │  │ Random   │ │ SVM │ │ LR │ │ Gradient     │  │
#  │  │ Forest   │ │     │ │    │ │ Boosting     │  │
#  │  └────┬─────┘ └──┬──┘ └─┬──┘ └──────┬───────┘  │
#  │       │          │      │            │          │
#  │       ▼          ▼      ▼            ▼          │
#  │  ┌──────────────────────────────────────────┐   │
#  │  │  Meta-Learner (Level 1)                  │   │
#  │  │  Logistic Regression on base predictions │   │
#  │  └────────────────┬─────────────────────────┘   │
#  │                   │                             │
#  │                   ▼                             │
#  │            Final Prediction                     │
#  └─────────────────────────────────────────────────┘
#wasd              sa a a a a a a a a a wawwwsww wwwa w dwwa waa 
# ================================================================

print("\n🧠 Building Hybrid Stacking Ensemble...\n")

# ----- Base Learners -----
base_learners = [
    ("rf", RandomForestClassifier(
        n_estimators=500,
        max_depth=None,
        min_samples_split=5,
        class_weight="balanced",
        random_state=42,
        n_jobs=-1
    )),
    ("svm", SVC(
        kernel="rbf",
        C=10,
        gamma="scale",
        probability=True,       # Required for stacking & confidence scores
        class_weight="balanced",
        random_state=42
    )),
    ("lr", LogisticRegression(
        max_iter=3000,
        C=1.0,
        class_weight="balanced",
        random_state=42
    )),
    ("gb", GradientBoostingClassifier(
        n_estimators=300,
        learning_rate=0.05,
        max_depth=4,
        subsample=0.8,
        random_state=42
    )),
]

# ----- Meta-Learner -----
meta_learner = LogisticRegression(
    max_iter=2000,
    random_state=42
)

# ----- Stacking Classifier -----
stacking_model = StackingClassifier(
    estimators=base_learners,
    final_estimator=meta_learner,
    cv=5,                # 5-fold CV to generate meta-features
    stack_method="predict_proba",  # Pass probabilities to meta-learner
    n_jobs=-1,
    passthrough=False     # Only pass base model predictions, not raw features
)

# Also build a simple Voting ensemble for comparison
voting_model = VotingClassifier(
    estimators=base_learners,
    voting="soft",  # Use probability averaging
    n_jobs=-1
)

# ===== Train & Evaluate All Approaches =====
results = {}

# 1) Individual base models
print("--- Individual Models ---")
for name, model in base_learners:
    model.fit(X_train, y_train)
    pred = model.predict(X_test)
    acc = accuracy_score(y_test, pred)
    results[name.upper()] = acc
    print(f"  {name.upper():20s} → {acc * 100:.2f}%")

# 2) Voting Ensemble
print("\n--- Voting Ensemble ---")
voting_model.fit(X_train, y_train)
pred_vote = voting_model.predict(X_test)
acc_vote = accuracy_score(y_test, pred_vote)
results["VOTING"] = acc_vote
print(f"  {'VOTING':20s} → {acc_vote * 100:.2f}%")

# 3) Stacking Ensemble
print("\n--- Stacking Ensemble ---")
stacking_model.fit(X_train, y_train)
pred_stack = stacking_model.predict(X_test)
acc_stack = accuracy_score(y_test, pred_stack)
results["STACKING"] = acc_stack
print(f"  {'STACKING':20s} → {acc_stack * 100:.2f}%")

# ===== Pick Best Overall =====
best_name = max(results, key=results.get)
best_acc = results[best_name]

print(f"\n🏆 Best Model: {best_name} ({best_acc * 100:.2f}%)")

# Map name back to the trained model object
model_map = {name.upper(): model for name, model in base_learners}
model_map["VOTING"] = voting_model
model_map["STACKING"] = stacking_model

best_model = model_map[best_name]

# Print detailed report for the best model
print(f"\n{'='*50}")
print(f"Classification Report for {best_name}")
print(f"{'='*50}")
best_pred = best_model.predict(X_test)
print(classification_report(y_test, best_pred))

# ===== Cross-Validation on best model =====
print("Running 5-fold cross-validation on best model...")
cv_scores = cross_val_score(best_model, X_scaled, y, cv=5, scoring="accuracy")
print(f"CV Scores: {[f'{s:.3f}' for s in cv_scores]}")
print(f"CV Mean:   {cv_scores.mean() * 100:.2f}% ± {cv_scores.std() * 100:.2f}%")

# ===== Save =====
with open("eeg_model.pkl", "wb") as f:
    pickle.dump(best_model, f)

with open("scaler.pkl", "wb") as f:
    pickle.dump(scaler, f)

print(f"\n✅ Saved best model ({best_name}) → eeg_model.pkl")
print(f"✅ Saved scaler → scaler.pkl")
print(f"\nFeatures per window: {len(X[0])}")
print(f"Total windows trained on: {len(X_train)}")