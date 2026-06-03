using BepInEx;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
#if DEBUG
using UnityHotReloadNS;
#endif


namespace LobbyPlayersEclipseLevels
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LobbyPlayersEclipseLevels : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "diselgonk";
        public const string PluginName = "LobbyPlayersEclipseLevels";
        public const string PluginVersion = "1.1.0";

        public void Awake()
        {
            Log.Init(Logger);

            On.RoR2.UI.VoteInfoPanelController.UpdateElements += VoteInfoPanelController_UpdateElements;
            NetworkUser.onNetworkUserUnlockablesUpdated += NetworkUser_OnNetworkUserUnlockablesUpdated;
        }

        private void NetworkUser_OnNetworkUserUnlockablesUpdated(NetworkUser networkUser)
        {
#if DEBUG
            Log.Debug("NetworkUserUnlockablesUpdated. Updating user eclipse levels.");
#endif
            eclipseTooltipCache[networkUser.id] = GetUserEclipseLevelsFormatted(networkUser);
        }

        private readonly Dictionary<NetworkUserId, string> eclipseTooltipCache = new();
        private void VoteInfoPanelController_UpdateElements(On.RoR2.UI.VoteInfoPanelController.orig_UpdateElements orig, RoR2.UI.VoteInfoPanelController self) 
        {
            orig(self);

            if (!NetworkServer.active)
            {
                return;
            }

            int playerVoteCount = self.voteController.GetVoteCount();
            for (int i = 0; i < playerVoteCount; i++)
            {
                UserVote vote = self.voteController.GetVote(i);
                if (!vote.networkUserObject)
                {
                    Log.Warning("No vote.networkUserObject");
                    continue;
                }

                var networkUser = vote.networkUserObject.GetComponent<NetworkUser>();
                eclipseTooltipCache.TryGetValue(networkUser.id, out string? tooltipText);
                if (tooltipText != null)
                {
                    self.indicators[i].tooltipProvider.overrideBodyText = tooltipText;
                }
            }
        }

        /// <summary>
        /// Gets eclipse levels for a network user.
        /// Works only when you are the host. Otherwise players eclipse levels are always empty.
        /// </summary>
        /// <param name="user">The network user.</param>
        /// <returns>
        /// A formatted string containing the user's eclipse levels; otherwise,
        /// <c>null</c> if the levels are empty.
        /// </returns>
        private static string GetUserEclipseLevelsFormatted(NetworkUser user)
        {
            string name = user.userName;
            var survivorEclipseLevels = new Dictionary<RoR2.SurvivorDef, int>();
            foreach (var survivor in RoR2.SurvivorCatalog.orderedSurvivorDefs)
            {
                int survivorEclipseLevel = RoR2.EclipseRun.GetNetworkUserSurvivorCompletedEclipseLevel(user, survivor);
                survivorEclipseLevels.Add(survivor, survivorEclipseLevel);
            }

            string output = "Finished eclipses: <br>";
            foreach (var (survivor, eclipseLevel) in survivorEclipseLevels)
            {
                if (eclipseLevel == 0)
                    continue;

                string survivorDisplayName = RoR2.Language.GetString(survivor.displayNameToken).Trim();
                if (string.IsNullOrEmpty(survivorDisplayName))
                {
                    Log.Warning($"No survivor display name for {survivor.cachedName}");
                    continue;
                }

                // Getting survivor body because survivor.primaryColor is set incorrectly for character from DLC 2 and 3... gearbox...
                CharacterBody survivorBody = survivor.bodyPrefab?.GetComponent<CharacterBody>(); 
                Color survivorColor = survivorBody.bodyColor;
                survivorColor.a = 1f; // Fully opaque, just in case
                string survivorColorHtmlString = ColorUtility.ToHtmlStringRGB(survivorBody.bodyColor);

                if (!string.IsNullOrEmpty(survivorColorHtmlString)) {
                    output += $"<color=#{survivorColorHtmlString}>";
                }
                output += $"{survivorDisplayName} - ";
                if (eclipseLevel == 8)
                {
                    output += $"<i>{eclipseLevel}</i>";
                } 
                else
                {
                    output += $"{eclipseLevel}";
                }
                if (!string.IsNullOrEmpty(survivorColorHtmlString))
                {
                    output += $"</color><br>";
                }
            }
            if (survivorEclipseLevels.All(kvp => kvp.Value == 0))
            {
                output += "None";
                return output;
            }
            return output;
        }
#if DEBUG
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.F2))
            {
                Log.Debug("Reloading");
                UnityHotReload.LoadNewAssemblyVersion(
                    typeof(LobbyPlayersEclipseLevels).Assembly,
                    BuildInfo.TargetPath
                );
            }
        }
#endif
    }
}
