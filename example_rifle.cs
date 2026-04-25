using ClockStone; //AudioItem
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ToolsOfTheTrade;

namespace TotTExample
{
    internal class example_rifle
    {
        //TODO: SHEDLOADS OF COMMENTS
        static bool isLoaded = false;

        public static PlayerCardData data = null;
        static GameObject bulletPrefab = null;

        static float dashTimer;
        static Vector3 dashDirection;
        static Vector3 dashEndVector;
        static Vector3 moveDirection;
        static readonly List<BaseDamageable> dashTargets = [];
        static void DoDiscard()
        {
            BigChungus.currentlyActiveUpdateEffects.Add(data.cardID);

            dashDirection = RM.mechController.playerCamera.PlayerCam.transform.parent.forward;
            dashTimer = 0.35f;

            dashEndVector = dashDirection * 10f;

            AudioController.Play("MECH_DASH");
            if (RM.drifter.GetIsStomping())
            {
                AccessTools.Field(typeof(FirstPersonDrifter), "stomping")
                           .SetValue(RM.drifter, false);
                AudioController.Stop("MECH_BOOST");
                AudioController.Stop("ABILITY_STOMP_LOOP");
            }
            if (RM.drifter.GetIsTelefragging())
            {
                //stop telefragging or smth i dunno
            }
            if ((bool)AccessTools.Field(typeof(FirstPersonDrifter), "ziplining").GetValue(RM.drifter))
            {
                RM.drifter.CancelZiplineFromAnotherAbility();
            }
            dashTargets.Clear();
        }
        static void EarlyUpdateVelocity(float deltaTime)
        {
            RM.drifter.playerDashDamageableTrigger.SetTrigger(true);
            Vector3 vector3 = dashDirection * AxKEasing.EaseOutQuad(0f, 120f, dashTimer / 0.35f);
            dashTimer -= deltaTime;
            if (dashTimer <= 0f)
            {
                BigChungus.currentlyActiveUpdateEffects.Remove(data.cardID);
                RM.drifter.AddExternalVelocity(dashEndVector);
                var ffffff = AccessTools.Field(typeof(FirstPersonDrifter), "velocity");
                var vel = (Vector3)ffffff.GetValue(RM.drifter);
                vel.y = 1f;
                ffffff.SetValue(RM.drifter, vel);

                if (!RM.drifter.Motor.GetState().GroundingStatus.IsStableOnGround)
                {
                    AccessTools.Field(typeof(FirstPersonDrifter), "jumpTimer").SetValue(RM.drifter, 0);
                    AccessTools.Field(typeof(FirstPersonDrifter), "jumpForgivenessTimer").SetValue(RM.drifter, -1f);
                }
                vector3 = Vector3.zero;
            }
            moveDirection = vector3;
        }
        static void LateUpdateVelocity(float _)
        {
            AccessTools.Field(typeof(FirstPersonDrifter), "moveDirection").SetValue(RM.drifter, moveDirection);
            AccessTools.Field(typeof(FirstPersonDrifter), "movementVelocity").SetValue(RM.drifter, moveDirection);
            AccessTools.Field(typeof(FirstPersonDrifter), "velocity").SetValue(RM.drifter, Vector3.zero);
        }
        static bool OnMovementHit(BaseDamageable damageable)
        {
            if (damageable == null) { return false; }
            bool isEnemy = damageable.GetDamageableType() == BaseDamageable.DamageableType.Enemy;
            bool isBalloon = damageable.GetEnemyType() == Enemy.Type.balloon;

            if (isEnemy && isBalloon)
            {
                damageable.gameObject.GetComponent<EnemyBalloon>().OnBalloonHit();
            }

            if ((!isEnemy || !isBalloon) && dashTargets.Contains(damageable) == false)
            {
                int damage = 40;
                if (damageable.GetEnemyType() == Enemy.Type.bossBasic)
                {
                    damage /= 2;
                }
                var asdasd = ((RaycastHit)AccessTools.Field(typeof(FirstPersonDrifter), "hit").GetValue(RM.drifter)).point;
                damageable.OnHit(asdasd, damage, BaseDamageable.DamageSource.Dash);
                RM.mechController.ForceStompAfterglow();
                dashTargets.Add(damageable);
            }

            if (isEnemy
             && damageable.GetEnemyType() == Enemy.Type.mimic
             && EnemyMimic.mimicType == EnemyMimic.MimicType.Attack)
            {
                RM.mechController.OnHit(RM.mechController.currentHealth, damageable.transform.position, true);
            }
            return false;
        }
        static ProjectileBase CreateProjectile(Vector3 startPosition, Vector3 direction)
        {
            var newProjectile = ObjectPool.Spawn(bulletPrefab, startPosition, Quaternion.LookRotation(direction));
            if (newProjectile.TryGetComponent(out example_rifleProjectile component) == false)
            {
                component = newProjectile.AddComponent<example_rifleProjectile>();
            }
            component.OnSpawn(startPosition, direction);
            return component;
        }
        public static void Init()
        {
            if (!isLoaded)
            {
                data = ScriptableObject.CreateInstance<PlayerCardData>();

                if ((data.cardDesignTexture = Main.assets.LoadAsset<Texture2D>("example_rifle_card")) == null)
                {
                    throw new ArgumentException("failed to load Asset \"example_rifle_card\"");
                }
                if ((data.abilityIconTextureActive = Main.assets.LoadAsset<Texture2D>("example_rifle_uiAbilityIconActive")) == null)
                {
                    throw new ArgumentException("failed to load Asset \"example_rifle_uiAbilityIconActive\"");
                }
                if ((data.abilityIconTextureDisabled = Main.assets.LoadAsset<Texture2D>("example_rifle_uiAbilityIconInactive")) == null)
                {
                    throw new ArgumentException("failed to load Asset \"example_rifle_uiAbilityIconInactive\"");
                }
                if ((data.crystalTexture = Main.assets.LoadAsset<Texture2D>("example_rifle_crystal")) == null)
                {
                    throw new ArgumentException("failed to load Asset \"example_rifle_crystal\"");
                }
                if ((data.weaponIconTexture = Main.assets.LoadAsset<Texture2D>("example_rifle_icon")) == null)
                {
                    throw new ArgumentException("failed to load Asset \"example_rifle_icon\"");
                }
                AudioCategory weaponCategory = AudioController.GetCategory("WEAPONS");

                AudioSubItem[] fireClips =
                [
                    new AudioSubItem() { Clip = Main.assets.LoadAsset<AudioClip>("gun_example_rifle_shot_01") },
                    new AudioSubItem() { Clip = Main.assets.LoadAsset<AudioClip>("gun_example_rifle_shot_02") },
                    new AudioSubItem() { Clip = Main.assets.LoadAsset<AudioClip>("gun_example_rifle_shot_03") },
                    new AudioSubItem() { Clip = Main.assets.LoadAsset<AudioClip>("gun_example_rifle_shot_04") }
                ];
                if (fireClips.Any(elem => elem.Clip == null))
                {
                    throw new ArgumentException("failed to load an audio asset for FIRE");
                }
                var fire = new AudioItem()
                {
                    Name = "WEAPON_EXAMPLE_RIFLE_FIRE", //HAS to be "WEAPON_" + data.weaponAudioName + "_FIRE"
                    subItems = fireClips,
                    //SubItemPickMode = AudioPickSubItemMode.RandomNotSameTwice,    //this is default
                };
                AudioController.AddToCategory(weaponCategory, fire);

                var cock = Main.assets.LoadAsset<AudioClip>("gun_example_rifle_cock_01");
                if (cock == null)
                {
                    throw new ArgumentException("failed to load an audio asset for EQUIP");
                }
                AudioController.AddToCategory(weaponCategory, cock, "WEAPON_EXAMPLE_RIFLE_EQUIP");

                if ((bulletPrefab = Main.assets.LoadAsset<GameObject>("example_rifleProjectile")) == null)
                {
                    throw new ArgumentException("failed to load Asset \"example_rifleProjectile\"");
                }


                data.name = "Example_Weapon_Rifle";
                data.burstData = new HitscanBurstData();
                data.cardColor = new Color(0.4745098f, 0.5921569f, 0.9647059f, 1);
                data.cardDescription = "It's Godspeed";
                data.cardDrawTime = 0f;
                data.cardID = "EXAMPLE_RIFLE";
                data.cardName = "Godspeed";
                data.cardSwapTime = 0.025f;
                data.cardType = PlayerCardData.Type.WeaponProjectile;
                data.clipSize = 4;
                data.crosshairOverheatScaleRange = new Vector2(0.3f, 1f);
                data.discardAbility = (PlayerCardData.DiscardAbility)202;
                data.fireRate = 0.4f;
                data.manualFireRate = 0.18f;
                data.muzzleFlashType = MuzzleFlashController.FlashType.Focused;
                data.overheatDecaySpeed = 4;
                data.overheatPerShot = 1;
                data.projectileID = data.cardID;
                data.rigidbodyKnockback = 10f;
                data.screenshake = new Vector2(2.8f, 3f);
                data.showcaseItemType = MenuScreenItemShowcase.ItemType.Card;
                data.weaponAudioName = "EXAMPLE_RIFLE";

                isLoaded = true;
            }
            if (BigChungus.customDictionary.ContainsKey(data.cardID) == false)
            {
                BigChungus.customDictionary.Add(data.cardID, new(data: data,
                                                                 checkDiscardAllowed: () => true,
                                                                 abortAbility: (_)=> BigChungus.currentlyActiveUpdateEffects.Remove(data.cardID),
                                                                 createCustomProjectile: CreateProjectile,
                                                                 doDiscard: DoDiscard,
                                                                 updateVelocityEarly: EarlyUpdateVelocity,
                                                                 updateVelocityLate: LateUpdateVelocity,
                                                                 onMovementHit: OnMovementHit));
            }
            if (BigChungus.discardNumberToCardID.ContainsKey((int)data.discardAbility) == false)
            {
                BigChungus.discardNumberToCardID.Add((int)data.discardAbility, data.cardID);
            }
        }
        class example_rifleProjectile : ProjectileBase
        {
            public override void OnSpawn(Vector3 origin, Vector3 forward)
            {
                _collisionLayerMask = -29057023;//LayerMask.GetMask(blablabla who knows)
                _collisionRadiusDamageable = 0.35f;
                _collisionRadiusWorld = 0.05f;
                _damageTarget = DamageTarget.Damageable;
                _drag = 0;
                _explosionDamage = 3;
                _freezeMovement = false;
                _hitType = HitType.Bullet;
                _initialVelocity = new(0,0,455);
                _maxBounces = 0;
                _projectileModelHolder = gameObject.transform.Find("Bullet_body");
                destroysOtherProjectiles = true;
                base.OnSpawn(origin, forward);
            }
            //if you want custom projectile behaviour you can put it here
        }
    }
}