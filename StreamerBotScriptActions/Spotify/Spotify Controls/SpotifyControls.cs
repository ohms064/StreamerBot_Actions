//code bastardized by Mustached_Maniac
//https://ko-fi.com/mustached_maniac/tip

using Streamer.bot.Plugin.Interface;

namespace StreamerBotScriptActions.Spotify.Spotify_Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CPHInline : CPHInlineBase
{
    private HttpClient _httpClient;
    string _accessToken;
    string _user;
    string _tokenExpires;
    string _deviceId;
    string _clientId;
    string _clientSecret;
    string _refreshToken;
    int _numSongs;
    string _trackUri;
    string _userInput;
    string _trackName;
    string _artistName;
    string _selectedPlaylistUri;
    string _playlistId;
    long _unixTimestamp;
    bool _blockSongBool;
    public static Dictionary<string, string> _songRequests = new Dictionary<string, string>();
    public static Dictionary<string, string> _blockedSongs = new Dictionary<string, string>();
    public void Init()
    {
        _selectedPlaylistUri = "";
        _httpClient = new HttpClient();
        _accessToken = CPH.GetGlobalVar<string>("spotifyAccessToken", true);
        _tokenExpires = CPH.GetGlobalVar<string>("spotifyTokenExpires", true);
        _deviceId = CPH.GetGlobalVar<string>("spotifyDeviceId", true);
        _clientSecret = CPH.GetGlobalVar<string>("spotifyClientSecret", true);
        _clientId = CPH.GetGlobalVar<string>("spotifyClientId", true);
        _refreshToken = CPH.GetGlobalVar<string>("spotifyRefreshToken", true);
        _playlistId = CPH.GetGlobalVar<string>("spotifyPlaylistId", true);
        _numSongs = 1;
        _userInput = string.Empty;
        _unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _blockSongBool = false;
        CPH.LogInfo($"Spotify Control Initialized. Device ID: {_deviceId}, Access Token: {_accessToken}");
    }

    public bool SpotifyPlaySong()
    {
        bool spotifyPlaying = CPH.GetGlobalVar<bool>("spotifyPlaying", false);
        if (spotifyPlaying)
        {
            CPH.SendMessage("Spotify is already playing.", true);
            return false;
        }

        return SendCommand("play");
    }

    public bool SpotifyPauseSong()
    {
        bool spotifyPlaying = CPH.GetGlobalVar<bool>("spotifyPlaying", false);
        if (!spotifyPlaying)
        {
            CPH.SendMessage("Spotify is already paused.", true);
            return false;
        }

        return SendCommand("pause");
    }

    public bool SpotifySkipSong()
    {
        return SendCommand("skip");
    }

    public bool SpotifyPreviousSong()
    {
        return SendCommand("previous");
    }

    public bool SpotifySetVolume()
    {
        if (args.ContainsKey("volPercent"))
        {
            if (int.TryParse(args["volPercent"].ToString(), out int volumePercent))
            {
                if (volumePercent >= 0 && volumePercent <= 100)
                {
                    return SendCommand("volume", volumePercent);
                }
                else
                {
                    CPH.SendMessage($"Volume value: '{volumePercent}' is invalid. Please provide a value between 0-100.", true);
                    return false;
                }
            }
        }

        if (args.ContainsKey("input0") && !string.IsNullOrWhiteSpace(args["input0"].ToString()))
        {
            string input = args["input0"].ToString().Trim();
            if (input.Length > 1 && (input[0] == '+' || input[0] == '-') && int.TryParse(input.Substring(1), out int adjustment))
            {
                int spotifyVolume = CPH.GetGlobalVar<int>("spotifyVolume", false);
                int volumePercent = input[0] == '+' ? spotifyVolume + adjustment : spotifyVolume - adjustment;
                if (volumePercent > 100)
                {
                    CPH.SendMessage("I'm sorry, Spotify has a maximum volume of 100.", true);
                    return false;
                }
                else if (volumePercent < 0)
                {
                    CPH.SendMessage("I'm sorry, this would bring Spotify's volume below the minimum value of 0.", true);
                    return false;
                }

                return SendCommand("volume", volumePercent);
            }
            else if (int.TryParse(input, out int volumePercent))
            {
                if (volumePercent >= 0 && volumePercent <= 100)
                {
                    return SendCommand("volume", volumePercent);
                }
                else
                {
                    CPH.SendMessage($"Volume value: '{volumePercent}' is invalid. Please provide a value between 0-100.", true);
                    return false;
                }
            }
            else
            {
                CPH.SendMessage("Invalid input. Please provide a valid number between 0-100 for volume adjustment.", true);
                return false;
            }
        }

        CPH.SendMessage("You can only adjust the volume with numerical values between 0-100.", true);
        return false;
    }

    public bool SpotifyCurrentlyPlaying()
    {
        bool spotifyPlaying = CPH.GetGlobalVar<bool>("spotifyPlaying", false);
        if (!spotifyPlaying)
        {
            CPH.SendMessage("Press 'Play' on Spotify to see the current song.", true);
            return false;
        }

        return SendCommand("currentsong");
    }

