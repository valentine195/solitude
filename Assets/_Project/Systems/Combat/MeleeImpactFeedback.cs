using System;
using System.Collections.Generic;
using SOLITUDE.Items;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Combat
{
    /// <summary>Optional recipient presentation. The returned lease cancels only this hit's feedback.</summary>
    public interface IMeleeHitFeedback
    {
        IDisposable Present(in DamageContext contact, MeleeAttackSpec configuration, float unitsPerTile);
    }

    [DisallowMultipleComponent]
    public sealed class MeleeImpactFeedback : MonoBehaviour
    {
        private readonly List<GameObject> effects = new();
        private readonly List<IDisposable> recipients = new();
        private MeleeAttackExecution owner;
        private AudioSource audioSource;
        private float remaining;
        private bool restoredAudioPause;
        public void Begin(MeleeAttackExecution execution)
        {
            Clear(); owner = execution; owner.ContactResolved += OnHit;
            Play(execution.Configuration.SwingSound);
        }
        private void Play(AudioClip clip)
        {
            if (clip == null) return;
            if (audioSource == null)
            {
                var host = new GameObject("MeleeAudio"); host.transform.SetParent(transform, false);
                audioSource = host.AddComponent<AudioSource>(); audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0; audioSource.loop = false;
            }
            audioSource.PlayOneShot(clip);
        }
        private void OnHit(DamageContext contact, DamageResult result)
        {
            if (owner == null || !owner.Context.CanApplyEffects) return;
            var spec = owner.Configuration;
            var entity = contact.Target.Entity;
            if (entity != null && entity.GetComponent<IMeleeHitFeedback>() is IMeleeHitFeedback feedback)
            {
                var lease = feedback.Present(contact, spec, owner.Context.UnitsPerTile);
                if (lease != null) recipients.Add(lease);
            }
            if (spec.ImpactEffect != null)
            {
                var effect = Instantiate(spec.ImpactEffect, contact.ImpactPosition, Quaternion.identity);
                SceneManager.MoveGameObjectToScene(effect, gameObject.scene);
                // The short flash interval also bounds this small static contact marker.
                var actorSprite = GetComponentInChildren<SpriteRenderer>();
                if (actorSprite != null) foreach (var sprite in effect.GetComponentsInChildren<SpriteRenderer>())
                { sprite.sortingLayerID = actorSprite.sortingLayerID; sprite.sortingOrder = actorSprite.sortingOrder + 1; }
                effects.Add(effect); remaining = Mathf.Max(remaining, spec.FlashDuration);
            }
            Play(spec.ImpactSound);
        }
        private void Update()
        {
            if (owner == null) return;
            bool paused = !owner.Context.CanApplyEffects;
            if (audioSource != null && paused != restoredAudioPause)
            { if (paused) audioSource.Pause(); else audioSource.UnPause(); restoredAudioPause = paused; }
            if (paused) return;
            remaining -= Time.deltaTime;
            if (remaining <= 0) ClearEffects();
        }
        public void End(MeleeAttackExecution execution) { if (ReferenceEquals(owner, execution)) Clear(); }
        private void ClearEffects()
        {
            foreach (var effect in effects) if (effect != null) { effect.SetActive(false); Destroy(effect); }
            effects.Clear(); remaining = 0;
        }
        private void Clear()
        {
            if (owner != null) owner.ContactResolved -= OnHit;
            owner = null; ClearEffects();
            foreach (var recipient in recipients) recipient.Dispose(); recipients.Clear();
            if (audioSource != null) audioSource.Stop(); restoredAudioPause = false;
        }
        private void OnDisable() => Clear();
        private void OnDestroy() => Clear();
    }
}
