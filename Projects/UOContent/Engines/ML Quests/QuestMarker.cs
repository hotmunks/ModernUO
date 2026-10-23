using System;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.MLQuests
{
    /// <summary>
    ///     Marks quest givers that have a quest the viewing player can accept right now with a hued effect
    ///     that only that player sees. Effects carry no text, so nothing reaches the journal; each one expires
    ///     on its own, so the marker is re-sent on an interval.
    /// </summary>
    public static class QuestMarker
    {
        private const int Range = 12;
        private const int Speed = 10;

        private static bool _enabled;
        private static int _hue;
        private static int _effect;
        private static int _duration;
        private static TimeSpan _interval;

        public static void Configure()
        {
            _enabled = ServerConfiguration.GetOrUpdateSetting("questSystem.questMarkers", true);
            _hue = ServerConfiguration.GetOrUpdateSetting("questSystem.questMarkerHue", 0x35);

            // Sparkles
            _effect = ServerConfiguration.GetOrUpdateSetting("questSystem.questMarkerEffect", 0x376A);
            _interval = ServerConfiguration.GetOrUpdateSetting("questSystem.questMarkerInterval", TimeSpan.FromSeconds(10));

            // Shorter than the interval so the marker pulses instead of sparkling nonstop.
            _duration = ServerConfiguration.GetOrUpdateSetting("questSystem.questMarkerDuration", 60);
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
            Span<byte> effect = stackalloc byte[OutgoingEffectPackets.HuedEffectLength].InitializePacket();

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
                        // Create* skips a buffer that already holds a packet, so clear it for each quest giver.
                        effect.InitializePacket();
                        OutgoingEffectPackets.CreateTargetHuedEffect(effect, bc, _effect, Speed, _duration, _hue, 0);
                        ns.Send(effect);
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
