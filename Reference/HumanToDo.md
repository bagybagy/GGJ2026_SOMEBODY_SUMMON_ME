# HumanToDo - 残作業リスト

## � ビジュアル・設定調整（手動推奨）

### 1. マテリアル設定
*   **MiniMask.prefab** (`Assets/Prefabs/Allies/`)
    *   [ ] 全身の色を緑色（Green）に変更してください（敵と区別するため）。
*   **UpgradedMask.prefab** (`Assets/Prefabs/Allies/`)
    *   [ ] 全身の色を特別色（金・紫など）に変更してください。
    *   [ ] 「帽子」などの装飾オブジェクトを追加して、強化された見た目にしてください。

### 2. PlayerInput イベント設定
*   **PlayerRoot** (Scene Object)
    *   [ ] `PlayerInput` コンポーネントの Events を展開。
    *   [ ] `Revive` アクションにイベントを追加。
    *   [ ] Target: `Player` component on PlayerRoot.
    *   [ ] Function: `Player.OnRevive`.
    *   *(スクリプトでの自動設定が難しいため、ここだけ手動でお願いします)*

---

## ✨ エフェクト作成（任意）

### 3. 各種エフェクトの割り当て
以下のコンポーネントにエフェクトプレハブ（パーティクル等）を割り当ててください。現状は空欄またはデフォルトです。

*   **GlobalRevivalSkill** (on PlayerRoot)
    *   `Revive Effect`: 蘇生時の演出
*   **MergeController** (on PlayerRoot)
    *   `Merge Effect`: 10体合体時の演出

---

## ✅ 完了した項目（自動化済み）

*   [x] `MiniMask.prefab` の作成とStatus設定（Team=Green, Lv=1）
*   [x] `UpgradedMask.prefab` の作成とStatus設定（Scale=1.5, Lv=10）
*   [x] `IntelligenceEnemy` へのMiniMask参照設定
*   [x] `PlayerRoot` へのスキルコンポーネント追加（`GlobalRevivalSkill`, `MergeController`）
*   [x] スキルパラメータの初期設定
*   [x] `Player.cs` へのスキル参照設定

**Note:** 自動化スクリプト `Assets/Editor/AntigravitySetup.cs` は `Tools/Configure All Tasks` から再実行可能です。
