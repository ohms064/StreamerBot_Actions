using System.Collections.Generic;
using System.Linq;
using Streamer.bot.Plugin.Interface.Model;

namespace SBCustomClasses.StreamDeck
{
    [System.Flags]
    public enum StreamDeckSections
    {
        TeamMembers,
        Characters,
        CurrentPlayerPFP,
        CurrentPlayerCharacter,
        CurrentStocks,
        TeamLeft,
        TeamRight,
        BothTeams = TeamRight & TeamLeft,
        AllSections = TeamMembers & Characters & CurrentPlayerCharacter & CurrentPlayerPFP &
                      CurrentStocks
    }

    public struct TeamData
    {
        public List<TeamMemberData> TeamMembers;
        public string Name;
    }

    public struct TeamMemberData
    {
        public string Id;
        public IEnumerable<string> Characters;
    }

    public class TeamUserInfo
    {
        public TwitchUserInfoEx TwitchUserInfo { get; private set; }
        public List<string> Characters { get; private set; }
        public List<string> OriginalCharacters { get; private set; }
        private string Nickname { get; set; }

        public string UserName => string.IsNullOrEmpty(Nickname) ? TwitchUserInfo.UserName : Nickname;

        public TeamUserInfo(TwitchUserInfoEx userInfo, string nickname, IEnumerable<string> characters)
        {
            TwitchUserInfo = userInfo;
            Nickname = nickname;
            var characterList = characters.ToList();
            Characters = characterList;
            OriginalCharacters = characterList;
        }
    }

    public class TeamInfo : Dictionary<string, TeamUserInfo>
    {
        private const string ErrorImageUrl =
            "https://www.shutterstock.com/shutterstock/photos/1261316767/display_1500/stock-vector-x-cross-icon-wrong-and-error-symbol-1261316767.jpg";
        private static readonly TeamUserInfo _emptyUser = new TeamUserInfo(
            new TwitchUserInfoEx() { ProfileImageUrl = ErrorImageUrl, UserName = "Empty" },
            "None", new List<string>());
        
        public string LastSelectedCharacterButtonId;
        public string LastSelectedPlayerButtonId;
        public string CurrentSelectedPlayerId;
        public int Stocks;
        public List<string> OrderedPlayers = new List<string>();

        public string TeamName
        {
            get => string.IsNullOrEmpty(_teamName) ? this.First().Value.UserName : _teamName;
            set => _teamName = value;
        }

        public TeamUserInfo CurrentSelectedPlayer =>
            this.ContainsKey(CurrentSelectedPlayerId) ? this[CurrentSelectedPlayerId] : _emptyUser;
    
        private string _teamName;
        
        
    }
}