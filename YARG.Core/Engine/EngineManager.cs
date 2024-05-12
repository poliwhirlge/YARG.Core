using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.WebSockets;
using YARG.Core.Chart;
using YARG.Core.Engine.Guitar;
using YARG.Core.Engine.Guitar.Engines;
using YARG.Core.Engine.Drums.Engines;
using YARG.Core.Game;
using YARG.Core.Logging;

namespace YARG.Core.Engine
{
    public class EngineManager
    {
        private List<YargProfile> YargProfiles = new();
        private List<BaseEngine> Engines = new();
        //private SongChart Chart;
        private List<UnisonPhrase> UnisonPhrases;
        private uint[] NumPlayersRequiredToHitPhrase;
        private uint[] NumPlayersHaveHitPhrase;
        private Dictionary<Guid, BaseEngine> EnginesByYargProfile = new();

        public EngineManager(SongChart chart)
        {
            //Chart = chart;
            UnisonPhrases = chart.UnisonPhrases;
            NumPlayersRequiredToHitPhrase = new uint[UnisonPhrases.Count];
            NumPlayersHaveHitPhrase = new uint[UnisonPhrases.Count];

            foreach(var unisonPhrase in UnisonPhrases)
            {
                var phrase = unisonPhrase.Phrase;
                var instrumentList = unisonPhrase.Instruments;
                YargLogger.LogFormatInfo<uint, uint, string>("[Unison] Tick: {0} to {1} for {2}", phrase.Tick, phrase.TickEnd, String.Join(", ", instrumentList));
            }
        }

        public void RegisterPlayer(YargProfile yargProfile, BaseEngine engine)
        {
            YargProfiles.Add(yargProfile);
            Engines.Add(engine);
            EnginesByYargProfile[yargProfile.Id] = engine;
            YargLogger.LogFormatInfo<string, string>("[Register] player {0} with engine {1}", yargProfile.Id.ToString(), engine.ToString());

            for (int i = 0; i < UnisonPhrases.Count; i++)
            {
                var instruments = UnisonPhrases[i].Instruments;
                if (instruments.Contains(GetInstrument(yargProfile)))
                    NumPlayersRequiredToHitPhrase[i]++;
            }
            YargLogger.LogFormatInfo<string>("{0}", String.Join(", ", NumPlayersRequiredToHitPhrase));
            // foreach (var x in NumPlayersHaveHitPhrase)
            // {
            //     YargLogger.LogFormatInfo("n {0}", x);
            // }
        }

        public void OnStarPowerPhraseHit(YargProfile yargProfile, ChartEvent note)
        {
            YargLogger.LogFormatInfo<string, Instrument, uint>("Player {0} [{1}] hit phrase with note tick {2}", yargProfile.Id.ToString(), yargProfile.CurrentInstrument, note.Tick);

            var instrument = GetInstrument(yargProfile);
            for (int unisonPhraseNum = 0; unisonPhraseNum < UnisonPhrases.Count; unisonPhraseNum++)
            {
                var unisonPhrase = UnisonPhrases[unisonPhraseNum];
                if (unisonPhrase.Phrase.Tick <= note.Tick & unisonPhrase.Phrase.TickEnd > note.Tick)
                {
                    if (unisonPhrase.Instruments.Contains(instrument))
                    {
                        NumPlayersHaveHitPhrase[unisonPhraseNum]++;
                        if (NumPlayersHaveHitPhrase[unisonPhraseNum] == NumPlayersRequiredToHitPhrase[unisonPhraseNum])
                            AwardUnisonBonus(unisonPhraseNum);
                    }
                }
            }
        }

        public void OnStarPowerPhraseMissed(YargProfile yargProfile, ChartEvent note)
        {
            YargLogger.LogFormatInfo<string, Instrument, uint>("Player {0} [{1}] missed phrase with note tick {2}", yargProfile.Id.ToString(), yargProfile.CurrentInstrument, note.Tick);
        }

        private void AwardUnisonBonus(int unisonPhraseNum)
        {
            YargLogger.LogFormatInfo("Awarding unison bonus to for hitting phrase {0}", unisonPhraseNum);
            var phrase = UnisonPhrases[unisonPhraseNum].Phrase;
            var instruments = UnisonPhrases[unisonPhraseNum].Instruments;
            foreach(var profile in YargProfiles)
            {
                if (instruments.Contains(GetInstrument(profile)))
                {
                    YargLogger.LogFormatInfo("Awarding unison bonus to player {0}", profile.Id);
                    EnginesByYargProfile[profile.Id].AwardUnisonBonusStarPower();
                }
            }
        }

        private Instrument GetInstrument(YargProfile profile)
        {
            // pro and 4lane grouped together
            return profile.CurrentInstrument == Instrument.FourLaneDrums ? Instrument.ProDrums : profile.CurrentInstrument;
        }
    }

}