using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Streamer.bot.Plugin.Interface.Model;

namespace SBCustomClasses.StreamDeck
{
    [System.Flags]
    public enum StreamDeckSections
    {
        TeamMembers = 1 << 0,
        Characters = 1 << 1,
        CurrentPlayerPFP = 1 << 2,
        CurrentPlayerCharacter = 1 << 3,
        CurrentStocks = 1 << 4,
        TeamLeft = 1 << 5,
        TeamRight = 1 << 6,
        BothTeams = TeamRight | TeamLeft,
        AllSections = TeamMembers | Characters | CurrentPlayerCharacter | CurrentPlayerPFP | CurrentStocks,
        StartingSections = TeamMembers | Characters | CurrentPlayerPFP | CurrentStocks
    }

    public struct TeamData
    {
        public List<TeamMemberData> TeamMembers;
        public string Name;

        public TeamData(List<TeamMemberData> teamMembers, string name)
        {
            TeamMembers = teamMembers;
            Name = name;
        }
        
        public TeamData(TeamMemberData teamMember, string name)
        {
            TeamMembers = new List<TeamMemberData>(){teamMember};
            Name = name;
        }
    }

    public struct TeamMemberData
    {
        public string Id;
        public IEnumerable<string> Characters;

        public TeamMemberData(string id, IEnumerable<string> characters)
        {
            Id = id;
            Characters = characters;
        }
    }

    internal  class TeamUserInfo
    {
        public TwitchUserInfoEx TwitchUserInfo { get; private set; }
        public List<string> Characters { get; internal set; }
        public List<string> OriginalCharacters { get; private set; }
        private string Nickname { get; set; }

        public string UserName => string.IsNullOrEmpty(Nickname) ? TwitchUserInfo.UserName : Nickname;

        public TeamUserInfo(TwitchUserInfoEx userInfo, string nickname, IEnumerable<string> characters)
        {
            TwitchUserInfo = userInfo;
            Nickname = nickname;
            Characters = characters.ToList();
            OriginalCharacters = characters.ToList();
        }
    }

    internal  class TeamInfo
    {
        private const string ErrorImageUrl =
            "https://www.shutterstock.com/shutterstock/photos/1261316767/display_1500/stock-vector-x-cross-icon-wrong-and-error-symbol-1261316767.jpg";
        private static readonly TeamUserInfo _emptyUser = new TeamUserInfo(
            new TwitchUserInfoEx() { ProfileImageUrl = ErrorImageUrl, UserName = "Empty" },
            "None", new List<string>());

        [JsonProperty("team_members")]
        public Dictionary<string, TeamUserInfo> TeamMembers = new Dictionary<string, TeamUserInfo>();
        [JsonProperty("last_selected_character_button_id")]
        public string LastSelectedCharacterButtonId;
        [JsonProperty("last_selected_player_button_id")]
        public string LastSelectedPlayerButtonId;
        [JsonProperty("current_selected_player_button_id")]
        public string CurrentSelectedPlayerId;
        [JsonProperty("stocks")]
        public int Stocks;
        [JsonProperty("ordered_players")]
        public List<string> OrderedPlayers = new List<string>();

        [JsonProperty("team_name")]
        public string TeamName
        {
            get => string.IsNullOrEmpty(_teamName) ? TeamMembers.First().Value.UserName : _teamName;
            set => _teamName = value;
        }

        public TeamUserInfo CurrentSelectedPlayer =>
            TeamMembers.ContainsKey(CurrentSelectedPlayerId) ? TeamMembers[CurrentSelectedPlayerId] : _emptyUser;
        
        private string _teamName;
        
        
    }
}