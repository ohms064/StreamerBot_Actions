using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SBCustomClasses.TSH;
using SocketIOClient.Newtonsoft.Json;


namespace TestEnviroment
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Test();
            //SocketTest();
            //Test2();
        }

        private static void Test()
        {
            var entrantsJson = File.ReadAllText("D:\\Streams\\localmatch\\current_match.json");
            var entrants = JsonConvert.DeserializeObject<List<EntrantRequest>>(entrantsJson);
            var team = 1;
            
            var score = new ScoreRequest()
            {
                Team1Score = 15,
                Team2Score = 2,
            };
        
            Task.Run(
                async () =>
                {
                    var socketUri = "ws://127.0.0.1:5000/";
                    var socket = new SocketIOClient.SocketIO(socketUri);

                    await socket.ConnectAsync();
                    
                    foreach (var entrant in entrants)
                    {
                        entrant.Team = team;
                        await socket.EmitAsync("update_team", JsonConvert.SerializeObject(entrant));
                        team++;
                    }
        
                    await socket.EmitAsync("score", JsonConvert.SerializeObject(score));
                    await socket.DisconnectAsync();
        
                }).GetAwaiter().GetResult();
        
        }

        // private static void SocketTest()
        // {
        //     var socket = new SocketIOSharp.Client.SocketIOClient(
        //         new SocketIOClientOption(EngineIOScheme.http, "127.0.0.1", 5000, Reconnection:true));
        //     socket.Connect();
        //     var entrantsJson = File.ReadAllText("D:\\Streams\\localmatch\\current_match.json");
        //     var entrants = JsonConvert.DeserializeObject<List<EntrantRequest>>(entrantsJson);
        //     var team = 1;
        //     
        //     foreach(var entrant in entrants)
        //     {
        //         entrant.Team = team;
        //         socket.Emit("update_team", JsonConvert.SerializeObject(entrant));
        //         team++;
        //     }
        //
        //     var score = new ScoreRequest()
        //     {
        //         Team1Score = 15,
        //         Team2Score = 2,
        //     };
        //     socket.Emit("score", JsonConvert.SerializeObject(score));
        //     socket.Close();
        // }

        // private static void Test2()
        // {
        //     var entrantsJson = File.ReadAllText("D:\\Streams\\localmatch\\current_match.json");
        //     var entrants = JsonConvert.DeserializeObject<List<EntrantRequest>>(entrantsJson);
        //     var team = 1;
        //     
        //     var score = new ScoreRequest()
        //     {
        //         Team1Score = 15,
        //         Team2Score = 2,
        //     };
        //     
        //     Task.Run(
        //         async () =>
        //         {
        //             var socketUri = "ws://127.0.0.1:5000/";
        //             var socket = new H.Socket.IO.SocketIoClient();
        //             await socket.ConnectAsync(new Uri(socketUri));
        //             
        //             foreach (var entrant in entrants)
        //             {
        //                 entrant.Team = team;
        //                 await socket.Emit("update_team", JsonConvert.SerializeObject(entrant));
        //                 team++;
        //             }
        //
        //             await socket.Emit("score", JsonConvert.SerializeObject(score));
        //             await socket.DisconnectAsync();
        //         }
        //         ).GetAwaiter().GetResult();
        // }
    }
}