using UnityEngine;

//SE Prefabを生成してを鳴らして消去する汎用スクリプト
[RequireComponent(typeof(AudioSource))]
public class SoundEffect : MonoBehaviour
{
    void Start()
    {
        var audioSource = GetComponent<AudioSource>();
        // 音の長さ分だけ待ってから自身を消去する（PlayOnAwakeしてればピッタリ消滅）
        Destroy(gameObject, audioSource.clip.length);
    }
}