    public bool SpotifyQueue()
    {
        int parsedNumSongs = 1;
        if (args.ContainsKey("input0") && !string.IsNullOrWhiteSpace(args["input0"].ToString()))
        {
            if (!int.TryParse(args["input0"].ToString(), out parsedNumSongs))
            {
                CPH.SendMessage("You must enter the number of songs to retrieve, or leave it blank to see the next song.", true);
                return false;
            }
            else if (parsedNumSongs <= 0)
            {
                CPH.SendMessage("Invalid input. Please provide a positive number only.", true);
                return false;
            }
        }

        _numSongs = parsedNumSongs;
        return SendCommand("queue");
    }

    public bool SpotifyLastSong()
    {
        int parsedNumSongs = 1;
        if (args.ContainsKey("input0") && !string.IsNullOrWhiteSpace(args["input0"].ToString()))
        {
            if (!int.TryParse(args["input0"].ToString(), out parsedNumSongs))
            {
                CPH.SendMessage("You must enter the number of songs to retrieve, or leave it blank to see the next song.", true);
                return false;
            }
            else if (parsedNumSongs <= 0)
            {
                CPH.SendMessage("Invalid input. Please provide a positive number only.", true);
                return false;
            }
            else if (parsedNumSongs > 10)
            {
                CPH.SendMessage("Sorry, only the last 10 songs played on Spotify are stored.", true);
                return false;
            }
        }

        _numSongs = parsedNumSongs;
        var recentSongs = CPH.GetGlobalVar<List<Dictionary<string, string>>>("recentSongs", false);
        if (recentSongs != null && recentSongs.Any())
        {
            int startIndex = Math.Max(0, recentSongs.Count - _numSongs - 1);
            var songs = recentSongs.Skip(startIndex).Reverse().ToList();
            var message = ConstructLastSongMessage(songs, _numSongs);
            CPH.SendMessage(message, true);
        }
        else
        {
            CPH.SendMessage("Sorry, there are no recent songs available.", true);
        }

        return true;
    }

    public bool SpotifyAddSong()
    {
        _user = args["user"].ToString();
        _userInput = args["rawInput"].ToString();
        if (string.IsNullOrEmpty(_userInput))
        {
            CPH.SendMessage("You must enter search criteria to request a song!", true);
            return false;
        }
        else
        {
            string pattern = @"open\.spotify\.com\/(?:[^\/]+\/)?track\/";
            if (Regex.IsMatch(_userInput, pattern))
            {
                int startIndex = _userInput.IndexOf("track/") + "track/".Length;
                int endIndex = _userInput.IndexOf("?", startIndex);
                if (endIndex == -1)
                {
                    endIndex = _userInput.Length;
                }

                string uri = _userInput.Substring(startIndex, endIndex - startIndex);
                _trackUri = uri;
                return GetTrack(_trackUri);
            }
            else if (_userInput.Contains("open.spotify.com/playlist/") || _userInput.Contains("open.spotify.com/album/") || _userInput.Contains("open.spotify.com/artist/"))
            {
                CPH.SendMessage("Solo puedes agregar links de canciones, no se aceptan playlists, artistas o álbumes", true);
                return false;
            }
            else
            {
                return SearchSong(_userInput);
            }
        }
    }

    public bool SpotifyMyRequests()
    {
        _user = args["user"].ToString();
        _numSongs = 20;
        return SendCommand("mysongs");
    }

    private bool GetTrack(string _trackUri)
    {
        var blockedSongs = CPH.GetGlobalVar<Dictionary<string, string>>("spotifyBlockedSongs", true);
        CPH.LogInfo($"track uri::: {_trackUri}");
        string fullTrackUri = "spotify:track:" + _trackUri.Trim();
        if (blockedSongs != null && blockedSongs.ContainsValue(fullTrackUri))
        {
            CPH.SendMessage("Unable to request this song, it is on the 'blocked' list.");
            return false;
        }

        return SendCommand("gettrack");
    }

    private bool SearchSong(string _userInput)
    {
        return SendCommand("searchsong");
    }

    public bool SpotifyTransferPlayback()
    {
        return SendCommand("transfer");
    }

    public bool SpotifyPlaylists()
    {
        if (args.ContainsKey("input0") && !string.IsNullOrWhiteSpace(args["input0"].ToString()))
        {
            var storedPlaylists = CPH.GetGlobalVar<List<string>>("spotifyPlaylists", true);
            if (storedPlaylists != null)
            {
                int selectedIndex;
                if (int.TryParse(args["input0"].ToString(), out selectedIndex))
                {
                    if (selectedIndex > 0 && selectedIndex <= storedPlaylists.Count)
                    {
                        string selectedPlaylistInfo = storedPlaylists[selectedIndex - 1];
                        string[] parts = selectedPlaylistInfo.Split(new[] { ", Uri: " }, StringSplitOptions.None);
                        if (parts.Length == 2)
                        {
                            _selectedPlaylistUri = parts[1].Split(',')[0];
                            CPH.LogInfo($"Selected Playlist URI: {_selectedPlaylistUri}");
                            string[] uriParts = _selectedPlaylistUri.Split(':');
                            string playlistId = uriParts[2];
                            CPH.SetGlobalVar("spotifyPlaylistId", playlistId, true);
                            SendCommand("setplaylist");
                            return true;
                        }
                        else
                        {
                            CPH.LogInfo("Failed to parse selected playlist information.");
                        }
                    }
                    else
                    {
                        CPH.SendMessage("You must type a number that matches one of your Spotify playlists.", true);
                    }
                }
                else
                {
                    CPH.SendMessage("You must type a number that matches one of your Spotify playlists.", true);
                }
            }
        }
        else
        {
            return SendCommand("playlists");
        }

        return false;
    }

