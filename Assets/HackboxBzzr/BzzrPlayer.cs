namespace Hackbox.Bzzr
{
    public class BzzrPlayer
    {
        public BzzrPlayer(Member member)
        {
            Member = member;
        }

        internal BzzrPlayer()
        {
        }

        public Member Member
        {
            get;
            set;
        }

        public string Name => Member.Name;
        public string UserID => Member.UserID;

        public bool Connected
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        }

        public bool Locked
        {
            get;
            set;
        }

        public override int GetHashCode()
        {
            return UserID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is BzzrPlayer otherPlayer)
            {
                return UserID == otherPlayer.UserID;
            }

            return false;
        }
    }
}
