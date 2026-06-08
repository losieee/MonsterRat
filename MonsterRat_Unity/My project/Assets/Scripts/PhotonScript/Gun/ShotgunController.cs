using Fusion;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;


public class ShotgunController : InvenBase
{
    public override ToolType Type => ToolType.ShotGun;

    public float ratDistance = 8f;
    public float ratAimRadius = 0.15f;
    public LayerMask ratMask;
    public LayerMask boxHeadMask;
    public GameObject pollutionPreb;
    public int hitCountAmount = 1;

    public AudioSource source;
    public AudioClip shotgunSound;
    public AudioClip reloadSound;

    [Header("Cooldown")]
    public float fireCooldown = 1.5f;
    private float nextFireTime = 0f;

    [Header("Ammo")]
    public int maxAmmo = 2;
    public float reloadTime = 2f;
    public GameObject ammoUIGroup;
    public Image[] ammoImages;

    private int currentAmmo;
    private bool isReloading;

    private void Start()
    {
        currentAmmo = maxAmmo;

        if (ammoUIGroup != null)
            ammoUIGroup.SetActive(false);

        if (!HasInputAuthority)
            return;

        UpdateAmmoUI();
    }

    public override void OnSelect()
    {
        if (!HasInputAuthority) return;

        if (ammoUIGroup != null)
            ammoUIGroup.SetActive(true);

        UpdateAmmoUI();
    }

    public override void OnDeselect()
    {
        if (!HasInputAuthority) return;

        if (ammoUIGroup != null)
            ammoUIGroup.SetActive(false);
    }

    public override void Tick()
    {
        if (!HasInputAuthority) return;

        if (SlimUI.ModernMenu.UISettingsManager.isMenuOpen || PhotonPlayerUIState.isGlobalStoreOpen)
            return;

        if (isReloading) return;

        if (Input.GetKeyDown(KeyCode.R))
            StartCoroutine(Reload());

        if (!Input.GetMouseButtonDown(0)) return;

        if (Time.time < nextFireTime) return;

        currentAmmo--;
        UpdateAmmoUI();

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
        }

        nextFireTime = Time.time + fireCooldown;

        Rpc_PlayShotgunSound();

        bool rat = TryShootRat();
        if (rat) return;

        bool box = TryShootBoxHead();
        if (box) return;

        // 쥐가 아닌 벽/바닥을 맞췄을 때
        //TrySpawnPollutionAtHit();
    }

    bool TryShootRat()
    {
        if (interactor == null) return false;

        // 레이캐스트를 쏴서 쥐를 찾음
        if (interactor.SphereCast(ratAimRadius, ratDistance, ratMask, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;

            // 멀티플레이용 PhotonRatController 가져오기
            PhotonRatController rat = hitObj.GetComponentInParent<PhotonRatController>();
            if (rat == null)
            {
                // 최상단 부모에서 한 번 더 검사
                rat = hitObj.transform.root.GetComponent<PhotonRatController>();
            }

            if (rat != null)
            {
                if (rat.IsDead) return false;

                rat.Rpc_TakeDamage(hit.point, hitCountAmount);

                interactor.ForceSetLookTarget(rat.gameObject);

                // 이거 튜토리얼이라서 일단 막았어요
                // var tm = Object.FindAnyObjectByType<TutorialManager>();
                // if (tm != null) tm.NotifyRatKilled(rat.gameObject);

                return true;
            }
        }
        return false;
    }

    bool TryShootBoxHead()
    {
        if (interactor == null) return false;

        if (interactor.SphereCast(ratAimRadius, ratDistance, boxHeadMask, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;

            MonsterBoxHead boxHead = hitObj.GetComponentInParent<MonsterBoxHead>();
            if (boxHead == null)
            {
                boxHead = hitObj.transform.root.GetComponent<MonsterBoxHead>();
            }

            if (boxHead != null)
            {
                boxHead.Rpc_RequestHit(hitCountAmount);
                interactor.ForceSetLookTarget(boxHead.gameObject);
                return true;
            }
        }
        return false;
    }

    bool TrySpawnPollutionAtHit()
    {
        if (interactor == null || pollutionPreb == null) return false;

        // 쥐를 못맞추고 일반 벽이나 바닥에 맞았을 때 이펙트 생성
        if (interactor.RaycastWorld(ratDistance, out RaycastHit hit))
        {
            int layer = hit.collider.gameObject.layer;
            // 특정 레이어는 무시
            if (layer == 3 || layer == 6 || layer == 8) return false;

            Vector3 pos = hit.point;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);

            Rpc_SpawnPollution(pos, rot);

            return true;
        }

        return false;
    }

    private void UpdateAmmoUI()
    {
        if (ammoImages == null) return;

        for (int i = 0; i < ammoImages.Length; i++)
        {
            if (ammoImages[i] != null)
                ammoImages[i].enabled = i < currentAmmo;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_SpawnPollution(Vector3 spawnPos, Quaternion spawnRot)
    {
        if (pollutionPreb != null)
        {
            Runner.Spawn(pollutionPreb, spawnPos, spawnRot, null);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_PlayShotgunSound()
    {
        if (source == null || shotgunSound == null)
            return;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        float baseVolume = HasInputAuthority ? 0.85f : 1f;
        float finalVolume = baseVolume * effectVolume;

        source.PlayOneShot(shotgunSound, finalVolume);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_PlayReRoadSound()
    {
        if (source == null || reloadSound == null)
            return;

        float effectVolume = 1f;

        if (SlimUI.ModernMenu.UISettingsManager.Instance != null)
            effectVolume = SlimUI.ModernMenu.UISettingsManager.Instance.EffectVolume;

        float baseVolume = HasInputAuthority ? 0.85f : 1f;
        float finalVolume = baseVolume * effectVolume;

        source.PlayOneShot(reloadSound, finalVolume);
    }

    private IEnumerator Reload()
    {
        if (currentAmmo >= maxAmmo) yield break;

        isReloading = true;

        int reloadCount = maxAmmo - currentAmmo;

        for (int i = 0; i < ammoImages.Length; i++)
        {
            if (ammoImages[i] != null)
                ammoImages[i].enabled = false;
        }

        for (int i = 0; i < reloadCount; i++)
        {
            Rpc_PlayReRoadSound();

            yield return new WaitForSeconds(reloadTime / reloadCount);
        }

        for (int i = 0; i < ammoImages.Length; i++)
        {
            if (ammoImages[i] != null)
                ammoImages[i].enabled = true;
        }

        currentAmmo = maxAmmo;
        UpdateAmmoUI();
        isReloading = false;
    }
}