    public bool SpotifyAddToPlaylist()
    {
        var storedPlaylists = CPH.GetGlobalVar<List<string>>("spotifyPlaylists", true);
        if (storedPlaylists == null || storedPlaylists.Count == 0)
        {
            CPH.SendMessage("You must select a Spotify playlist using the '!playlist' command before adding a song to it.");
            return false;
        }

        _playlistId = CPH.GetGlobalVar<string>("spotifyPlaylistId", true);
        _trackUri = CPH.GetGlobalVar<string>("currentSpotifyUri", true);
        bool isOwnedPlaylist = false;
        foreach (var playlistInfo in storedPlaylists)
        {
            if (playlistInfo.Contains(_playlistId) && playlistInfo.Contains("Owned: True"))
            {
                isOwnedPlaylist = true;
                break;
            }
        }

        if (!isOwnedPlaylist)
        {
            CPH.SendMessage("You can only add songs to Spotify playlists that you have created.");
            return false;
        }

        return SendCommand("addtoplaylist");
    }
    
    public bool SpotifyAddToPlaylistUserRequests()
    {
        //Assuming we own the playlist
        Dictionary<string, string> storedRequests = JsonConvert.DeserializeObject<Dictionary<string, string>>(CPH.GetGlobalVar<string>("songRequests", false) ?? "{}");
        _playlistId = CPH.GetGlobalVar<string>("spotifyPlaylistId_UserRequests", true);
        _trackUri = storedRequests.Last().Key;
        
        return SendCommand("addtoplaylist");
    }

    public bool SpotifyBlockSong()
    {
        _blockSongBool = true;
        _trackUri = CPH.GetGlobalVar<string>("currentSpotifyUri", true);
        _trackName = CPH.GetGlobalVar<string>("currentSongName", true);
        _userInput = args["rawInput"].ToString();
        _blockedSongs = CPH.GetGlobalVar<Dictionary<string, string>>("spotifyBlockedSongs", true);
        if (string.IsNullOrEmpty(_userInput))
        {
            if (_blockedSongs == null)
            {
                _blockedSongs = new Dictionary<string, string>();
            }

            if (_blockedSongs.ContainsKey(_trackUri))
            {
                CPH.SendMessage("This song is already on the blocked list.");
                _blockSongBool = false;
                return true;
            }

            _blockedSongs.Add(_trackName, _trackUri);
            CPH.SetGlobalVar("spotifyBlockedSongs", _blockedSongs);
            CPH.SendMessage($"'{_trackName}' is now blocked and can no longer be added via song requests.");
            _blockSongBool = false;
            return SendCommand("skip");
            return true;
        }
        else
        {
            string pattern = @"open\.spotify\.com\/(?:[^\/]+\/)?track\/";
            if (Regex.IsMatch(_userInput, pattern))
            {
                int startIndex = _userInput.IndexOf("track/") + "track/".Length;
                int endIndex = _userInput.IndexOf("?", startIndex);
                if (endIndex == -1)
                {
                    endIndex = _userInput.Length;
                }

                string uri = _userInput.Substring(startIndex, endIndex - startIndex);
                _trackUri = uri;
                if (!SendCommand("gettrack"))
                {
                    return false;
                }
            }
            else if (_userInput.Contains("open.spotify.com/playlist/") || _userInput.Contains("open.spotify.com/album/") || _userInput.Contains("open.spotify.com/artist/"))
            {
                CPH.SendMessage("You can only block 'songs' Spotify track links. No links to artists, albums, or playlists!", true);
                return false;
            }
            else
            {
                if (!SendCommand("searchsong"))
                {
                    return false;
                }
            }
        }

        bool result = BlockCurrentSong();
        _blockSongBool = false;
        return result;
    }

    private bool BlockCurrentSong()
    {
        if (_blockedSongs == null)
        {
            _blockedSongs = new Dictionary<string, string>();
        }

        if (_blockedSongs.ContainsKey(_trackName))
        {
            CPH.SendMessage("This song is already on the blocked list.");
            return true;
        }

        _blockedSongs.Add(_trackName, _trackUri);
        CPH.SetGlobalVar("spotifyBlockedSongs", _blockedSongs);
        CPH.SendMessage($"'{_trackName}' is now blocked and can no longer be added via song requests.");
        return true;
    }

