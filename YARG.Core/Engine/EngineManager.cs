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
        private SongChart chart;
        private Dictionary<Phrase, List<Instrument>> UnisonPhrasesToInstruments;
        private List<Phrase> UnisonPhrases;
        private uint[] NumPlayersRequiredToHitPhrase;
        private uint[] NumPlayersHaveHitPhrase;
        private Dictionary<Guid, BaseEngine> EnginesByYargProfile = new();

        public EngineManager(SongChart chart)
        {
            this.chart = chart;
            UnisonPhrases = chart.UnisonPhrases;
            UnisonPhrasesToInstruments = chart.UnisonPhrasesToInstruments;
            NumPlayersRequiredToHitPhrase = new uint[UnisonPhrasesToInstruments.Count];
            NumPlayersHaveHitPhrase = new uint[UnisonPhrasesToInstruments.Count];

            foreach(var (phrase, instrumentList) in UnisonPhrasesToInstruments)
            {
                YargLogger.LogFormatInfo<uint, uint, string>("[Unison] Tick: {0} to {1} for {2}", phrase.Tick, phrase.TickEnd, String.Join(", ", instrumentList.ToArray()));
            }
        }

        public void RegisterPlayer(YargProfile yargProfile, BaseEngine engine)
        {
            YargProfiles.Add(yargProfile);
            Engines.Add(engine);
            EnginesByYargProfile[yargProfile.Id] = engine;
            YargLogger.LogFormatInfo<string, string>("Registered player {0} with engine {1}", yargProfile.Id.ToString(), engine.ToString());

            for (int i = 0; i < UnisonPhrases.Count; i++)
            {
                var phrase = UnisonPhrases[i];
                if (UnisonPhrasesToInstruments[phrase].Contains(GetInstrument(yargProfile)))
                    NumPlayersRequiredToHitPhrase[i]++;
            }
            YargLogger.LogFormatInfo<string>("{0}", String.Join(", ", NumPlayersRequiredToHitPhrase));
        }

        public void OnStarPowerPhraseHit(YargProfile yargProfile, ChartEvent note)
        {
            YargLogger.LogFormatInfo<string, Instrument, uint>("Player {0} [{1}] hit phrase with note tick {2}", yargProfile.Id.ToString(), yargProfile.CurrentInstrument, note.Tick);

            for (int unisonPhraseNum = 0; unisonPhraseNum < UnisonPhrases.Count; unisonPhraseNum++)
            {
                var phrase = UnisonPhrases[unisonPhraseNum];
                if (phrase.Tick <= note.Tick & phrase.TickEnd > note.Tick)
                {
                    NumPlayersHaveHitPhrase[unisonPhraseNum]++;
                    // todo: check that instrument is elligible for phrase
                    if (NumPlayersHaveHitPhrase[unisonPhraseNum] == NumPlayersRequiredToHitPhrase[unisonPhraseNum])
                        AwardUnisonBonus(unisonPhraseNum);
                }
            }
        }

        private void AwardUnisonBonus(int unisonPhraseNum)
        {
            YargLogger.LogFormatInfo("Awarding unison bonus to for hitting phrase {0}", unisonPhraseNum);
            var phrase = UnisonPhrases[unisonPhraseNum];
            var instruments = UnisonPhrasesToInstruments[phrase];
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