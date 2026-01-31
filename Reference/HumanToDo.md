# HumanToDo - 残作業リスト

- [ ] **Player.inputactions**: "Revive" アクションを追加してください (キー: R, ゲームパッド: 北ボタン)。
- [ ] **プレハブ作成**: `Assets/Prefabs/Enemys/IntelligenceEnemy` を複製し、名前を `Ally_MiniMask` に変更してください。
- [ ] **シーン設定**: `LevelDesignStage.scene` に空の GameObject `InfectionManager` を作成し、`InfectionManager.cs` をアタッチしてください。
- [ ] **アセット参照**: `InfectionManager` の `Ally Prefab` フィールドに `Ally_MiniMask` プレハブを割り当ててください。
- [ ] **プレハブ設定**: `Ally_MiniMask` プレハブから `EnemyAI` コンポーネントを **削除** し、代わりに `AllyAI` コンポーネントを追加してください。
- [ ] **タグ設定**: `Ally_MiniMask` のタグが `Ally`、レイヤーが `PlayerHitbox` になっていることを確認してください。
- [ ] **シーン設定 (蘇生)**: 空の GameObject `ReviveManager` を作成し、`ReviveManager.cs` をアタッチしてください。
- [ ] **アセット参照**: `ReviveManager` の `Revive Effect Prefab` に適当なエフェクトプレハブを割り当ててください（任意）。
- [ ] **シーン設定 (テスト用)**: 感染機能テストのため、タグ `Enemy` のオブジェクトをシーンにいくつか配置してください。
