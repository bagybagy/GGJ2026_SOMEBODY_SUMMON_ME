# HumanToDo - 残作業リスト

- [x] **Player.inputactions**: "Revive" アクションを追加してください (例: R, ゲームパッドNorth)。
- [x] **プレハブ作成**: `Assets/Prefabs/Enemys/IntelligenceEnemy` を複製し、名前を `Ally_MiniMask` に変更してください。
- [x] **シーン設定**: `LevelDesignStage.scene` に空の GameObject `InfectionManager` を作成し、`InfectionManager.cs` をアタッチしてください。
- [ ] **アセット参照**: `InfectionManager` の `Ally Prefab` フィールドに `Ally_MiniMask` プレハブを割り当ててください。
- [x] **プレハブ設定**: `Ally_MiniMask` プレハブから `EnemyAI` コンポーネントを **削除** し、代わりに `AllyAI` コンポーネントを追加してください。
- [x] **プレハブ設定 (Follow)**: `Ally_MiniMask` に `AllyActionFollow` コンポーネントを追加してください。
- [ ] **タグ設定**: `Ally_MiniMask` のタグが `Ally`、レイヤーが `PlayerHitbox` になっていることを確認してください。
- [ ] **シーン設定 (蘇生)**: 空の GameObject `ReviveManager` を作成し、`ReviveManager.cs` をアタッチしてください。
- [ ] **アセット参照**: `ReviveManager` の `Revive Effect Prefab` に適当なエフェクトプレハブを割り当ててください（任意）。
- [ ] **シーン設定 (テスト用)**: 感染機能テストのため、タグ `Enemy` のオブジェクトをシーンにいくつか配置してください。
- [ ] **シーン設定 (合体)**: 空の GameObject `MergeManager` を作成し、`MergeManager.cs` をアタッチしてください。
- [ ] **アセット参照**: `MergeManager` の `Hat Mask Prefab` に、合体後に生成するプレハブ（HatMask）をアサインしてください。
- [ ] **InputSystem設定**: `Player.inputactions` に以下のアクションを追加・設定してください。
    - **Merge**: キー `M` (または任意のボタン)
    - **Gather**: キー `G` (または任意のボタン)
- [ ] **Playerコンポーネント設定**: Playerオブジェクトの `Player Input` コンポーネントの Events に、追加した Action (`Merge`, `Gather`) と `Player.OnMerge`, `Player.OnGather` を紐づけてください。
