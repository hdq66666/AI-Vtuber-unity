using System;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

public class PlayerControl : MonoBehaviour
{
    [Header("Animator / BlendShape")]
    public Animator animator;
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public string[] blendShapeNames = { "闭眼", "开口" };

    [Header("Blink Settings")]
    public float idleBlinkDelay = 5f;        // 闲置多久开始眨眼
    [Range(0.05f, 1f)] public float blinkSpeed = 0.15f;

    private float lastActionTime;
    private bool  isBlinking;
    private const int EYES = 1;              // blendShapeNames 中“closeEyes”索引

    private void Start()
    {
        StartCoroutine(Speak(10));
      //  IEnumerator Speak(float duration)
    }

    /* ---------- Unity Loop ---------- */
    private void Update()
    {
      //  HandleBlendShapes();
        CheckIdleBlink();
    }

    /* ---------- 公共 API ---------- */
    public void MarkAction() => lastActionTime = Time.time;

    public void PlayerAni(int index)
    {
        string trigger = GetAnimationTrigger(index);
        if (string.IsNullOrEmpty(trigger)) return;

        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.normalizedTime < 1f) return;

        animator.SetTrigger(trigger);
        MarkAction();
    }

    /* ---------- Blink ---------- */
    private void CheckIdleBlink()
    {
        if (isBlinking) return;

        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.normalizedTime < 1f) return;                // 有动画在播

        if (Time.time - lastActionTime >= idleBlinkDelay)
            StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        isBlinking = true;
        int idx = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendShapeNames[EYES]);
        if (idx == -1) yield break;

        // 闭眼
        for (float t = 0; t < 1; t += Time.deltaTime / blinkSpeed)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(idx, Mathf.Lerp(0, 100, t));
            yield return null;
        }
        // 睁眼
        for (float t = 0; t < 1; t += Time.deltaTime / blinkSpeed)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(idx, Mathf.Lerp(100, 0, t));
            yield return null;
        }

        skinnedMeshRenderer.SetBlendShapeWeight(idx, 0);
        MarkAction();             // 眨眼也算一次活动
        isBlinking = false;
    }

    /* ---------- BlendShape 示例 ---------- */
    // private void HandleBlendShapes()
    // {
    //     int mouthIdx = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendShapeNames[0]);
    //     if (mouthIdx != -1)
    //     {
    //         float w = Mathf.PingPong(Time.time * 50f, 100f);
    //         skinnedMeshRenderer.SetBlendShapeWeight(mouthIdx, w);
    //     }
    // }
    public IEnumerator Speak(float duration)
    {
        int mouthIdx = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendShapeNames[1]);
        if (mouthIdx == -1) yield break;

        float timer = 0f;
        float openCloseSpeed = 8f;        // 嘴巴开合次数(Hz) * 2π; 可在 Inspector 暴露

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float weight = (Mathf.Sin(timer * openCloseSpeed) + 1f) * 50f; // 0‑100 区间
            skinnedMeshRenderer.SetBlendShapeWeight(mouthIdx, weight);
            yield return null;
        }

        // 复位嘴型
        skinnedMeshRenderer.SetBlendShapeWeight(mouthIdx, 0f);
    }
    /* ---------- Helper ---------- */
    private static string GetAnimationTrigger(int n) => n switch
    {
        1  => $"one_{Random.Range(0, 2)}",
        2  => "two",
        3  => "three",
        4  => "four",
        5  => "five",
        6  => "six",
        7  => "seven",
        8  => "eight",
        9  => "nine",
        10 => "ten",
        11 => "eleven",
        12 => "twelve",
        13 => "thirteen",
        14 => "fourteen",
        15 => "fifteen",
        16 => "sixteen",
        17 => "seventeen",
        _  => null
    };
}