    public bool SpotifyAllowSong()
    {
        _userInput = args["rawInput"].ToString();
        var spotifyBlockedSongs = CPH.GetGlobalVar<Dictionary<string, string>>("spotifyBlockedSongs", false);
        if (spotifyBlockedSongs == null || spotifyBlockedSongs.Count == 0)
        {
            CPH.SendMessage("There are currently no blocked songs.");
            return true;
        }

        if (string.IsNullOrEmpty(_userInput))
        {
            string message = "List of blocked songs: ";
            int index = 1;
            foreach (var song in spotifyBlockedSongs)
            {
                message += $"{index}. {song.Key}";
                if (index < spotifyBlockedSongs.Count)
                    message += " // ";
                index++;
            }

            int maxLength = 500;
            while (!string.IsNullOrEmpty(message))
            {
                if (message.Length > maxLength)
                {
                    int splitIndex = message.LastIndexOf(" // ", maxLength);
                    if (splitIndex == -1)
                        splitIndex = maxLength;
                    CPH.SendMessage(message.Substring(0, splitIndex));
                    message = message.Substring(splitIndex).TrimStart(' ', '/');
                }
                else
                {
                    CPH.SendMessage(message);
                    break;
                }
            }

            return true;
        }

        if (int.TryParse(_userInput, out int number))
        {
            if (number > 0 && number <= spotifyBlockedSongs.Count)
            {
                var blockedList = spotifyBlockedSongs.ToList();
                string removedSongName = blockedList[number - 1].Key;
                spotifyBlockedSongs.Remove(removedSongName);
                CPH.SetGlobalVar("spotifyBlockedSongs", spotifyBlockedSongs);
                CPH.SendMessage($"'{removedSongName}' has been removed from the blocked list.");
                return true;
            }
        }

        foreach (var song in spotifyBlockedSongs)
        {
            if (song.Key.Equals(_userInput, StringComparison.OrdinalIgnoreCase))
            {
                spotifyBlockedSongs.Remove(song.Key);
                CPH.SetGlobalVar("spotifyBlockedSongs", spotifyBlockedSongs);
                CPH.SendMessage($"'{song.Key}' has been removed from the blocked list.");
                return true;
            }
        }

        CPH.SendMessage("No matching song found in the blocked list.");
        return false;
    }

