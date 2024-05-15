using System;
using System.Collections.Generic;
using System.Linq;
using YARG.Core.Chart;
using YARG.Core.Game;
using YARG.Core.Logging;

namespace YARG.Core.Engine
{
    public delegate void OnUnisonPhraseStart(UnisonPhrase unisonPhrase, List<YargProfile> playersInUnison);
    public delegate void OnUnisonPhraseFail(UnisonPhrase unisonPhrase, YargProfile yargProfile);
    public delegate void OnUnisonPhraseComplete(UnisonPhrase unisonPhrase, YargProfile yargProfile);
    public delegate void OnUnisonPhraseAward(UnisonPhrase unisonPhrase);
    public delegate void OnUnisonPhraseEnd(UnisonPhrase unisonPhrase);
    
    public class EngineManager
    {
        private List<YargProfile> YargProfiles = new();
        private List<BaseEngine> Engines = new();
        //private SongChart Chart;
        private List<UnisonPhrase> UnisonPhrases;
        private uint[] NumPlayersRequiredToHitPhrase;
        private uint[] NumPlayersHaveHitPhrase;
        private uint[] NumPlayersHaveMissedPhrase;
        private Dictionary<Guid, BaseEngine> EnginesByYargProfile = new();

        public OnUnisonPhraseStart? OnUnisonPhraseStart;
        public OnUnisonPhraseFail? OnUnisonPhraseFail;
        public OnUnisonPhraseComplete? OnUnisonPhraseComplete;
        public OnUnisonPhraseAward? OnUnisonPhraseAward;
        public OnUnisonPhraseEnd? OnUnisonPhraseEnd;

        private int CurrentUnisonPhraseIdx = 0;

        private enum UnisonStatus
        {
            NotStarted, Started, Failed, Complete
        }

        private UnisonStatus[] UnisonPhraseStatuses;

        public EngineManager(SongChart chart)
        {
            UnisonPhrases = chart.UnisonPhrases;
            NumPlayersRequiredToHitPhrase = new uint[UnisonPhrases.Count];
            NumPlayersHaveHitPhrase = new uint[UnisonPhrases.Count];
            NumPlayersHaveMissedPhrase = new uint[UnisonPhrases.Count];
            UnisonPhraseStatuses = new UnisonStatus[UnisonPhrases.Count];
            
            Array.Fill(UnisonPhraseStatuses, UnisonStatus.NotStarted);

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
        }

