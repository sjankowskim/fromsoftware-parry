using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Networking;

namespace FromSoftwareParry
{
    public enum ParrySound
    {
        // Demon Souls parry SFX is same as DS1
        DS1,
        DS2,
        Bloodborne,
        // Dark Souls 3 parry SFX is same as DS1 & Sekiro Deathblow
        // Dark Souls Remastered parry SFX is same as DS1
        Sekiro,
        SekiroHuge,
        DeSR,
        EldenRing,
        Custom
    }

    public enum ParryType
    {
        Posture,
        Slowed,
        Stagger,
        Tiered
    }

    public enum PostureParryMode
    {
        Slowed,
        Stagger,
        Disarm
    }

    public class FromSoftwareParry : ThunderScript
    {
        public static ModOptionFloat[] zeroToOneHundered()
        {
            ModOptionFloat[] options = new ModOptionFloat[101];
            float val = 0;
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = new ModOptionFloat(val.ToString("0.0"), val);
                val += 1f;
            }
            return options;
        }

        public static ModOptionFloat[] zeroToHundredWithTenths()
        {
            ModOptionFloat[] options = new ModOptionFloat[1001];
            float val = 0;
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = new ModOptionFloat(val.ToString("0.0"), val);
                val += 0.1f;
            }
            return options;
        }

        public static ModOptionFloat[] zeroToEightWith2Tenths()
        {
            ModOptionFloat[] options = new ModOptionFloat[41];
            float val = 0;
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = new ModOptionFloat(val.ToString("0.0"), val);
                val += 0.2f;
            }
            return options;
        }

        public static ModOptionInt[] oneToTen()
        {
            ModOptionInt[] options = new ModOptionInt[10];
            int val = 1;
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = new ModOptionInt(val.ToString("0"), val);
                val++;
            }
            return options;
        }

        public static ModOptionInt[] zeroToThree()
        {
            ModOptionInt[] options = new ModOptionInt[4];
            int val = 0;
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = new ModOptionInt(val.ToString("0"), val);
                val++;
            }
            return options;
        }

        // +-------------+
        // |   GENERAL   |
        // +-------------+ 

        [ModOption(name: "Use Mod", tooltip: "Turns on/off the FromSoftware parries mod.", defaultValueIndex = 1, order = 0)]
        public static bool useFSParries;

        [ModOption(name: "Parry Type", tooltip: "Determines what type parry to perform.", defaultValueIndex = 1, order = 1)]
        public static ParryType parryType;

        [ModOptionSlider]
        [ModOption(name: "Parry SFX Volume", tooltip: "Determines the volume of the parry SFX.", valueSourceName = nameof(zeroToOneHundered), defaultValueIndex = 25, order = 2)]
        public static void ParrySFXVolumeChange(float parrySFXvolume)
        {
            parrySFXsource.volume = parrySFXvolume / 100f;
        }

        [ModOption(name: "Shield Only Parries", tooltip: "Determines if parries should only occur when the parry item is a shield.", defaultValueIndex = 0, order = 3)]
        public static bool shieldOnlyParries;

        // +-------------+
        // |   POSTURE   |
        // +-------------+ 
        [ModOption(name: "Posture Parry Mode", tooltip: "Determines what type of parry effect to use for a Posture parry. ", defaultValueIndex = 2, category = "Posture (Sekiro)", order = 0)]
        public static PostureParryMode postureParryMode;

        [ModOptionSlider]
        [ModOption(name: "Posture Min. Velocity", tooltip: "Determines the minimum velocity weapons must clash at to register as a Posture parry.", valueSourceName = nameof(zeroToEightWith2Tenths), defaultValueIndex = 30, category = "Posture (Sekiro)", order = 1)]
        public static float postureMinVelocity;

        [ModOption(name: "Posture Parry Count", tooltip: "Determines how many parries need to be done to stagger an enemy.", valueSourceName = nameof(oneToTen), defaultValueIndex = 2, category = "Posture (Sekiro)", order = 2)]
        public static int postureParryCount;

        [ModOption(name: "Use Posture Parry Sound", tooltip: "Determines whether to play a SFX on a posture parry (NOT on a stagger).", defaultValueIndex = 1, category = "Posture (Sekiro)", order = 3)]
        public static bool postureUseClashSFX;

        [ModOption(name: "Posture Parry SFX", tooltip: "Determines the parry sound that will play on a stagger for Sekiro parries.", defaultValueIndex = 3, category = "Posture (Sekiro)", order = 4)]
        public static ParrySound postureParrySound;

        // +--------------+
        // |    SLOWED    |
        // +--------------+ 

        [ModOptionSlider]
        [ModOption(name: "Slowed Parry Min. Velocity", tooltip: "Determines the minimum velocity weapons must clash at to register as a Dark Souls 1 parry.", valueSourceName = nameof(zeroToEightWith2Tenths), defaultValueIndex = 30, category = "Slowed (DeS, DS1, BB, DS3, DeSR, ER)", order = 0)]
        public static float slowedMinVelocity;

        [ModOptionSlider]
        [ModOption(name: "Slowed Duration", tooltip: "Determines the duration a creature is slowed for after a Dark Souls 1 parry.", valueSourceName = nameof(zeroToOneHundered), defaultValueIndex = 4, category = "Slowed (DeS, DS1, BB, DS3, DeSR, ER)", order = 1)]
        public static float slowedParryDuration;

        [ModOptionSlider]
        [ModOption(name: "Slowed Percentage", tooltip: "Determines a creature's speed on parry in percentage. This number should be low to notice a difference.", valueSourceName = nameof(zeroToOneHundered), defaultValueIndex = 10, category = "Slowed (DeS, DS1, BB, DS3, DeSR, ER)", order = 2)]
        public static float slowedParrySlow;

        [ModOption(name: "Slowed Parry SFX", tooltip: "Determines the parry sound that will play for a Dark Souls 1 parry.", defaultValueIndex = 0, category = "Slowed (DeS, DS1, BB, DS3, DeSR, ER)", order = 3)]
        public static ParrySound slowedParrySound;

        // +-------------+
        // |   STAGGER   |
        // +-------------+ 

        [ModOptionSlider]
        [ModOption(name: "Stagger Parry Min. Velocity", tooltip: "Determines the minimum velocity weapons must clash at to register as a Dark Souls 2 parry.", valueSourceName = nameof(zeroToEightWith2Tenths), defaultValueIndex = 30, category = "Stagger (DS2)", order = 0)]
        public static float staggerMinVelocity;

        [ModOptionSlider]
        [ModOption(name: "Stagger Delay", tooltip: "Determines how long to wait to destabilize the creature after a Dark Souls 2 parry.", valueSourceName = nameof(zeroToHundredWithTenths), defaultValueIndex = 3, category = "Stagger (DS2)", order = 1)]
        public static float staggerDelayDuration;

        [ModOption(name: "Stagger Parry SFX", tooltip: "Determines the parry sound that will play for a Dark Souls 2 parry.", defaultValueIndex = 1, category = "Stagger (DS2)", order = 2)]
        public static ParrySound staggerParrySound;

        // +--------------+
        // |    TIERED    |
        // +--------------+

        [ModOptionSlider]
        [ModOption(name: "Tier 1 Min. Velocity", tooltip: "Determines the minimum velocity weapons must clash at to register as a Tier 1 parry.", valueSourceName = nameof(zeroToEightWith2Tenths), defaultValueIndex = 30, category = "Tiered", order = 0)]
        public static float tier1MinParry;

        [ModOption(name: "Teir 1 Parry SFX", tooltip: "Determines the parry sound that will play on a Tier 1 parry.", defaultValueIndex = 0, category = "Tiered", order = 1)]
        public static ParrySound tier1ParrySFX;

        [ModOptionSlider]
        [ModOption(name: "Tier 2 Min. Velocity", tooltip: "Determines the minimum velocity weapons must clash at to register as a Tier 2 parry.", valueSourceName = nameof(zeroToEightWith2Tenths), defaultValueIndex = 40, category = "Tiered", order = 2)]
        public static float tier2MinParry;

        [ModOption(name: "Teir 2 Parry SFX", tooltip: "Determines the parry sound that will play on a Tier 2 parry.", defaultValueIndex = 1, category = "Tiered", order = 3)]
        public static ParrySound tier2ParrySFX;

        private Dictionary<Creature, int> allCreaturePoise = new Dictionary<Creature, int>();
        private static AudioSource parrySFXsource;
        private static AudioClip[] parrySFXclips = new AudioClip[8];
        /* 0: DS1
         * 1: DS2
         * 2: BB
         * 3: Sekiro
         * 4: SekiroHuge
         * 5: DeSR
         * 6: EldenRing
         * 7: Custom
         */

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            parrySFXsource = GameManager.local.gameObject.AddComponent<AudioSource>();
            GameManager.local.StartCoroutine(LoadSFX());
            EventManager.onCreatureKill += OnCreatureKill;
            EventManager.onCreatureAttackParry += OnCreatureAttackParry;
        }

        public override void ScriptUpdate()
        {
            base.ScriptUpdate();
            if (useFSParries && Player.currentCreature)
            {
                Player.currentCreature.TryRemoveSkill("ShockParry");
                Player.currentCreature.TryRemoveSkill("TemporalRiposte");
            }
        }

        private void OnCreatureAttackParry(Creature parriedCreature, Item parriedItem, Creature parryingCreature, Item parryingItem, CollisionInstance collisionInstance)
        {
            if (!useFSParries)
            {
                return;
            }

            if (parryingItem != null && parryingCreature == Player.currentCreature)
            {
                if (!shieldOnlyParries || (shieldOnlyParries && parryingItem.data.type == ItemData.Type.Shield))
                {
                    float velocity = parryingItem.physicBody.velocity.magnitude;
                    switch (parryType)
                    {
                        case ParryType.Slowed:
                            if (velocity >= slowedMinVelocity)
                            {
                                parrySFXsource.clip = parrySFXclips[(int)slowedParrySound];
                                GameManager.local.StartCoroutine(SlowedParry(parriedCreature));
                            }
                            break;
                        case ParryType.Stagger:
                            if (velocity >= staggerMinVelocity)
                            {
                                parrySFXsource.clip = parrySFXclips[(int)staggerParrySound];
                                GameManager.local.StartCoroutine(StaggerParry(parriedCreature));
                            }
                            break;
                        case ParryType.Posture:
                            if (velocity >= postureMinVelocity)
                            {
                                parrySFXsource.clip = parrySFXclips[(int)postureParrySound];
                                PostureParry(parriedCreature, collisionInstance);
                            }
                            break;
                        case ParryType.Tiered:
                            if (velocity >= tier2MinParry)
                            {
                                parrySFXsource.clip = parrySFXclips[(int)tier2ParrySFX];
                                GameManager.local.StartCoroutine(StaggerParry(parriedCreature));
                            }
                            else if (velocity >= tier1MinParry)
                            {
                                parrySFXsource.clip = parrySFXclips[(int)tier1ParrySFX];
                                GameManager.local.StartCoroutine(SlowedParry(parriedCreature));
                            }
                            break;
                    }
                }
            }
        }

        private void OnCreatureKill(Creature creature, Player player, CollisionInstance collisionInstance, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
                return;

            if (creature.animator.speed == slowedParrySlow)
                creature.animator.speed = 1.0f;

            if (allCreaturePoise.ContainsKey(creature))
                allCreaturePoise.Remove(creature);
        }

        private void PostureParry(Creature creature, CollisionInstance collisionInstance)
        {
            if (!allCreaturePoise.ContainsKey(creature))
                allCreaturePoise.Add(creature, postureParryCount);

            allCreaturePoise[creature]--;

            if (allCreaturePoise[creature] == 0)
            {
                if (postureParrySound == ParrySound.Sekiro)
                    Catalog.GetData<EffectData>("SekiroBigParry").Spawn(collisionInstance.contactPoint, Quaternion.identity).Play();

                parrySFXsource.Play();

                switch (postureParryMode)
                {
                    case PostureParryMode.Slowed:
                        GameManager.local.StartCoroutine(SlowedParry(creature));
                        break;
                    case PostureParryMode.Stagger:
                        GameManager.local.StartCoroutine(StaggerParry(creature));
                        break;
                    case PostureParryMode.Disarm:
                        DisarmCreature(creature);
                        break;
                }

                allCreaturePoise.Remove(creature);
            }
            else if (postureUseClashSFX)
            {
                Catalog.GetData<EffectData>("SekiroParry").Spawn(collisionInstance.contactPoint, Quaternion.identity).Play();
            }
        }

        private void DisarmCreature(Creature creature)
        {
            BrainModuleMelee module = creature.brain.instance.GetModule<BrainModuleMelee>();
            module.StopAttack(module, module.attack, module.attackCount);
            Creature.DisarmCreature(creature);
            creature.TryPush(
                Creature.PushType.Magic,
                -creature.brain.transform.forward,
                0);
        }

        private IEnumerator SlowedParry(Creature creature)
        {
            parrySFXsource.Play();
            creature.animator.speed = slowedParrySlow / 100;
            yield return new WaitForSeconds(slowedParryDuration);
            creature.animator.speed = 1.0f;
        }

        private IEnumerator StaggerParry(Creature creature)
        {
            parrySFXsource.Play();
            yield return new WaitForSeconds(staggerDelayDuration);
            creature.TryPush(Creature.PushType.Magic, -creature.brain.transform.forward, 3, 0);
        }

        private IEnumerator LoadSFX()
        {
            Catalog.LoadAssetAsync<AudioClip>("ChillioX.FromSoftwareParry.DS1", value => parrySFXclips[0] = value, "ChillioX");
            Catalog.LoadAssetAsync<AudioClip>("ChillioX.FromSoftwareParry.DS2", value => parrySFXclips[1] = value, "ChillioX");
            Catalog.LoadAssetAsync<AudioClip>("ChillioX.FromSoftwareParry.Bloodborne", value => parrySFXclips[2] = value, "ChillioX");
            Catalog.LoadAssetAsync<AudioClip>("ChillioX.FromSoftwareParry.Sekiro", value => parrySFXclips[3] = value, "ChillioX");
            Catalog.LoadAssetAsync<AudioClip>("ChillioX.FromSoftwareParry.SekiroHuge", value => parrySFXclips[4] = value, "ChillioX");
            Catalog.LoadAssetAsync<AudioClip>("ChillioX.FromSoftwareParry.DeSR", value => parrySFXclips[5] = value, "ChillioX");
            Catalog.LoadAssetAsync<AudioClip>("ChillioX.FromSoftwareParry.EldenRing", value => parrySFXclips[6] = value, "ChillioX");

            if (parrySFXclips.Length == 0)
            {
                Debug.LogError("(FromSoftwareParry) Unable to find any parry sound effects!");
            }

            string mp3File = Directory.GetFiles(Application.streamingAssetsPath + "/Mods/FromSoftwareParry", "*.mp3", SearchOption.AllDirectories)[0];
            UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip("file://" + mp3File, AudioType.MPEG);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                parrySFXclips[7] = DownloadHandlerAudioClip.GetContent(req);
            }
            else
            {
                Debug.LogError("(FromSoftwareParry) Unable to load custom MP3!");
            }
        }
    }
}