    private bool SendCommand(string command, int? volumePercent = null)
    {
        try
        {
            _accessToken = CPH.GetGlobalVar<string>("spotifyAccessToken", true);
            // while (CPH.GetGlobalVar<bool>("lockStatus", false))
            // {
            //     CPH.Wait(100);
            // }

            string apiUrl = "https://api.spotify.com/v1/me/player";
            string commandEndpoint = "";
            HttpMethod httpMethod = HttpMethod.Put;
            string requestBody = null;
            string fullUrl = apiUrl;
            bool tokenRefreshed = false;
            if (IsTokenExpired())
            {
                tokenRefreshed = true;
            }

            switch (command)
            {
                case "play":
                    commandEndpoint = "/play";
                    requestBody = "{\"position_ms\": 0}";
                    break;
                case "pause":
                    commandEndpoint = "/pause";
                    break;
                case "skip":
                    commandEndpoint = "/next";
                    httpMethod = HttpMethod.Post;
                    break;
                case "previous":
                    commandEndpoint = "/previous";
                    httpMethod = HttpMethod.Post;
                    break;
                case "volume":
                    commandEndpoint = "/volume";
                    httpMethod = HttpMethod.Put;
                    fullUrl += $"{commandEndpoint}?volume_percent={volumePercent}&device_id={_deviceId}";
                    break;
                case "queue":
                    commandEndpoint = "/queue";
                    httpMethod = HttpMethod.Get;
                    break;
                case "currentsong":
                    commandEndpoint = "/currently-playing";
                    httpMethod = HttpMethod.Get;
                    break;
                case "transfer":
                    commandEndpoint = "";
                    httpMethod = HttpMethod.Put;
                    fullUrl = apiUrl;
                    requestBody = "{\"device_ids\": [\"" + _deviceId + "\"]}";
                    break;
                case "addsong":
                    commandEndpoint = "/queue";
                    httpMethod = HttpMethod.Post;
                    fullUrl = apiUrl + $"{commandEndpoint}?uri={Uri.EscapeDataString(_trackUri)}&device_id={_deviceId}";
                    Dictionary<string, string> storedRequests = JsonConvert.DeserializeObject<Dictionary<string, string>>(CPH.GetGlobalVar<string>("songRequests", false) ?? "{}");
                    if (storedRequests.ContainsKey(_trackUri))
                    {
                        CPH.SendMessage("That song is already in the queue!", true);
                        break;
                    }
                    else
                    {
                        storedRequests.Add(_trackUri, _user);
                        string serializedRequests = JsonConvert.SerializeObject(storedRequests);
                        CPH.SetGlobalVar("songRequests", serializedRequests, false);
                    }

                    break;
                case "searchsong":
                    httpMethod = HttpMethod.Get;
                    fullUrl = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(_userInput)}&type=track&limit=1&offset=0";
                    break;
                case "gettrack":
                    httpMethod = HttpMethod.Get;
                    fullUrl = $"https://api.spotify.com/v1/tracks/{_trackUri}";
                    break;
                case "mysongs":
                    commandEndpoint = "/queue";
                    httpMethod = HttpMethod.Get;
                    break;
                case "playlists":
                    httpMethod = HttpMethod.Get;
                    fullUrl = $"https://api.spotify.com/v1/me/playlists?limit=20";
                    break;
                case "setplaylist":
                    commandEndpoint = "/play";
                    requestBody = $"{{ \"context_uri\": \"{_selectedPlaylistUri}\", \"position_ms\": 0 }}";
                    break;
                case "addtoplaylist":
                    httpMethod = HttpMethod.Post;
                    fullUrl = $"https://api.spotify.com/v1/playlists/{_playlistId}/tracks";
                    requestBody = $"{{ \"uris\": [ \"{_trackUri}\" ], \"position\": 0 }}";
                    break;
            }

            if (command != "volume" && command != "searchsong" && command != "addsong" && command != "gettrack" && command != "playlists")
            {
                fullUrl += $"{commandEndpoint}?device_id={_deviceId}";
            }

            HttpContent content = null;
            if (requestBody != null)
            {
                content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            HttpRequestMessage request = new HttpRequestMessage(httpMethod, fullUrl);
            request.Content = content;
            HttpResponseMessage response = _httpClient.SendAsync(request).Result;
            if (!response.IsSuccessStatusCode)
            {
                ErrorResponse errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response.Content.ReadAsStringAsync().Result);
                if (response.StatusCode == (HttpStatusCode)401)
                {
                    if (tokenRefreshed)
                    {
                        CPH.LogError($"Failed to refresh Spotify token. StatusCode: 401, Reason: {errorResponse.Error.Message}");
                        return false;
                    }

                    if (command == "searchsong")
                    {
                        return SearchSong(_userInput);
                    }

                    return SendCommand(command, volumePercent);
                }
                else if (response.StatusCode == (HttpStatusCode)403)
                {
                    if (command == "previous")
                    {
                        CPH.SendMessage("Sorry, Spotify doesn't allow you to go any further back.", true);
                    }
                    else
                    {
                        CPH.LogError($"Spotify error- StatusCode: 403, Reason: {errorResponse.Error.Message}");
                        CPH.SendMessage("Sorry, you must have Spotify Premium to use these features.");
                        return false;
                    }
                }
                else if (response.StatusCode == (HttpStatusCode)404)
                {
                    if (!SendCommand("transfer"))
                    {
                        CPH.LogError("Failed to transfer playback.");
                        return false;
                    }

                    CPH.Wait(1000);
                    if (!SendCommand(command, volumePercent))
                    {
                        CPH.LogError($"Failed to execute the Spotify command: {command}.");
                        return false;
                    }

                    CPH.LogError($"Spotify error - StatusCode: 404, Reason: {errorResponse.Error.Message}");
                    return false;
                }
                else if (response.StatusCode == (HttpStatusCode)429)
                {
                    CPH.LogError($"Too many requests. StatusCode: 429, Reason: {errorResponse.Error.Message}");
                    CPH.SendMessage("You're making too many requests to the Spotify server in a short period of time, try again shortly.", true);
                    return false;
                }
                else
                {
                    CPH.LogError($"Failed to execute command '{command}'. StatusCode: {errorResponse.Error.Status}, Reason: {errorResponse.Error.Message}");
                    return false;
                }
            }

            if (command == "queue")
            {
                string queueResponse = response.Content.ReadAsStringAsync().Result;
                var songs = ExtractSongs(queueResponse, _numSongs);
                if (songs != null && songs.Any())
                {
                    var message = ConstructQueueMessage(songs, _numSongs);
                    CPH.SendMessage(message, true);
                }
                else
                {
                    CPH.SendMessage("No track currently playing.", true);
                }
            }
            else if (command == "mysongs")
            {
                string queueResponse = response.Content.ReadAsStringAsync().Result;
                var songs = ExtractSongs(queueResponse, _numSongs);
                if (songs != null && songs.Any())
                {
                    var message = ConstructRequestsMessage(songs, _numSongs);
                    CPH.SendMessage(message, true);
                }
                else
                {
                    CPH.SendMessage("No track currently playing.", true);
                }
            }
            else if (command == "searchsong")
            {
                string responseContent = response.Content.ReadAsStringAsync().Result;
                JObject searchResponse = JObject.Parse(responseContent);
                JToken firstTrack = searchResponse.SelectToken("tracks.items[0]");
                if (firstTrack != null)
                {
                    _trackName = firstTrack["name"].ToString();
                    _artistName = string.Join(", ", firstTrack["artists"].Take(3).Select(artist => artist["name"]));
                    _trackUri = firstTrack["uri"].ToString();
                    var blockedSongs = CPH.GetGlobalVar<Dictionary<string, string>>("spotifyBlockedSongs", true);
                    if (blockedSongs != null && blockedSongs.ContainsValue(_trackUri))
                    {
                        CPH.SendMessage("Unable to request this song, it is on the 'blocked' list.");
                        return false;
                    }

                    if (_blockSongBool == false || _blockSongBool == null)
                    {
                        SendCommand("addsong");
                    }
                }
                else
                {
                    CPH.LogInfo("No track found in the search response.");
                }
            }
            else if (command == "currentsong")
            {
                string responseContent = response.Content.ReadAsStringAsync().Result;
                JObject currentResponse = JObject.Parse(responseContent);
                JToken currentItem = currentResponse.SelectToken("item");
                if (currentItem != null && currentItem["name"] != null && currentItem["artists"] != null && currentItem["artists"].Any())
                {
                    _trackName = currentItem["name"].ToString();
                    _artistName = string.Join(", ", currentItem["artists"].Take(3).Select(artist => artist["name"]));
                    string _trackUri = CPH.GetGlobalVar<string>("currentSpotifyUri", true);
                    string message = $"'{_trackName}' de {_artistName}";
                    Dictionary<string, string> storedRequests = JsonConvert.DeserializeObject<Dictionary<string, string>>(CPH.GetGlobalVar<string>("songRequests", false) ?? "{}");
                    if (storedRequests.ContainsKey(_trackUri))
                    {
                        string entry = storedRequests[_trackUri];
                        string requesterName = entry.Substring(entry.LastIndexOf(':') + 1);
                        if (CPH.GetGlobalVar<bool>("spotifyRequesterName", true))
                        {
                            message += $" (Requested by: @{requesterName})";
                        }
                    }

                    CPH.SendMessage(message, true);
                }
                else
                {
                    CPH.SendMessage("No track currently playing.", true);
                }
            }
            else if (command == "gettrack")
            {
                string responseContent = response.Content.ReadAsStringAsync().Result;
                JObject trackResponse = JObject.Parse(responseContent);
                if (trackResponse != null)
                {
                    _trackName = trackResponse["name"].ToString();
                    _artistName = string.Join(", ", trackResponse["artists"].Select(artist => artist["name"].ToString()));
                    _trackUri = trackResponse["uri"].ToString();
                    var blockedSongs = CPH.GetGlobalVar<Dictionary<string, string>>("spotifyBlockedSongs", true);
                    if (blockedSongs != null && blockedSongs.ContainsValue(_trackUri))
                    {
                        CPH.SendMessage("Unable to request this song, it is on the 'blocked' list.");
                        return false;
                    }

                    if (_blockSongBool == false || _blockSongBool == null)
                    {
                        SendCommand("addsong");
                    }
                }
                else
                {
                    CPH.LogInfo("No track found in the get track response.");
                }
            }
            else if (command == "addsong")
            {
                CPH.SendMessage($"@{_user} agregó '{_trackName}' de {_artistName}.", true);
            }
            else if (command == "playlists")
            {
                string responseContent = response.Content.ReadAsStringAsync().Result;
                JObject playlistResponse = JObject.Parse(responseContent);
                if (playlistResponse != null)
                {
                    string currentUserId = CPH.GetGlobalVar<string>("spotifyUserId", true);
                    var messages = ConstructPlaylistMessages(playlistResponse, currentUserId);
                    foreach (var message in messages)
                    {
                        CPH.SendMessage(message, true);
                        CPH.Wait(100);
                    }
                }
                else
                {
                    CPH.SendMessage("You have no Spotify playlists.", true);
                }
            }

            if (command == "addtoplaylist")
            {
                string playlistName = null;
                var storedPlaylists = CPH.GetGlobalVar<List<string>>("spotifyPlaylists", true);
                foreach (var playlistInfo in storedPlaylists)
                {
                    string[] parts = playlistInfo.Split(new[] { ", Uri: " }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        string[] nameAndUriPart = parts[1].Split(new[] { ", Owned: " }, StringSplitOptions.None);
                        string playlistUri = nameAndUriPart[0];
                        string playlistId = playlistUri.Split(new[] { "spotify:playlist:" }, StringSplitOptions.None).LastOrDefault();
                        if (playlistId == _playlistId)
                        {
                            playlistName = parts[0].Split(new[] { ", Name: " }, StringSplitOptions.None).LastOrDefault();
                            break;
                        }
                    }
                }

                if (playlistName != null)
                {
                    CPH.SendMessage($"The current song has been added to the playlist: {playlistName}.", true);
                }
                else
                {
                    CPH.LogInfo("Failed to retrieve playlist name.");
                }
            }
            else
            {
                CPH.LogInfo($"Spotify Command: '{command}' executed successfully.");
            }

            return true;
        }
        catch (Exception ex)
        {
            CPH.LogError($"Exception during Spotify Command: '{command}': {ex.Message}");
            return false;
        }
    }

    private List<JToken> ExtractSongs(string queueResponse, int _numSongs)
    {
        var songs = new List<JToken>();
        try
        {
            var response = JObject.Parse(queueResponse);
            if (response == null)
            {
                CPH.LogError("Response object is null.");
                return songs;
            }

            var queue = response["queue"];
            if (queue != null && queue.HasValues)
            {
                var numSongs = Math.Min(_numSongs, queue.Count());
                for (int i = 0; i < numSongs; i++)
                {
                    songs.Add(queue[i]);
                }
            }
            else
            {
                CPH.LogInfo("No songs found in the queue.");
            }
        }
        catch (Exception ex)
        {
            CPH.LogError($"Error extracting songs: {ex.Message}");
        }

        return songs;
    }

    private string ConstructQueueMessage(List<JToken> songs, int _numSongs)
    {
        if (songs.Count == 0)
        {
            return "You must press 'Play' on Spotify to return queue information!";
        }

        string message = $"The next {(_numSongs > 1 ? _numSongs.ToString() + " songs are:" : "song is")} ";
        for (int i = 0; i < _numSongs && i < songs.Count; i++)
        {
            var currentItem = songs[i];
            string songName = currentItem["name"].ToString();
            string artistNames = string.Join(", ", currentItem["artists"].Select(artist => artist["name"]));
            string _trackUri = currentItem["uri"].ToString();
            Dictionary<string, string> storedRequests = JsonConvert.DeserializeObject<Dictionary<string, string>>(CPH.GetGlobalVar<string>("songRequests", false) ?? "{}");
            if (storedRequests.ContainsKey(_trackUri))
            {
                string entry = storedRequests[_trackUri];
                string requesterName = entry.Substring(entry.LastIndexOf(':') + 1);
                if (CPH.GetGlobalVar<bool>("spotifyRequesterName", true))
                {
                    message += $"'{songName}' de {artistNames} (Pedido por: @{requesterName})";
                }
                else
                {
                    message += $"'{songName}' de {artistNames}";
                }
            }
            else
            {
                message += $"'{songName}' de {artistNames}";
            }

            if (i < _numSongs - 1 && i < songs.Count - 1)
            {
                message += " // ";
            }

            if (message.Length > 500)
            {
                return "Results exceeded the 500 character limit. Please lower the amount of requested songs and try again.";
            }
        }

        message += ".";
        return message;
    }

    private string ConstructRequestsMessage(List<JToken> songs, int _numSongs)
    {
        string message = "";
        List<string> foundSongs = new List<string>();
        Dictionary<string, string> storedRequests = JsonConvert.DeserializeObject<Dictionary<string, string>>(CPH.GetGlobalVar<string>("songRequests", false) ?? "{}");
        for (int i = 0; i < _numSongs && i < songs.Count; i++)
        {
            var currentItem = songs[i];
            string _trackUri = currentItem["uri"].ToString();
            if (storedRequests.ContainsKey(_trackUri))
            {
                string entry = storedRequests[_trackUri];
                string requesterName = entry.Substring(entry.LastIndexOf(':') + 1);
                foundSongs.Add($"#{i + 1}");
            }
        }

        if (foundSongs.Count == 1)
        {
            message = $"@{_user}, your song is {foundSongs[0]} in the queue.";
        }
        else if (foundSongs.Count > 1)
        {
            message = $"@{_user}, your songs are {string.Join(", ", foundSongs)} in the queue.";
        }
        else
        {
            message = $"Sorry @{_user}, you don't have any requests in the queue.";
        }

        return message;
    }

    private string ConstructLastSongMessage(List<Dictionary<string, string>> songs, int _numSongs)
    {
        string message = $"The last {(_numSongs > 1 ? _numSongs.ToString() + " songs were:" : "song was")} ";
        int startIndex = songs.Count - _numSongs;
        if (startIndex < 0)
        {
            startIndex = 0;
        }

        for (int i = startIndex; i < startIndex + _numSongs && i < songs.Count; i++)
        {
            var currentItem = songs[i];
            string songName = currentItem["SongName"];
            string artistName = currentItem["ArtistName"];
            string requesterName = null;
            if (_numSongs > 1 && i > startIndex)
            {
                requesterName = songs[i - 1].ContainsKey("RequesterName") ? songs[i - 1]["RequesterName"] : null;
            }
            else if (_numSongs == 1)
            {
                requesterName = currentItem.ContainsKey("RequesterName") ? currentItem["RequesterName"] : null;
            }

            if (i > startIndex)
            {
                message += " // ";
            }

            if (CPH.GetGlobalVar<bool>("spotifyRequesterName", true) && requesterName != null)
            {
                message += $"'{songName}' de {artistName} (Pedido por: @{requesterName})";
            }
            else
            {
                message += $"'{songName}' de {artistName}";
            }

            if (message.Length > 500)
            {
                return "Results exceeded the 500 character limit. Please lower the amount of requested songs and try again.";
            }
        }

        message += ".";
        return message;
    }

    private List<string> ConstructPlaylistMessages(JObject playlistResponse, string currentUserId)
    {
        List<string> messages = new List<string>();
        if (playlistResponse == null || !playlistResponse.HasValues)
        {
            messages.Add("You have no Spotify playlists.");
            return messages;
        }

        var playlists = playlistResponse["items"];
        var storedPlaylists = new List<string>();
        int playlistCount = playlists.Count();
        string messagePrefix = playlistCount > 1 ? "Your Spotify playlists are: " : "Your Spotify playlist is: ";
        string currentMessage = messagePrefix;
        for (int i = 0; i < playlistCount; i++)
        {
            var playlist = playlists[i];
            string playlistName = playlist["name"].ToString();
            string playlistUri = playlist["uri"].ToString();
            string ownerUserId = playlist["owner"]["id"].ToString();
            bool owned = ownerUserId == currentUserId;
            int index = i + 1;
            string playlistInfo = $"Index: {index}, Name: {playlistName}, Uri: {playlistUri}, Owned: {owned}";
            storedPlaylists.Add(playlistInfo);
            string playlistEntry = $"({index}) - {playlistName}";
            if (owned)
            {
                playlistEntry += " (owned)";
            }

            if ((currentMessage + playlistEntry).Length > 500)
            {
                messages.Add(currentMessage.TrimEnd(' ', '/'));
                currentMessage = messagePrefix + playlistEntry;
            }
            else
            {
                currentMessage += playlistEntry;
                if (i < playlistCount - 1)
                {
                    currentMessage += " // ";
                }
            }
        }

        if (!string.IsNullOrEmpty(currentMessage))
        {
            messages.Add(currentMessage.TrimEnd(' ', '/'));
        }

        CPH.SetGlobalVar("spotifyPlaylists", storedPlaylists, true);
        return messages;
    }

    public bool IsTokenExpired()
    {
        try
        {
            string expirationTimeString = CPH.GetGlobalVar<string>("spotifyTokenExpires", true);
            if (DateTime.TryParseExact(expirationTimeString, "MM/dd/yy - HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime expirationTime))
            {
                if (expirationTime > DateTime.Now)
                {
                    return false;
                }
                else
                {
                    CPH.ExecuteMethod("_Spotify_Auto_Runner", "SpotifyTokenRefresh");
                    return true;
                }
            }
            else
            {
                CPH.LogError($"Error parsing token expiration time: {expirationTimeString}");
                return true;
            }
        }
        catch (Exception ex)
        {
            CPH.LogError($"Exception in IsTokenExpiredError(): {ex.Message}");
            return true;
        }
    }

    public bool SetSpotifySongsToChat()
    {
        bool spotifySongsToChat = CPH.GetGlobalVar<bool>("spotifySongsToChat", true);
        bool toggledValue;
        if (args.ContainsKey("input0") && !string.IsNullOrWhiteSpace(args["input0"].ToString()))
        {
            string input = args["input0"].ToString().ToLower();
            if (input == "yes" || input == "true" || input == "on" || input == "y")
            {
                toggledValue = true;
            }
            else if (input == "no" || input == "false" || input == "off" || input == "n")
            {
                toggledValue = false;
            }
            else
            {
                CPH.SendMessage("You may only use 'Yes', 'On', or 'True' to enable and 'No', 'Off', or 'False' to disable Spotify from displaying song information in the chat.");
                return spotifySongsToChat;
            }

            if (toggledValue == spotifySongsToChat)
            {
                CPH.SendMessage($"Spotify is already {(toggledValue ? "showing" : "not showing")} new songs in the chat.");
                return spotifySongsToChat;
            }

            CPH.SetGlobalVar("spotifySongsToChat", toggledValue, true);
        }
        else
        {
            toggledValue = !spotifySongsToChat;
            CPH.SetGlobalVar("spotifySongsToChat", toggledValue, true);
        }

        CPH.SendMessage($"Spotify will now {(toggledValue ? "show" : "no longer show")} new songs in the chat when they play.", true);
        return toggledValue;
    }

    public bool SpotifyRequesterNames()
    {
        bool spotifyRequesterName = CPH.GetGlobalVar<bool>("spotifyRequesterName", true);
        bool toggledValue;
        if (args.ContainsKey("input0") && !string.IsNullOrWhiteSpace(args["input0"].ToString()))
        {
            string input = args["input0"].ToString().ToLower();
            if (input == "yes" || input == "true" || input == "on" || input == "y")
            {
                toggledValue = true;
            }
            else if (input == "no" || input == "false" || input == "off" || input == "n")
            {
                toggledValue = false;
            }
            else
            {
                CPH.SendMessage("You may only use 'Yes', 'On', or 'True' to enable and 'No', 'Off', or 'False' to disable Spotify from displaying the Song Requester's in the chat.");
                return spotifyRequesterName;
            }

            if (toggledValue == spotifyRequesterName)
            {
                CPH.SendMessage($"Spotify is already {(toggledValue ? "showing" : "not showing")} the Song Requester's name in the chat.");
                return spotifyRequesterName;
            }

            CPH.SetGlobalVar("spotifyRequesterName", toggledValue, true);
        }
        else
        {
            toggledValue = !spotifyRequesterName;
            CPH.SetGlobalVar("spotifyRequesterName", toggledValue, true);
        }

        CPH.SendMessage($"Spotify will {(toggledValue ? "show" : "no longer show")} the Song Requester's name in the chat when they play.", true);
        return toggledValue;
    }
}

public class ErrorResponse
{
    [JsonProperty("error")]
    public Error Error { get; set; }
}

public class Error
{
    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}//code bastardized by Mustached_Maniac
//https://ko-fi.com/mustached_maniac/tip