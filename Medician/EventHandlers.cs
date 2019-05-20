using System;
using System.Collections.Generic;
using System.Linq;
using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.EventSystem.Events;

namespace Medician
{
    public class EventHandlers : IEventHandlerSpawn, IEventHandlerTeamRespawn,
        IEventHandlerFixedUpdate, IEventHandlerDisconnect, IEventHandlerPlayerJoin,
        IEventHandlerWaitingForPlayers, IEventHandlerSetConfig,
        IEventHandlerPlayerHurt, IEventHandlerShoot, IEventHandlerThrowGrenade, IEventHandlerGrenadeExplosion
    {
        private readonly Medician plugin;

        private bool friendly_fire = true;

        public EventHandlers(Medician plugin)
        {
            this.plugin = plugin;
        }

        public void OnSpawn(PlayerSpawnEvent ev)
        {
            if (ev.Player.TeamRole.Role == Role.NTF_SCIENTIST)
            {
                ev.Player.PersonalClearBroadcasts();
                ev.Player.PersonalBroadcast(15,
                    $"<color=red><size=60>Sei stato selezionato come medico.</size></color>\\n<color=orange>Il tuo compito è aiutare i tuoi compagni nel momento del bisogno con la tua arma medica ( {medicItem.ToString()} )</color>",
                    false);
                for (int i = 0; i < plugin.GetConfigInt("medician_medikits"); i++)
                {
                    ev.Player.GiveItem(ItemType.MEDKIT);
                }

                if (ev.Player == medician)
                    medician = null;
            }
            else
            {
                if( ev.Player == medician )
                    medician.ChangeRole(Role.NTF_SCIENTIST);
            }
        }

        private Player medician = null;

        public void OnTeamRespawn(TeamRespawnEvent ev)
        {
            if (!ev.SpawnChaos)
            {
                if (ev.PlayerList.Count >= 5)
                {
                    medician = ev.PlayerList.Skip(4).First();
                    ev.PlayerList = ev.PlayerList.Where(p => p != medician).ToList();
                    medician.ChangeRole(Role.NTF_SCIENTIST);
                }
            }
        }

        private Dictionary<string, bool> fire_once = new Dictionary<string, bool>();

        public void OnFixedUpdate(FixedUpdateEvent ev)
        {
            foreach (var player in plugin.Server.GetPlayers())
            {
                if (player.GetCurrentItem().ItemType == medicItem)
                {
                    if (fire_once[player.IpAddress])
                    {
                        player.PersonalClearBroadcasts();
                        player.PersonalBroadcast(3,
                            "<color=red>Questa è un arma medica</color>\\n<color=green>serve a curare i tuoi compagni</color>",
                            false);
                        fire_once[player.IpAddress] = false;
                    }
                }
                else
                    fire_once[player.IpAddress] = true;
            }

            // ReSharper disable once RedundantCheckBeforeAssignment
            if (last_grenade != Smod2.API.Team.NONE)
            {
                last_grenade = Smod2.API.Team.NONE;
            }
        }

        public void OnDisconnect(DisconnectEvent ev)
        {
            fire_once.Remove(ev.Connection.IpAddress);
        }

        public void OnPlayerJoin(PlayerJoinEvent ev)
        {
            if(!fire_once.ContainsKey(ev.Player.IpAddress))
                fire_once.Add(ev.Player.IpAddress, true);
        }

        private ItemType medicItem = ItemType.NULL;
        private DamageType medicDamage = DamageType.NONE;

        public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
        {
            fire_once.Clear();
            if (Enum.TryParse(plugin.GetConfigString("medician_medic_weapon"), out ItemType item))
            {
                medicItem = item;
            }

            if (Enum.TryParse(plugin.GetConfigString("medician_medic_weapon"), out DamageType damage))
            {
                medicDamage = damage;
            }

            foreach (var pair in player_grenades)
            {
                pair.Value.Clear();
            }
            
            player_grenades.Clear();
        }


        public void OnPlayerHurt(PlayerHurtEvent ev)
        {
            if(ev.DamageType == DamageType.LURE || ev.DamageType == DamageType.NUKE || ev.DamageType == DamageType.WALL || ev.DamageType == DamageType.DECONT || ev.DamageType == DamageType.CONTAIN || ev.DamageType == DamageType.FLYING || ev.DamageType == DamageType.FALLDOWN || ev.DamageType== DamageType.RAGDOLLLESS)
                return;
            if (medicDamage != DamageType.NONE && ev.DamageType == medicDamage)
            {
                float damage = ev.Damage * plugin.GetConfigInt("medician_medic_multiplier") / 100;
                if (ev.Player.GetHealth() < ev.Player.TeamRole.MaxHP)
                {
                    if (ev.Player.TeamRole.MaxHP - ev.Player.GetHealth() < damage)
                    {
                        ev.Damage = 0;
                        ev.Player.SetHealth(ev.Player.TeamRole.MaxHP);
                    }
                    else
                        ev.Damage = -damage;
                    ev.Player.PersonalClearBroadcasts();
                    ev.Player.PersonalBroadcast(5,
                        $"<color=light_green>Sei stato curato da:</color> <color=orange>{ev.Attacker.Name}</color>",
                        true);
                }
                else
                    ev.Damage = 0;
            }
            else
            {
                if (!friendly_fire)
                    if (!allowDamage(ev.Attacker, ev.Player, ev.DamageType))
                    {
                        ev.Damage = 0;
                        ev.DamageType = DamageType.NONE;
                    }
            }
        }

        private bool allowDamage(Player p1, Player p2, DamageType type)
        {
            if (type == medicDamage)
                return true;
            if (p1 == null || p2 == null)
                return true;
            if (p1.Name.ToLower().Trim() == "server" || p2.Name.ToLower().Trim() == "server")
                return true;
            
            if (p1.PlayerId == p2.PlayerId)
                return true;
            
            Smod2.API.Team t1 = p1.TeamRole.Team;
            Smod2.API.Team t2 = p2.TeamRole.Team;

            if (type == DamageType.FRAG)
            {
                t1 = last_grenade;
            }

            if (t1 == t2)
                return false;
            if (t1 == Smod2.API.Team.CLASSD && t2 == Smod2.API.Team.CHAOS_INSURGENCY)
                return false;
            if (t2 == Smod2.API.Team.CLASSD && t1 == Smod2.API.Team.CHAOS_INSURGENCY)
                return false;
            if (t1 == Smod2.API.Team.SCIENTIST && t2 == Smod2.API.Team.NINETAILFOX)
                return false;
            if (t2 == Smod2.API.Team.SCIENTIST && t1 == Smod2.API.Team.NINETAILFOX)
                return false;
            return true;
        }

        public void OnSetConfig(SetConfigEvent ev)
        {
            if (ev.Key.Equals("friendly_fire"))
            {
                plugin.Info($"frinedly_fire: {(bool)ev.Value}");
                friendly_fire = (bool) ev.Value;
                ev.Value = true;
            }
        }

        public void OnShoot(PlayerShootEvent ev)
        {
            if (!allowDamage(ev.Player, ev.Target, ev.Weapon))
            {
                ev.ShouldSpawnHitmarker = false;
            }
        }


        private Dictionary<String, Queue<Smod2.API.Team>> player_grenades = new Dictionary<String, Queue<Smod2.API.Team>>();

        private Smod2.API.Team last_grenade = Smod2.API.Team.NONE;
        
        public void OnThrowGrenade(PlayerThrowGrenadeEvent ev)
        {
            if (!player_grenades.ContainsKey(ev.Player.IpAddress))
            {
                player_grenades.Add(ev.Player.IpAddress,new Queue<Smod2.API.Team>());
            }
            
            player_grenades[ev.Player.IpAddress].Enqueue(ev.Player.TeamRole.Team);
        }

        public void OnGrenadeExplosion(PlayerGrenadeExplosion ev)
        {
            if (player_grenades.ContainsKey(ev.Player.IpAddress) && player_grenades[ev.Player.IpAddress].Count>0)
            {
                last_grenade = player_grenades[ev.Player.IpAddress].Dequeue();
            }
            else
            {
                last_grenade = ev.Player.TeamRole.Team;
            }
        }
        
        
    }
}