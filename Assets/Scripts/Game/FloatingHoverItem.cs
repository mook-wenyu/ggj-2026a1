using System.Collections;

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 可漂浮摇曳的交互物体：鼠标悬停时放大，移出时恢复；同时在场景中缓慢无规律漂浮移动。
/// 点击后播放粒子、显示 2D 精灵淡出动画，物体即刻消失。
/// 可单独使用，或与 InteractiveItem 搭配（若搭配使用，可将 enableHoverScale 关闭避免重复缩放）。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FloatingHoverItem : MonoBehaviour
{
    [Header("悬停缩放（与 InteractiveItem 同款）")]
    [Tooltip("鼠标悬停时的缩放倍数")]
    public float hoverScale = 1.1f;

    [Tooltip("缩放动画时长")]
    public float hoverDuration = 0.05f;

    [Header("漂浮摇曳")]
    [Tooltip("位置漂移幅度（世界单位）")]
    [SerializeField] private float _floatMoveRadius = 0.05f;

    [Tooltip("旋转摇曳幅度（度）")]
    [SerializeField] private float _floatRotateAmount = 3f;

    [Tooltip("漂浮速度（越大移动越快）")]
    [SerializeField] private float _floatSpeed = 0.5f;

    [Tooltip("随机种子，不同物体填不同值可避免同步")]
    [SerializeField] private float _noiseSeed = 0f;

    [Tooltip("是否启用悬停缩放（若同时挂 InteractiveItem 可关闭）")]
    [SerializeField] private bool _enableHoverScale = true;

    [Header("点击反馈")]
    [Tooltip("点击时播放的粒子预制体（可空）")]
    [SerializeField] private GameObject _particlePrefab;

    [Tooltip("点击时出现的 2D 精灵预制体（可空），停留 0.2 秒后向上移动并淡出")]
    [SerializeField] private GameObject _spritePrefab;

    [Tooltip("2D 精灵在物体后方停留时长")]
    [SerializeField] private float _spriteStayDuration = 0.2f;

    [Tooltip("2D 精灵向上移动距离")]
    [SerializeField] private float _spriteMoveUpDistance = 1f;

    [Tooltip("2D 精灵向上移动并淡出的总时长")]
    [SerializeField] private float _spriteFadeDuration = 0.5f;

    private Vector3 _baseLocalPosition;
    private Quaternion _baseLocalRotation;
    private Vector3 _baseScale;
    private bool _isHovered;
    private bool _isClicked;
    private Coroutine _hoverCoroutine;

    private void Awake()
    {
        _baseLocalPosition = transform.localPosition;
        _baseLocalRotation = transform.localRotation;
        _baseScale = transform.localScale;
        EnsureCollider2D();
    }

    private void EnsureCollider2D()
    {
        if (GetComponent<Collider2D>() != null)
        {
            return;
        }

        var collider = gameObject.AddComponent<BoxCollider2D>();
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            collider.size = spriteRenderer.sprite.bounds.size;
        }
    }

    private void Update()
    {
        if (_isClicked)
        {
            return;
        }

        ApplyFloating();
    }

    private void ApplyFloating()
    {
        var t = Time.time * _floatSpeed + _noiseSeed * 100f;

        // Perlin 噪声产生平滑无规律位移
        var x = (Mathf.PerlinNoise(t * 0.7f, _noiseSeed) - 0.5f) * 2f;
        var y = (Mathf.PerlinNoise(t * 0.5f + 100f, _noiseSeed + 50f) - 0.5f) * 2f;
        var offset = new Vector3(x, y, 0f) * _floatMoveRadius;

        transform.localPosition = _baseLocalPosition + offset;

        // 旋转摇曳
        var rot = (Mathf.PerlinNoise(t * 0.3f + 200f, _noiseSeed + 100f) - 0.5f) * 2f * _floatRotateAmount;
        transform.localRotation = _baseLocalRotation * Quaternion.Euler(0f, 0f, rot);
    }

    private void OnMouseEnter()
    {
        if (!_enableHoverScale || IsPointerOverUi())
        {
            return;
        }

        _isHovered = true;
        StartHoverEffect(true);
    }

    private void OnMouseExit()
    {
        if (!_enableHoverScale || IsPointerOverUi())
        {
            return;
        }

        _isHovered = false;
        StartHoverEffect(false);
    }

    private void OnMouseDown()
    {
        if (_isClicked || IsPointerOverUi())
        {
            return;
        }

        _isClicked = true;
        var worldPos = transform.position;

        if (_particlePrefab != null)
        {
            var particle = Instantiate(_particlePrefab, worldPos, Quaternion.identity);
            var ps = particle.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = particle.GetComponentInChildren<ParticleSystem>();
            }

            if (ps != null)
            {
                ps.Play();
            }
        }

        if (_spritePrefab != null)
        {
            var sprite = Instantiate(_spritePrefab, worldPos, Quaternion.identity);
            sprite.SetActive(true);
            SetSpriteBehindObject(sprite);
            var animator = sprite.GetComponent<SpriteRiseFadeOut>();
            if (animator == null)
            {
                animator = sprite.AddComponent<SpriteRiseFadeOut>();
            }

            animator.Play(_spriteStayDuration, _spriteMoveUpDistance, _spriteFadeDuration);
        }

        gameObject.SetActive(false);
    }

    private void SetSpriteBehindObject(GameObject sprite)
    {
        var mySr = GetComponent<SpriteRenderer>();
        var spriteSrs = sprite.GetComponentsInChildren<SpriteRenderer>(true);
        var baseOrder = mySr != null ? mySr.sortingOrder - 1 : -1;
        var layerName = mySr != null ? mySr.sortingLayerName : "Default";

        foreach (var sr in spriteSrs)
        {
            sr.sortingLayerName = layerName;
            sr.sortingOrder = baseOrder;
        }
    }

    private void StartHoverEffect(bool isEntering)
    {
        if (_hoverCoroutine != null)
        {
            StopCoroutine(_hoverCoroutine);
        }

        _hoverCoroutine = StartCoroutine(HoverEffect(isEntering));
    }

    private IEnumerator HoverEffect(bool isEntering)
    {
        var elapsedTime = 0f;
        var startScale = transform.localScale;
        var targetScale = isEntering ? _baseScale * hoverScale : _baseScale;

        while (elapsedTime < hoverDuration)
        {
            var t = hoverDuration <= 0f ? 1f : elapsedTime / hoverDuration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
        _hoverCoroutine = null;
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
