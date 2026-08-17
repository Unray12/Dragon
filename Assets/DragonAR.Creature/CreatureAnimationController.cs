using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DragonAR.Creature
{
    // Phat cac animation co san theo thu tu, lap vong lien tuc (bat dau tu startIndex khac
    // nhau moi con de lech pha, khong dong bo). Thinh thoang (moi ~15s) tam dung, quay mat
    // huong ve phia doi phuong/tam be mat de mo phong "tuong tac" - KHONG phai animation
    // tuong tac rieng (file goc khong co), chi la hieu ung xoay + chon dung clip Idle.
    //
    // QUAN TRONG: dung Animator + AnimatorOverrideController (doi clip gan vao 1 state co
    // san trong Controller) chu KHONG dung Animation (legacy) component + clip.legacy=true.
    // Cach do tung dung nhung chi chay dung trong Unity Editor, mat animation khi build ra
    // APK that (model import kieu Generic danh cho Animator/Mecanim, ep clip.legacy=true
    // luc runtime khong duoc IL2CPP build xu ly nhat quan voi Editor).
    public sealed class CreatureAnimationController : MonoBehaviour
    {
        private const float InteractionIntervalSeconds = 15f;
        private const float InteractionHoldSeconds = 2.5f;
        private const float TurnDegreesPerSecond = 90f;

        private Animator _animator;
        private AnimatorOverrideController _overrideController;
        private readonly Dictionary<string, AnimationClip> _clipsByName = new();

        private string _baseStateName;
        private string[] _order;
        private float _secondsPerClip;
        private int _currentIndex;
        private float _clipTimer;

        private Transform _partner;
        private Vector3 _centerPoint;
        private bool _isInteracting;
        private float _interactionTimer;

        public void Init(
            GameObject dragon,
            string modelResourcePath,
            string controllerResourcePath,
            string baseStateName,
            string[] order,
            float secondsPerClip,
            int startIndex)
        {
            _baseStateName = baseStateName;
            _order = order;
            _secondsPerClip = secondsPerClip;
            _currentIndex = order.Length == 0 ? 0 : startIndex % order.Length;

            _animator = dragon.GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                Debug.LogWarning("[CreatureAnimationController] Model khong co Animator, khong the phat animation.");
                return;
            }

            var baseController = Resources.Load<RuntimeAnimatorController>(controllerResourcePath);
            if (baseController == null)
            {
                Debug.LogWarning($"[CreatureAnimationController] Khong tim thay Animator Controller tai Resources/{controllerResourcePath}.");
                return;
            }

            foreach (var clip in Resources.LoadAll<AnimationClip>(modelResourcePath))
            {
                _clipsByName[clip.name] = clip;
            }

            _overrideController = new AnimatorOverrideController(baseController);
            _animator.runtimeAnimatorController = _overrideController;

            PlayCurrent();
        }

        public void SetInteractionPartner(Transform partner, Vector3 centerPoint)
        {
            _partner = partner;
            _centerPoint = centerPoint;
        }

        private void Update()
        {
            if (_overrideController == null || _isInteracting)
            {
                return;
            }

            _clipTimer += Time.deltaTime;
            if (_clipTimer >= _secondsPerClip)
            {
                _clipTimer = 0f;
                _currentIndex = (_currentIndex + 1) % _order.Length;
                PlayCurrent();
            }

            if (_partner != null)
            {
                _interactionTimer += Time.deltaTime;
                if (_interactionTimer >= InteractionIntervalSeconds)
                {
                    _interactionTimer = 0f;
                    StartCoroutine(InteractionBeat());
                }
            }
        }

        private void PlayCurrent()
        {
            SetOverrideClip(_order[_currentIndex]);
        }

        // Doi clip dang gan vao state goc, roi Play lai tu dau (0f) - vi day la thay clip
        // ben duoi 1 state duy nhat (khong phai chuyen state that) nen can restart de tranh
        // loi thoi gian phat.
        private void SetOverrideClip(string clipName)
        {
            if (!_clipsByName.TryGetValue(clipName, out var clip))
            {
                Debug.LogWarning($"[CreatureAnimationController] Khong tim thay clip '{clipName}' trong model.");
                return;
            }

            _overrideController[_baseStateName] = clip;
            _animator.Play(_baseStateName, 0, 0f);
        }

        private IEnumerator InteractionBeat()
        {
            _isInteracting = true;
            var normalRotation = transform.rotation;

            var lookAtPoint = _partner != null ? _partner.position : _centerPoint;
            var lookDirection = lookAtPoint - transform.position;
            lookDirection.y = 0f;
            var targetRotation = lookDirection.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(lookDirection.normalized, transform.up)
                : transform.rotation;

            SetOverrideClip(FindIdleClipName());

            yield return RotateTowards(targetRotation);
            yield return new WaitForSeconds(InteractionHoldSeconds);
            yield return RotateTowards(normalRotation);

            _isInteracting = false;
            PlayCurrent();
        }

        private IEnumerator RotateTowards(Quaternion target)
        {
            while (Quaternion.Angle(transform.rotation, target) > 0.5f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, TurnDegreesPerSecond * Time.deltaTime);
                yield return null;
            }
            transform.rotation = target;
        }

        private string FindIdleClipName()
        {
            foreach (var name in _order)
            {
                if (name.Contains("Idel") || name.Contains("Idle"))
                {
                    return name;
                }
            }
            return _order[_currentIndex];
        }
    }
}
