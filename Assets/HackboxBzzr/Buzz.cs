using System;

namespace Hackbox.Bzzr
{
    public class Buzz
    {
        public Buzz(BzzrPlayer player, TimeSpan localTime)
        {
            Player = player;
            LocalTime = localTime;
        }

        public readonly BzzrPlayer Player;
        public readonly TimeSpan LocalTime;

        public override int GetHashCode()
        {
            return Player.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Buzz otherBuzz)
            {
                return Player == otherBuzz.Player && LocalTime == otherBuzz.LocalTime;
            }

            return false;
        }
    }
}
