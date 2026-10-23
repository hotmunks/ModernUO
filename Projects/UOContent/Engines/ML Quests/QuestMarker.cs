using System;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.MLQuests
{
    /// <summary>
    ///     Shows a "!" above quest givers that have a quest the viewing player can accept right now.
    ///     The client has no quest-icon support, so the marker is private overhead text re-sent on an interval.
    /// </summary>
    public static class QuestMarker
    {
        // Largest of the client's ASCII fonts.
        private const int Font = 0;
        private const int Range = 12;

        private static bool _enabled;
        private static int _hue;
        private static TimeSpan _interval;

        public static void Configure()
        {
            _enabled = ServerConfiguration.GetOrUpdateSetting("questSystem.questMarkers", true);
            _hue = ServerConfiguration.GetOrUpdateSetting("questSystem.questMarkerHue", 0x35);

            // Overhead text lingers about 4 seconds; resending sooner stacks markers above the head.
            _interval = ServerConfiguration.GetOrUpdateSetting("questSystem.questMarkerInterval", TimeSpan.FromSeconds(5));
        }

        public static void Initialize()
        {
            if (_enabled && MLQuestSystem.Enabled)
            {
                Timer.DelayCall(_interval, _interval, Tick);
            }
        }

        private static void Tick()
        {
            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile is not PlayerMobile { Alive: true } pm || pm.Map == null || pm.Map == Map.Internal)
                {
                    continue;
                }

                var context = MLQuestSystem.GetContext(pm);

                foreach (var bc in pm.Map.GetMobilesInRange<BaseCreature>(pm.Location, Range))
                {
                    if (bc.CanGiveMLQuest && pm.CanSee(bc) && HasOfferableQuest(bc, pm, context))
                    {
                        ns.SendMessage(bc.Serial, bc.Body, MessageType.Label, _hue, Font, true, bc.Language, bc.Name, "!");
                    }
                }
            }
        }

        private static bool HasOfferableQuest(IQuestGiver quester, PlayerMobile pm, MLQuestContext context)
        {
            if (context?.IsFull == true)
            {
                return false;
            }

            foreach (var quest in quester.MLQuests)
            {
                if (!quest.IsChainTriggered && context?.IsDoingQuest(quest) != true &&
                    quest.CanOffer(quester, pm, context, false))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
