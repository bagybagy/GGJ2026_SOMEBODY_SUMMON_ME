using UnityEngine;

// 💡 ScriptableObject: ゲームの「データ」を保存するための専用クラス
// MonoBehaviourと違い、GameObjectにアタッチせず、ファイルとして存在します。

// [CreateAssetMenu]: 右クリックメニューからこのデータファイルを作成できるようにする魔法の言葉
// fileName = デフォルトのファイル名, menuName = メニューのどこに表示するか
[CreateAssetMenu(fileName = "NewStats", menuName = "Game/Character Stats Data")]
public class CharacterStatsData : ScriptableObject
{
    // ここに書いた変数が、インスペクターで編集できるデータになります。
    
    [Header("Base Stats")]
    public int MaxHp = 100;      // 最大HP
    public int AttackPower = 10; // 基礎攻撃力
    [Header("Critical")]
    [Min(0f)]
    public float CriticalRate = 0.2f;

    // 💡 Step 8.6 追加: 成長パラメーター
    // レベルが1上がるごとの上昇値
    [Header("Growth Stats (Per Level)")]
    public int HpGrowth = 10;               // HP成長率
    public int AttackGrowth = 2;            // 攻撃成長率
    public float CritRateGrowth = 0.01f;    // クリ率成長率 (1%)

    [Header("Exp Settings")]
    public int BaseExpToNext = 100;    // Lv1 -> Lv2 に必要な経験値
}