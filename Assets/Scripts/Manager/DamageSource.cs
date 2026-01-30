using UnityEngine;

// Criticalに関するEnum
public enum CriticalType
{
    Normal,          // 通常
    Critical,        // 黄クリ
    SuperCritical,   // 橙クリ (今回ロジック対応)
    HyperCritical    // 赤クリ (今回ロジック対応)
}

// 「ダメージの発生源」として、攻撃の威力を計算するクラス
public class DamageSource : MonoBehaviour
{
    // 持ち主のStatusManagerを保持する変数
    private StatusManager ownerStatus;

    [Header("Attack Specs")]
    // 💡 変更点: 固定値加算をやめ、「倍率（Multiplier）」に変更
    // 例: 1.0 = 標準, 0.5 = 弱い, 2.0 = 強い
    [SerializeField] private float damageMultiplier = 1.0f;
    [SerializeField] private float criticalMultiplier = 2.0f;

    // 💡 Step 8.4 修正: Hitboxと同じロジックで親を探す
    // これにより、近接攻撃（剣や体当たり）は自動的に機能するようになる
    void Start()
    {
        // まだ持ち主が登録されていなければ、親から探す
        if (ownerStatus == null)
        {
            ownerStatus = GetComponentInParent<StatusManager>();
        }
    }

    // 飛び道具（レーザー）用：親がいないので生成時に外から教える
    public void Initialize(StatusManager owner)
    {
        this.ownerStatus = owner;
    }

    // 💡 Step 13 追加: 持ち主の場所を教えるプロパティ
    // これがあるおかげで、レーザーが消えても「撃った人」を特定できる
    public Transform OwnerTransform
    {
        get
        {
            if (ownerStatus != null)
            {
                return ownerStatus.transform;
            }
            else
            {
                return this.transform; // 持ち主がいなければ自分（罠など）
            }
        }
    }

    // 💡 Step 8.5 変更: 計算結果だけでなく、クリティカルの種類も一緒に返す
    // 【outキーワードについて】
    // 戻り値(int)とは別に、もう一つの結果(CriticalType)を呼び出し元に返すための仕組みです。
    // このメソッドが終わる時、引数で渡された変数 'type' に結果が書き込まれます。
    public int CalculateDamage(out CriticalType type)
    {
        type = CriticalType.Normal; // 必ず初期値を設定(return 0だとtypeが返せない)

        // 持ち主がいない場合の安全策
        if (ownerStatus == null) return 0;

        float finalDamage = ownerStatus.CurrentAttack * damageMultiplier;

        float remainingRate = ownerStatus.CurrentCritRate;
        int critCount = 0;

        // 100%を超える確率を考慮してループ処理
        while (remainingRate > 0f)
        {
            if (remainingRate >= 1.0f)
            {
                critCount++; // 100%分確定
                remainingRate -= 1.0f;
            }
            else
            {
                // 端数の確率判定
                if (Random.value < remainingRate) critCount++;
                break;
            }
        }

        // Crit回数に応じてタイプと倍率を決定
        switch (critCount)
        {
            case 0:
                type = CriticalType.Normal;
                break;
            case 1:
                type = CriticalType.Critical;
                finalDamage *= criticalMultiplier;
                break;
            case 2:
                type = CriticalType.SuperCritical;
                finalDamage *= criticalMultiplier * 2f;
                break;
            default: // 3回以上
                type = CriticalType.HyperCritical;
                finalDamage *= criticalMultiplier * 3f;
                break;
        }

        // ====================================================
        // 4. 完了：結果を返す
        // ====================================================
        // この時点で 'type' には判定結果（NormalやCriticalなど）が入っています。
        // この 'type' の値は、out引数を通じて呼び出し元に渡されます。

        return Mathf.RoundToInt(finalDamage);
    }
}