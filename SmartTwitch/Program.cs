using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace SmartTwitch
{
    class Program
    {
        #region Private Members

        private static AIHeroClient myHero = Player.Instance;

        private static Spell.Active Q;
        private static Spell.Skillshot W;
        private static Spell.Active E;
        private static Spell.Active R;

        private static bool IsQCasting => Player.Instance.HasBuff("TwitchHideInShadows");
        #endregion

        #region Public Properties
        /// <summary>
        /// TODO: Add Drawings and Misc menu
        /// </summary>
        public static Menu Menu, ComboMenu, HarassMenu, LaneAndJungleClearMenu;


        #endregion



        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }


        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (myHero.ChampionName != "Twitch")
                return;
            InitMenu();
            DeclareSpellValues();
            Chat.Print("SmartTwitch[Alpha] was Loaded! GL&HF :3", Color.DarkCyan);
            Game.OnUpdate += Game_OnUpdate;

        }

        private static void Game_OnUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneAndJungleClear();
            }
        }


        #region Orbwalker keys's logic

        ///TODO: Add Flee logic, Add smart last hit to <see cref="Harass"/>, more logic to <see cref="Combo"/>

        /// <summary>
        /// Combo Logic
        /// </summary>
        private static void Combo()
        {
            var useQ = ComboMenu["ComboQ"].Cast<CheckBox>().CurrentValue;
            var useW = ComboMenu["ComboW"].Cast<CheckBox>().CurrentValue;
            var useE = ComboMenu["ComboE"].Cast<CheckBox>().CurrentValue;
            var useR = ComboMenu["ComboR"].Cast<CheckBox>().CurrentValue;
            var minR = ComboMenu["MinR"].Cast<Slider>().CurrentValue;
            var minE = ComboMenu["MinESTCE"].Cast<Slider>().CurrentValue;

            foreach (var target in EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(E.Range) && !e.IsDead && !e.IsZombie))
            {
                if (useQ && Q.IsReady() && target.IsValidTarget(W.Range))
                {
                    Q.Cast();
                }

                if (useW && W.IsReady() && !IsQCasting && target.IsValidTarget(W.Range) && myHero.Distance(target) > Player.Instance.GetAutoAttackRange(target))
                {
                    var pred = W.GetPrediction(target);
                    if (pred.HitChance >= HitChance.Medium)
                    {
                        W.Cast(pred.CastPosition);
                    }
                }

                if (useE && E.IsReady() && E.IsInRange(target) && target.HasBuff("twitchdeadlyvenom"))
                {
                    if (ComboMenu["Combo" + target.ChampionName].Cast<CheckBox>().CurrentValue && Stacks(target) >= minE)
                    {
                        E.Cast();
                    }
                }

                if (useR && R.IsReady() && myHero.Position.CountEnemiesInRange(E.Range) >= minR)
                {
                    R.Cast();
                }
            }
        }

        /// <summary>
        /// Harrass Logic
        /// </summary>
        private static void Harass()
        {
            var useW = HarassMenu["HarassW"].Cast<CheckBox>().CurrentValue;
            var mana = HarassMenu["MHarrass"].Cast<Slider>().CurrentValue;
            var useE = HarassMenu["HarassE"].Cast<CheckBox>().CurrentValue;
            var minE = HarassMenu["HminESTCE"].Cast<Slider>().CurrentValue;

            if (Player.Instance.ManaPercent <= mana)
                return;

            foreach (var target in EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(E.Range) && !e.IsDead && !e.IsZombie))
            {
                if (useW && W.IsReady() && !IsQCasting && target.IsValidTarget(W.Range))
                {
                    var wpred = W.GetPrediction(target);
                    if (wpred.HitChance >= HitChance.Medium)
                    {
                        W.Cast(wpred.CastPosition);
                    }
                }

                if (useE && E.IsReady() && E.IsInRange(target) && target.HasBuff("twitchdeadlyvenom"))
                {
                    if (HarassMenu["eharass" + target.ChampionName].Cast<CheckBox>().CurrentValue && Stacks(target) >= minE)
                    {
                        E.Cast();
                    }
                }
            }
        }

        /// <summary>
        /// Lane and Jungle Clear Logic
        /// </summary>
        private static void LaneAndJungleClear()
        {
            var mana = LaneAndJungleClearMenu["M"].Cast<Slider>().CurrentValue;
            var useW = LaneAndJungleClearMenu["W"].Cast<CheckBox>().CurrentValue;
            var useE = LaneAndJungleClearMenu["E"].Cast<CheckBox>().CurrentValue;
            var minm = LaneAndJungleClearMenu["MLaneAndJungleClear"].Cast<Slider>().CurrentValue;
            var minS = LaneAndJungleClearMenu["MinS"].Cast<Slider>().CurrentValue;
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position, E.Range).ToArray();
            var monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters().FirstOrDefault(a => a.IsValidTarget(E.Range) && (a.BaseSkinName == "SRU_Dragon" || a.BaseSkinName == "SRU_Baron"
                                                                                                                                 || a.BaseSkinName == "SRU_Blue" || a.BaseSkinName == "SRU_Red" || a.BaseSkinName == "SRU_Dragon_Air" || a.BaseSkinName == "SRU_Dragon_Elder" || a.BaseSkinName == "SRU_Dragon_Earth"
                                                                                                                                 || a.BaseSkinName == "SRU_Dragon_Fire" || a.BaseSkinName == "SRU_Dragon_Water"));

            var wCal = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(minions, W.Width, (int)W.Range);
            if (Player.Instance.ManaPercent < mana) return;
            {
                W.Cast(wCal.CastPosition);
            }

            if (useE && E.IsReady())
            {
                int eCal = minions.Where(e => e.Distance(myHero.Position) < (E.Range) && Stacks(e) >= minS).Count(); ;
                if (eCal >= minm)
                {
                    E.Cast();
                }
            }

            if (monsters != null)
            {
                if (useW && W.CanCast(monsters) && W.IsInRange(monsters) && Stacks(monsters) <= 4)
                {
                    W.Cast(monsters);
                }

                if (useE && E.IsReady() && E.IsInRange(monsters) && monsters.HasBuff("twitchdeadlyvenom") && Stacks(monsters) <= 4)
                {
                    E.Cast();
                }
            }
        }

        #endregion

        #region Helper Methods
        ///TODO: Damage Indicator, Item Usage, Secure Kill, GapCloser, LastHit with E


        /// <summary>
        /// E Stacks on <see cref="target"/>
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static int Stacks(Obj_AI_Base target)
        {
            var estacks = 0;
            for (var t = 1; t < 7; t++)
            {
                if (ObjectManager.Get<Obj_GeneralParticleEmitter>().Any(s => s.Position.Distance(target.ServerPosition) <= 175 && s.Name == "twitch_poison_counter_0" + t + ".troy"))
                {
                    estacks = t;
                }
            }
            return estacks;
        }

        #endregion

        #region Init Methods

        /// <summary>
        /// Declare Spell Values
        /// </summary>
        private static void DeclareSpellValues()
        {
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 950, SkillShotType.Circular, 250, 1400, 275) { AllowedCollisionCount = int.MaxValue };
            E = new Spell.Active(SpellSlot.E, 1200);
            R = new Spell.Active(SpellSlot.R);
        }

        /// <summary>
        /// Initialize the Menu 
        /// TODO: Add Drawings, Misc(Items, Smart Logic, Secure Kill)
        /// </summary>
        private static void InitMenu()
        {
            Menu = MainMenu.AddMenu("SmartTwitch", "Twitch");
            ComboMenu = Menu.AddSubMenu("Combo Settings", "Combo");
            ComboMenu.AddGroupLabel("Combo Settings");
            ComboMenu.Add("ComboQ", new CheckBox("Use Q?"));
            ComboMenu.Add("ComboW", new CheckBox("Use W?"));
            ComboMenu.AddGroupLabel("E Settings");
            ComboMenu.Add("ComboE", new CheckBox("Use E?", false));
            ComboMenu.AddGroupLabel("Use E on:");
            foreach (var target in EntityManager.Heroes.Enemies)
            {
                ComboMenu.Add("ecombo" + target.ChampionName, new CheckBox("" + target.ChampionName));
            }
            ComboMenu.Add("MinESTCE", new Slider("Minimum stacks to cast E?", 6, 0, 6));

            ComboMenu.AddSeparator();
            ComboMenu.Add("ComboR", new CheckBox("Use R?"));
            ComboMenu.Add("MinR", new Slider("Minimum Enemies to use R on?", 3, 0, 5));

            HarassMenu = Menu.AddSubMenu("Harass Settings", "Harass");
            HarassMenu.AddGroupLabel("Harass Settings");
            HarassMenu.Add("HarassW", new CheckBox("Use W?", false));
            HarassMenu.AddGroupLabel("E Settings");
            HarassMenu.Add("HarassE", new CheckBox("Use E?", false));
            HarassMenu.Add("HminESTCE", new Slider("Minimum stacks to cast E?", 5, 0, 6));
            HarassMenu.AddGroupLabel("Use E on:");
            foreach (var target in EntityManager.Heroes.Enemies)
            {
                HarassMenu.Add("eharass" + target.ChampionName, new CheckBox("" + target.ChampionName));
            }
            HarassMenu.Add("MHarrass", new Slider("Minimum Mana For Harass", 40));

            LaneAndJungleClearMenu = Menu.AddSubMenu("Lane and Jungle Clear Settings", "LaneClear");
            LaneAndJungleClearMenu.AddGroupLabel("Lane and Jungle Clear Settings");
            LaneAndJungleClearMenu.AddLabel("E Settings");
            LaneAndJungleClearMenu.Add("E", new CheckBox("Use E?", false));
            LaneAndJungleClearMenu.Add("MinS", new Slider("Minimum stacks to cast E?", 4, 1, 6));
            LaneAndJungleClearMenu.AddLabel("W Settings");
            LaneAndJungleClearMenu.Add("W", new CheckBox("Use W?", false));
            LaneAndJungleClearMenu.AddLabel("Mana Settings");
            LaneAndJungleClearMenu.Add("MLaneAndJungleClear", new Slider("Min Mana For Lane and Jungle Clear?", 40));
        }

        #endregion

    }
}