        private int FindUnisonPhrase(uint tick, Instrument instrument)
        {
            for (int unisonPhraseNum = CurrentUnisonPhraseIdx; unisonPhraseNum < UnisonPhrases.Count; unisonPhraseNum++)
            {
                var unisonPhrase = UnisonPhrases[unisonPhraseNum];
                if (unisonPhrase.Phrase.Tick <= tick & unisonPhrase.Phrase.TickEnd > tick)
                {
                    if (unisonPhrase.Instruments.Contains(instrument))
                        return unisonPhraseNum;
                }
            }
            return -1;
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

        private Instrument GetInstrument(YargProfile yargProfile)
        {
            // pro and 4lane grouped together
            return yargProfile.CurrentInstrument == Instrument.FourLaneDrums ? Instrument.ProDrums : yargProfile.CurrentInstrument;
        }

        private void InvokeUnisonStart(int unisonIndex)
        {
            var instruments = UnisonPhrases[unisonIndex].Instruments;
            var profilesInUnison = YargProfiles.Where(p => instruments.Contains(GetInstrument(p))).ToList();
            YargLogger.LogFormatInfo<string>("Unison start: {0}", String.Join(",", profilesInUnison));
            OnUnisonPhraseStart?.Invoke(UnisonPhrases[unisonIndex], profilesInUnison);
        }

        private void InvokeUnisonFail(int unisonIndex, YargProfile yargProfile)
        {
            var instruments = UnisonPhrases[unisonIndex].Instruments;
            var profilesInUnison = YargProfiles.Where(p => instruments.Contains(GetInstrument(p))).ToList();
            OnUnisonPhraseFail?.Invoke(UnisonPhrases[unisonIndex], yargProfile);
        }

        internal void OnStarPowerPhraseStart<TNoteType>(YargProfile yargProfile, TNoteType note) where TNoteType : Note<TNoteType>
        {
            // if valid unison, "start" unison phrase
            var unisonPhraseNum = FindUnisonPhrase(note.Tick, GetInstrument(yargProfile));
            if (unisonPhraseNum == -1)
                return;
            
            if (UnisonPhraseStatuses[unisonPhraseNum] == UnisonStatus.NotStarted)
            {
                YargLogger.LogFormatInfo<string, Instrument, uint, int>("Player {0} [{1}] starting phrase with note tick {2} [Unison phrase {3}]", yargProfile.Id.ToString(), yargProfile.CurrentInstrument, note.Tick, unisonPhraseNum);
                InvokeUnisonStart(unisonPhraseNum);
                UnisonPhraseStatuses[unisonPhraseNum] = UnisonStatus.Started;
            }
                
            YargLogger.LogFormatInfo("Status of phrase: {0}", UnisonPhraseStatuses[unisonPhraseNum]);
        }

        public void OnStarPowerPhraseMissed(YargProfile yargProfile, ChartEvent note)
        {
            // if part of valid unison, "start" unison phrase, then fail it for this player
            var unisonPhraseNum = FindUnisonPhrase(note.Tick, GetInstrument(yargProfile));
            if (unisonPhraseNum == -1)
                return;
            
            YargLogger.LogFormatInfo<string, Instrument, uint, int>("Player {0} [{1}] missed phrase with note tick {2} [Unison phrase {3}]", yargProfile.Id.ToString(), yargProfile.CurrentInstrument, note.Tick, unisonPhraseNum);

            if (UnisonPhraseStatuses[unisonPhraseNum] == UnisonStatus.NotStarted)
                InvokeUnisonStart(unisonPhraseNum);
            InvokeUnisonFail(unisonPhraseNum, yargProfile);
            YargLogger.LogFormatInfo("Status of phrase: {0}", UnisonPhraseStatuses[unisonPhraseNum]);
            UnisonPhraseStatuses[unisonPhraseNum] = UnisonStatus.Failed;

            NumPlayersHaveMissedPhrase[unisonPhraseNum]++;
            CheckForPhraseCompletion(unisonPhraseNum);
        }

        public void OnStarPowerPhraseHit(YargProfile yargProfile, ChartEvent note)
        {
            var unisonPhraseNum = FindUnisonPhrase(note.Tick, GetInstrument(yargProfile));
            if (unisonPhraseNum == -1)
                return;
            
            YargLogger.LogFormatInfo<string, Instrument, uint, int>("Player {0} [{1}] hit phrase with note tick {2} [Unison phrase {3}]", yargProfile.Id.ToString(), yargProfile.CurrentInstrument, note.Tick, unisonPhraseNum);
            if (UnisonPhraseStatuses[unisonPhraseNum] == UnisonStatus.NotStarted)
            {
                InvokeUnisonStart(unisonPhraseNum);
                UnisonPhraseStatuses[unisonPhraseNum] = UnisonStatus.Started;
            }
            OnUnisonPhraseComplete?.Invoke(UnisonPhrases[unisonPhraseNum], yargProfile);

            NumPlayersHaveHitPhrase[unisonPhraseNum]++;
            CheckForPhraseCompletion(unisonPhraseNum);
        }

        private void CheckForPhraseCompletion(int unisonPhraseNum)
        {
            if (NumPlayersHaveHitPhrase[unisonPhraseNum] == NumPlayersRequiredToHitPhrase[unisonPhraseNum])
            {
                AwardUnisonBonus(unisonPhraseNum);
                UnisonPhraseStatuses[unisonPhraseNum] = UnisonStatus.Complete;
                OnUnisonPhraseAward?.Invoke(UnisonPhrases[unisonPhraseNum]);
                OnUnisonPhraseEnd?.Invoke(UnisonPhrases[unisonPhraseNum]);
            }
            else if (NumPlayersHaveHitPhrase[unisonPhraseNum] + NumPlayersHaveMissedPhrase[unisonPhraseNum] == NumPlayersRequiredToHitPhrase[unisonPhraseNum])
            {
                OnUnisonPhraseEnd?.Invoke(UnisonPhrases[unisonPhraseNum]);
            }
        }
    }

}