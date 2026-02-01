using System.Collections;

using UnityEngine;

/// <summary>
/// 2D 精灵向上移动并淡出，结束后销毁自身。用于点击反馈等场景。
/// </summary>
public class SpriteRiseFadeOut : MonoBehaviour
{
    [SerializeField] private float _stayDuration = 0.2f;
    [SerializeField] private float _moveUpDistance = 1f;
    [SerializeField] private float _fadeDuration = 0.5f;

    private SpriteRenderer[] _spriteRenderers;
    private Color[] _originalColors;

    /// <summary>
    /// 配置并启动动画。
    /// </summary>
    public void Play(float stayDuration, float moveUpDistance, float fadeDuration)
    {
        _stayDuration = stayDuration;
        _moveUpDistance = moveUpDistance;
        _fadeDuration = fadeDuration;
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        _originalColors = new Color[_spriteRenderers.Length];
        for (var i = 0; i < _spriteRenderers.Length; i++)
        {
            _originalColors[i] = _spriteRenderers[i].color;
        }

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        yield return new WaitForSeconds(_stayDuration);

        var startPos = transform.position;
        var endPos = startPos + Vector3.up * _moveUpDistance;
        var elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / _fadeDuration);

            transform.position = Vector3.Lerp(startPos, endPos, t);

            for (var i = 0; i < _spriteRenderers.Length; i++)
            {
                var sr = _spriteRenderers[i];
                if (sr == null)
                {
                    continue;
                }

                var c = _originalColors[i];
                c.a = Mathf.Lerp(c.a, 0f, t);
                sr.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
