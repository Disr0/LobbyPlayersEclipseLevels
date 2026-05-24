using BepInEx;
using RoR2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;


namespace LobbyPlayersEclipseLevels
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LobbyPlayersEclipseLevels : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "diselgonk";
        public const string PluginName = "LobbyPlayersEclipseLevels";
        public const string PluginVersion = "1.0.0";

        private List<NetworkUserId> processedUserIds = new List<NetworkUserId>();
        public void Awake()
        {
            Log.Init(Logger);

            On.RoR2.PreGameController.RecalculateModifierAvailability += PreGameController_RecalculateModifierAvailability;
            On.RoR2.PreGameController.OnDestroy += PreGameController_OnDestroy;
            On.RoR2.PreGameController.OnNetworkUserLost += PreGameController_OnNetworkUserLost;
        }

        private void PreGameController_OnNetworkUserLost(On.RoR2.PreGameController.orig_OnNetworkUserLost orig, PreGameController self, NetworkUser networkUser)
        {
            orig(self, networkUser);

            if (!NetworkServer.active)
                return;

            Log.Debug($"Removed user id {networkUser.id} from processed");
            processedUserIds.Remove(networkUser.id);
        }

        private void PreGameController_OnDestroy(On.RoR2.PreGameController.orig_OnDestroy orig, PreGameController self)
        {
            orig(self);

            if (!NetworkServer.active)
                return;

            Log.Debug($"Cleared processed user ids");
            processedUserIds.Clear();
        }

        private void PreGameController_RecalculateModifierAvailability(On.RoR2.PreGameController.orig_RecalculateModifierAvailability orig, PreGameController self)
        {
            orig(self);

            if (!NetworkServer.active)
                return;

            ReadOnlyCollection<NetworkUser> readOnlyInstancesList = NetworkUser.readOnlyInstancesList;
            foreach (var user in readOnlyInstancesList)
            {
                if (processedUserIds.Contains(user.id))
                    continue;
                if (user.isLocalPlayer)
                    continue;

                bool printed = PrintUserEclipseLevels(user);
                if (printed)
                {
                    Log.Debug($"Added user id {user.id} to processed");
                    processedUserIds.Add(user.id);
                }
            }
        }
        private static bool PrintUserEclipseLevels(NetworkUser user)
        {
            string name = user.userName;

            if (string.IsNullOrEmpty(name))
            {
                Log.Warning($"Couldn't print eclipse levels. {name} - username is empty. How so?");
                return false;
            }

            var survivorEclipseLevels = new Dictionary<RoR2.SurvivorDef, int>();
            foreach (var survivor in RoR2.SurvivorCatalog.allSurvivorDefs)
            {
                int survivorEclipseLevel = RoR2.EclipseRun.GetNetworkUserSurvivorCompletedEclipseLevel(user, survivor);
                survivorEclipseLevels.Add(survivor, survivorEclipseLevel);
            }

            string output = $"{name}: ";
            foreach (var (survivor, eclipseLevel) in survivorEclipseLevels)
            {
                if (eclipseLevel == 0)
                    continue;

                string survivorDisplayName = RoR2.Language.GetString(survivor.displayNameToken);
                string survivorColor = ColorUtility.ToHtmlStringRGBA(survivor.primaryColor);
                if (!string.IsNullOrEmpty(survivorColor)) {
                    output += $"<color=#{survivorColor}>";
                }
                output += $"{survivorDisplayName} - {eclipseLevel} ";
                if (!string.IsNullOrEmpty(survivorColor))
                {
                    output += $"</color>";
                }
            }
            if (survivorEclipseLevels.All(kvp => kvp.Value == 0))
            {
                return false;
            }

            Log.Info($"{output}");
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"{output}"
            });
            return true;
        }
    }
}
