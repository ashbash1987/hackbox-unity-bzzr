using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

namespace Hackbox.Bzzr
{
    [RequireComponent(typeof(Host))]
    public class BzzrHost : MonoBehaviour
    {
        #region Events
        [Header("Events")]
        public RoomCodeEvent OnRoomCreated = new RoomCodeEvent();
        public BzzrPlayerEvent OnPlayerJoined = new BzzrPlayerEvent();
        public BzzrPlayerEvent OnPlayerKicked = new BzzrPlayerEvent();
        public BuzzEvent OnBuzz = new BuzzEvent();
        public BuzzEvent OnClearBuzz = new BuzzEvent();
        #endregion

        #region States
        public StateAsset WaitingState = null;
        public StateAsset LockedState = null;
        public StateAsset ArmedState = null;
        public StateAsset BuzzedState = null;
        public StateAsset KickedState = null;
        #endregion

        #region Public Properties        
        public string RoomCode
        {
            get;
            private set;
        }

        public string UserID
        {
            get;
            private set;
        }

        public bool BuzzArmed
        {
            get;
            private set;
        }

        public BzzrPlayer[] AllPlayers => Players.Values.ToArray();
        public bool HasPlayers => Players.Any();
        public bool HasBuzzes => Buzzes.Any();
        public Buzz CurrentBuzz => Buzzes.FirstOrDefault();
        public Buzz[] AllBuzzes => Buzzes.ToArray();
        #endregion

        #region Private Properties
        private IEnumerable<Member> LockedMembers => Players.Values.Where(x => x.Locked).Select(x => x.Member);
        private IEnumerable<Member> UnlockedMembers => Players.Values.Where(x => !x.Locked).Select(x => x.Member);
        #endregion

        #region Private Fields
        private readonly ConcurrentDictionary<string, BzzrPlayer> Players = new ConcurrentDictionary<string, BzzrPlayer>();
        private readonly List<Buzz> Buzzes = new List<Buzz>();
        private Host _hackboxHost = null;
        private DateTimeOffset _armTime;
        #endregion

        #region Unity Events
        private void Awake()
        {
            _hackboxHost = GetComponent<Host>();
        }

        private void OnEnable()
        {
            _hackboxHost.OnRoomConnected.AddListener(OnRoomConnected);
            _hackboxHost.OnRoomDisconnected.AddListener(OnRoomDisconnected);
            _hackboxHost.OnMemberJoined.AddListener(OnMemberJoined);
            _hackboxHost.OnMemberKicked.AddListener(OnMemberKicked);
            _hackboxHost.OnMessage.AddListener(OnMessage);
        }

        private void OnDisable()
        {
            _hackboxHost.OnRoomConnected.RemoveListener(OnRoomConnected);
            _hackboxHost.OnRoomDisconnected.RemoveListener(OnRoomDisconnected);
            _hackboxHost.OnMemberJoined.RemoveListener(OnMemberJoined);
            _hackboxHost.OnMemberKicked.RemoveListener(OnMemberKicked);
            _hackboxHost.OnMessage.RemoveListener(OnMessage);
        }

        private void Start()
        {
            CreateNewRoom();
        }

        private void OnDestroy()
        {
            CloseRoom();
        }
        #endregion

        #region Public Methods
        public void CreateNewRoom()
        {
            _hackboxHost.Disconnect();
            _hackboxHost.Connect(true);
        }

        public void CloseRoom()
        {
            _hackboxHost.Disconnect();
        }

        public BzzrPlayer[] GetAllPlayers()
        {
            return Players.Values.ToArray();
        }

        public BzzrPlayer GetPlayer(string name)
        {
            return Players.Values.FirstOrDefault(x => x.Name.Equals(name, System.StringComparison.InvariantCultureIgnoreCase));
        }

        public void ArmBuzzers()
        {
            BuzzArmed = true;
            _armTime = DateTimeOffset.Now;
            _hackboxHost.UpdateMemberStates(LockedMembers, LockedState.State);
            _hackboxHost.UpdateMemberStates(UnlockedMembers, ArmedState.State);
            foreach (Buzz buzz in Buzzes)
            {
                OnClearBuzz.Invoke(buzz);
            }
            Buzzes.Clear();
        }

        public void DisarmBuzzers()
        {
            BuzzArmed = false;
            _hackboxHost.UpdateMemberStates(LockedMembers, LockedState.State);
            _hackboxHost.UpdateMemberStates(UnlockedMembers, WaitingState.State);
            foreach (Buzz buzz in Buzzes)
            {
                OnClearBuzz.Invoke(buzz);
            }
            Buzzes.Clear();
        }

        public void LockPlayer(string playerName)
        {
            LockPlayer(GetPlayer(playerName));
        }

        public void LockPlayer(BzzrPlayer player)
        {
            player.Locked = true;
            _hackboxHost.UpdateMemberState(player.Member, LockedState.State);
        }

        public void UnlockPlayer(string playerName)
        {
            UnlockPlayer(GetPlayer(playerName));
        }

        public void UnlockPlayer(BzzrPlayer player)
        {
            player.Locked = false;
            _hackboxHost.UpdateMemberState(player.Member, BuzzArmed ? ArmedState.State : WaitingState.State);
        }

        public void KickPlayer(string playerName)
        {
            KickPlayer(GetPlayer(playerName));
        }

        public void KickPlayer(BzzrPlayer player)
        {
            OnMemberKicked(player.Member);
            _hackboxHost.UpdateMemberState(player.Member, KickedState.State);
        }

        public void ClearBuzz(Buzz buzz)
        {
            Buzzes.Remove(buzz);
            OnClearBuzz.Invoke(buzz);
            _hackboxHost.UpdateMemberState(buzz.Player.Member, BuzzArmed ? (buzz.Player.Locked ? LockedState.State : ArmedState.State) : WaitingState.State);
        }

        public Buzz GetCurrentBuzz()
        {
            return CurrentBuzz;
        }

        public Buzz[] GetAllBuzzes()
        {
            return AllBuzzes;
        }
        #endregion

        #region Private Methods
        private void OnRoomConnected(string roomCode)
        {
            RoomCode = roomCode;
            OnRoomCreated.Invoke(RoomCode);
        }

        private void OnRoomDisconnected(string roomCode)
        {
            RoomCode = null;
        }

        private void OnMemberJoined(Member member)
        {
            BzzrPlayer newPlayer = new BzzrPlayer(member);
            Players[member.UserID] = newPlayer;
            _hackboxHost.UpdateMemberState(member, WaitingState.State);
            OnPlayerJoined.Invoke(newPlayer);
        }

        private void OnMemberKicked(Member member)
        {
            if (Players.TryRemove(member.UserID, out BzzrPlayer oldPlayer))
            {
                OnPlayerKicked.Invoke(oldPlayer);
            }
        }

        private void OnMessage(Message message)
        {
            switch (message.Event)
            {
                case "buzz":
                    TimeSpan buzzTime = message.Timestamp - _armTime;
                    Buzz newBuzz = new Buzz(Players[message.Member.UserID], buzzTime);
                    Buzzes.Add(newBuzz);
                    OnBuzz.Invoke(newBuzz);
                    State buzzState = new State(BuzzedState.State);
                    buzzState.SetComponentText("Text", $"{buzzTime.TotalSeconds:0.0000}s");
                    _hackboxHost.UpdateMemberState(message.Member, buzzState);                    
                    break;

                default:
                    break;
            }
        }
        #endregion
    }
}