// <copyright file="EmberSpirit.cs" company="Ensage">
//    Copyright (c) 2017 Ensage.
// </copyright>

namespace Vaper.Heroes
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Ensage;
    using Ensage.Common.Extensions;
    using Ensage.SDK.Abilities.Items;
    using Ensage.SDK.Abilities.npc_dota_hero_ember_spirit;
    using Ensage.SDK.Helpers;
    using Ensage.SDK.Inventory;
    using Ensage.SDK.Menu;

    using SharpDX;

    using Vaper.OrbwalkingModes;

    using Color = System.Drawing.Color;

    [ExportHero(HeroId.npc_dota_hero_ember_spirit)]
    public class EmberSpirit : BaseHero
    {
        public item_abyssal_blade AbyssalBlade { get; private set; }

        public ember_spirit_activate_fire_remnant Remnant { get; private set; }

        public MenuItem<bool> RemnantCountdown { get; private set; }

        public ember_spirit_fire_remnant ActivateFireRemnant { get; private set; }

        public ember_spirit_flame_guard FlameGuard { get; private set; }
        
        public ember_spirit_sleight_of_fist Fist { get; private set; }

        public ember_spirit_searing_chains Chains { get; private set; }
        
        public float CurrentCountdown { get; private set; }
        
        public float CountPrd { get; private set; } // 0.03221f; // = 15%

        protected override VaperOrbwalkingMode GetOrbwalkingMode()
        {
            return new EmberSpiritOrbwalker(this);
        }

        protected override void InventoryChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var newItem in e.NewItems.OfType<InventoryItem>())
                {
                    switch (newItem.Id)
                    {
                        case AbilityId.item_abyssal_blade:
                            this.AbyssalBlade = this.Ensage.AbilityFactory.GetAbility<item_abyssal_blade>(newItem.Item);
                            break;
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var oldItem in e.OldItems.OfType<InventoryItem>())
                {
                    switch (oldItem.Id)
                    {
                        case AbilityId.item_abyssal_blade:
                            this.AbyssalBlade = null;
                            break;
                    }
                }
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            this.Fist = this.Ensage.AbilityFactory.GetAbility<ember_spirit_sleight_of_fist>();
            this.Chains = this.Ensage.AbilityFactory.GetAbility<ember_spirit_searing_chains>();
            this.Remnant = this.Ensage.AbilityFactory.GetAbility<ember_spirit_activate_fire_remnant>();
            this.ActivateFireRemnant = this.Ensage.AbilityFactory.GetAbility<ember_spirit_fire_remnant>();
            this.FlameGuard = this.Ensage.AbilityFactory.GetAbility<ember_spirit_flame_guard>();

            this.AbyssalBlade = this.Ensage.AbilityFactory.GetItem<item_abyssal_blade>();

            var factory = this.Menu.Hero.Factory;
            this.RemnantCountdown = factory.Item("Show Remnant Countdown", true);
            this.RemnantCountdown.PropertyChanged += this.RemnantCountdownPropertyChanged;

            this.Ensage.Renderer.Draw += this.OnDraw;
            Entity.OnInt32PropertyChange += this.OnNetworkActivity;
        }

        protected override void OnDeactivate()
        {
            Entity.OnInt32PropertyChange -= this.OnNetworkActivity;
            this.Ensage.Renderer.Draw -= this.OnDraw;

            base.OnDeactivate();
        }

        protected override async Task OnKillsteal(CancellationToken token)
        {
            if (!this.Owner.IsAlive || !this.Fist.CanBeCasted)
            {
                await Task.Delay(125, token);
                return;
            }

            var killstealTarget = EntityManager<Hero>.Entities.FirstOrDefault(
                x => x.IsAlive
                     && x.Team != this.Owner.Team
                     && !x.IsIllusion
                     && this.Fist.CanHit(x)

                     && this.Fist.GetDamage(x) > x.Health);

            if (killstealTarget != null)
            {
                if (this.Fist.UseAbility(killstealTarget))
                {
                    var castDelay = this.Fist.GetCastDelay(killstealTarget);
                    await this.AwaitKillstealDelay(castDelay, token);
                }
            }

            await Task.Delay(125, token);
        }

}
