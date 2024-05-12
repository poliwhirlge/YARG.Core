using System;
using System.Collections.Generic;

namespace YARG.Core.Chart
{
    /// <summary>
    /// Possible phrase types.
    /// </summary>
    public enum PhraseType
    {
        // Note modifiers
        StarPower, // Mainly for visuals, notes are already marked directly as SP
        TremoloLane, // Guitar strum lanes, single drum rolls
        TrillLane, // Guitar trill lanes, double drum rolls
        DrumFill, // Also for visuals

        // Versus modes (face-off and the like)
        VersusPlayer1,
        VersusPlayer2,

        // Other events
        Solo, // Also for visuals
        BigRockEnding,
    }

    /// <summary>
    /// A phrase event that occurs in a chart.
    /// </summary>
    public class Phrase : ChartEvent, ICloneable<Phrase>
    {
        public PhraseType Type { get; }

        public Phrase(PhraseType type, double time, double timeLength, uint tick, uint tickLength)
            : base(time, timeLength, tick, tickLength)
        {
            Type = type;
        }

        public Phrase(Phrase other) : base(other)
        {
            Type = other.Type;
        }

        public Phrase Clone()
        {
            return new(this);
        }

        public override bool Equals(Object obj)
        {
            return Equals(obj as Phrase);
        }

        public bool Equals(Phrase? phrase)
        {
            if (phrase == null)
                return false;

            return (Tick == phrase.Tick && TickEnd == phrase.TickEnd);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Tick, TickEnd, Type);
        }
    }

    public class UnisonPhrase
    {
        public Phrase Phrase{ get; }
        public HashSet<Instrument> Instruments{ get; }

        public UnisonPhrase(Phrase phrase, HashSet<Instrument> instruments)
        {
            Phrase = phrase;
            Instruments = instruments;
        }
    }
}