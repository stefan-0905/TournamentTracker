using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackerLibrary.Models;

namespace TrackerLibrary.DataAccess.TextHelpers
{
    public static class TextConnectorProcessor
    {
        /// <summary>
        /// Returns full file path of specified path.
        /// </summary>
        /// <param name="fileName">Specified path.</param>
        /// <returns>Full file path</returns>
        public static string FullFilePath(this string fileName)
        {
            return $"{ ConfigurationManager.AppSettings["filePath"] }\\{ fileName }";
        }
        public static List<string> LoadFile(this string file)
        {
            if (!File.Exists(file))
            {
                return new List<string>();
            }

            return File.ReadAllLines(file).ToList();
        }
        public static List<PrizeModel> ConvertToPrizeModels(this List<string> lines)
        {
            List<PrizeModel> output = new List<PrizeModel>();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');
                PrizeModel newPrize = new PrizeModel();
                newPrize.Id = int.Parse(cols[0]);
                newPrize.PlaceNumber = int.Parse(cols[1]);
                newPrize.PlaceName = cols[2];
                newPrize.PrizeAmount = decimal.Parse(cols[3]);
                newPrize.PrizePercentage = double.Parse(cols[4]);

                output.Add(newPrize);
            }

            return output;
        }
        public static List<TeamModel> ConvertToTeamModels(this List<string> lines)
        {
            // id,team name,list of ids seperated by the pipe
            // 3,Stef's team,1|2|3
            List<TeamModel> output = new List<TeamModel>();

            List<PersonModel> people = GlobalConfig.PeopleFile.FullFilePath().LoadFile().ConvertToPersonModels();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');
                TeamModel team = new TeamModel();
                team.Id = int.Parse(cols[0]);
                team.TeamName = cols[1];

                string[] personIds = cols[2].Split('|');

                foreach (string id in personIds)
                {
                    team.TeamMembers.Add(people.Where(x => x.Id == int.Parse(id)).First());
                }

                output.Add(team);
            }

            return output;
        }
        public static List<TournamentModel> ConvertToTournamentModels(this List<string> lines)
        {
            //id,TournamentName,EntryFee,(id|id|id - Entered Teams),(id,id,id - Prizes),(Rounds - id^id^id|id^id^id|id^id^id)
            List<TournamentModel> tournaments = new List<TournamentModel>();
            List<TeamModel> teams = GlobalConfig.TeamFile.FullFilePath().LoadFile().ConvertToTeamModels();
            List<PrizeModel> prizes = GlobalConfig.PrizesFile.FullFilePath().LoadFile().ConvertToPrizeModels();
            List<MatchupModel> matchups = GlobalConfig.MatchupFile.FullFilePath().LoadFile().ConvertToMatchupModels();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');

                TournamentModel tournament = new TournamentModel();
                tournament.Id = int.Parse(cols[0]);
                tournament.TournamentName = cols[1];
                tournament.EntryFee = decimal.Parse(cols[2]);

                string[] teamIds = cols[3].Split('|');
                foreach (string id in teamIds)
                {
                    tournament.EnteredTeams.Add(teams.Where(x => x.Id == int.Parse(id)).First());
                }

                if (cols[4].Length > 0)
                {
                    string[] prizeIds = cols[4].Split('|');
                    foreach (string id in prizeIds)
                    {
                        tournament.Prizes.Add(prizes.Where(x => x.Id == int.Parse(id)).First());
                    }
                }

                // Capture rounds information
                string[] rounds = cols[5].Split('|');
                foreach (string round in rounds)
                {
                    string[] msText = round.Split('^');
                    List<MatchupModel> ms = new List<MatchupModel>();

                    foreach (string matchupModelTextId in msText)
                    {
                        ms.Add(matchups.Where(x => x.Id == int.Parse(matchupModelTextId)).First());
                    }

                    tournament.Rounds.Add(ms);
                }

                tournaments.Add(tournament);
            }
            return tournaments;
        }
        public static List<PersonModel> ConvertToPersonModels(this List<string> lines)
        {
            List<PersonModel> output = new List<PersonModel>();

            foreach(string line in lines)
            {
                string[] cols = line.Split(',');
                PersonModel newPerson = new PersonModel()
                {
                    Id = int.Parse(cols[0]),
                    FirstName = cols[1],
                    LastName = cols[2],
                    EmailAddress = cols[3],
                    CellphoneNumber = cols[4]
                };

                output.Add(newPerson);
            }
            return output;
        }
        public static void SaveToPrizeFile(this List<PrizeModel> models)
        {
            List<string> lines = new List<string>();

            foreach (PrizeModel p in models)
            {
                lines.Add($"{ p.Id },{ p.PlaceNumber },{ p.PlaceName },{ p.PrizeAmount },{ p.PrizePercentage }");
            }

            File.WriteAllLines(GlobalConfig.PrizesFile.FullFilePath(), lines);
        }
        public static void SaveToPersonFile(this List<PersonModel> models)
        {
            List<string> lines = new List<string>();

            foreach(PersonModel p in models)
            {
                lines.Add($"{p.Id},{p.FirstName},{p.LastName},{p.EmailAddress},{p.CellphoneNumber}");
            }

            File.WriteAllLines(GlobalConfig.PeopleFile.FullFilePath(), lines);
        }

        public static void SaveToTeamFile(this List<TeamModel> models, string fileName)
        {
            List<string> lines = new List<string>();

            foreach (TeamModel team in models)
            {
                lines.Add($"{team.Id},{team.TeamName},{ConvertPeopleListToString(team.TeamMembers)}");
            }

            File.WriteAllLines(GlobalConfig.TeamFile.FullFilePath(), lines);
        } 
        public static void SaveRoundsToFile(this TournamentModel model)
        {
            // Loop through each round
            // Loop through each Matchup
            // Get the id for the new Matchup and save the end record
            // Loop through the each entry, get the id, and save it

            foreach (List<MatchupModel> round in model.Rounds)
            {
                foreach (MatchupModel matchup in round)
                {
                    // Load all the matchups from file
                    // Get the top id and increment it
                    // Store the id
                    // Save the matchup record
                    matchup.SaveMatchupToFile();

                    
                }
            }
        }
        private static List<MatchupEntryModel> ConvertStringToMatchupEntryModels(string input)
        {
            string[] ids = input.Split('|');
            List<MatchupEntryModel> output = new List<MatchupEntryModel>();
            List<string> entries = GlobalConfig.MatchupEntryFile.FullFilePath().LoadFile();
            List<string> matchingEntries = new List<string>();
            foreach(string id in ids)
            {
                foreach (string entry in entries)
                {
                    string[] cols = entry.Split(',');
                    if (cols[0] == id)
                    {
                        matchingEntries.Add(entry);
                    }
                }
            }
            output = matchingEntries.ConvertToMatchupEntryModels();
            return output;
        }
        public static List<MatchupEntryModel> ConvertToMatchupEntryModels(this List<string> lines)
        {
            //id=0,teamCompeting=1,score=2,parentMatchup=3
            List<MatchupEntryModel> output = new List<MatchupEntryModel>();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');

                MatchupEntryModel entryM = new MatchupEntryModel();
                entryM.Id = int.Parse(cols[0]);
                if(cols[1].Length == 0)
                {
                    entryM.TeamCompeting = null;
                }
                else
                {
                    entryM.TeamCompeting = LookupTeamById(int.Parse(cols[1]));
                }
                entryM.Score = double.Parse(cols[2]);
                int parentId = 0;
                if (int.TryParse(cols[3], out parentId))
                {
                    entryM.ParentMatchup = LookupMatchupById(int.Parse(cols[3]));
                }
                else
                { 
                    entryM.ParentMatchup = null;
                }

                output.Add(entryM);
            }

            return output;
        }
        private static TeamModel LookupTeamById(int id)
        {
            List<string> teams = GlobalConfig.TeamFile.FullFilePath().LoadFile();

            foreach (string team in teams)
            {
                string[] cols = team.Split(',');
                if(cols[0] == id.ToString())
                {
                    List<string> matchingTeams = new List<string>();
                    matchingTeams.Add(team);
                    return matchingTeams.ConvertToTeamModels().First();
                }
            }
            return null;
        }
        private static MatchupModel LookupMatchupById(int id)
        {
            List<string> matchups = GlobalConfig.MatchupFile.FullFilePath().LoadFile();

            foreach (string matchup in matchups)
            {
                string[] cols = matchup.Split(',');
                if(cols[0] == id.ToString())
                {
                    List<string> matchingMatchups = new List<string>();
                    matchingMatchups.Add(matchup);
                    return matchingMatchups.ConvertToMatchupModels().First();
                }
            }
            return null;
        }
        public static List<MatchupModel> ConvertToMatchupModels(this List<string> lines)
        {
            //id=0,entries=1(pipe delimited by id), winner=2, mathupRound=3
            List<MatchupModel> output = new List<MatchupModel>();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');
                MatchupModel matchup = new MatchupModel();
                matchup.Id = int.Parse(cols[0]);
                matchup.Entries = ConvertStringToMatchupEntryModels(cols[1]);
                if(cols[2].Length == 0)
                {
                    matchup.Winner = null;
                }
                else
                {
                    matchup.Winner = LookupTeamById(int.Parse(cols[2]));
                }
                matchup.MatchupRound = int.Parse(cols[3]);

                output.Add(matchup);
            }

            return output;
        }
        public static void SaveMatchupToFile(this MatchupModel matchup)
        {
            List<MatchupModel> matchups = GlobalConfig.MatchupFile.FullFilePath().LoadFile().ConvertToMatchupModels();

            int currentId = 1;

            if (matchups.Count > 0)
            {
                currentId = matchups.OrderByDescending(x => x.Id).First().Id + 1;
            }

            matchup.Id = currentId;
            matchups.Add(matchup);
            
            foreach (MatchupEntryModel entry in matchup.Entries)
            {
                entry.SaveEntryToFile();
            }

            // Save to file

            List<string> lines = new List<string>();

            foreach (MatchupModel m in matchups)
            {
                string winner = "";

                if (m.Winner != null)
                // Check if matchuEntry has parent matchup
                {
                    winner = m.Winner.Id.ToString();
                }

                lines.Add($"{m.Id},{ConvertMatchupEntryListToString(m.Entries)},{winner},{m.MatchupRound}");
            }
            File.WriteAllLines(GlobalConfig.MatchupFile.FullFilePath(), lines);
        }
        public static void UpdateMatchupToFile(this MatchupModel matchup)
        {
            List<MatchupModel> matchups = GlobalConfig.MatchupFile.FullFilePath().LoadFile().ConvertToMatchupModels();

            // Removing old record of matchup we are trying to update
            MatchupModel oldMatchup = new MatchupModel();
            foreach (MatchupModel myMatch in matchups)
            {
                if (myMatch.Id == matchup.Id)
                    oldMatchup = myMatch;
            }
            matchups.Remove(oldMatchup);

            // Inserting updated matchup
            matchups.Add(matchup);

            // Update matchup entries
            foreach (MatchupEntryModel entry in matchup.Entries)
            {
                entry.UpdateEntryToFile();
            }

            // Save to file

            List<string> lines = new List<string>();

            foreach (MatchupModel m in matchups)
            {
                string winner = "";

                if (m.Winner != null)
                // Check if matchuEntry has parent matchup
                {
                    winner = m.Winner.Id.ToString();
                }

                lines.Add($"{m.Id},{ConvertMatchupEntryListToString(m.Entries)},{winner},{m.MatchupRound}");
            }
            File.WriteAllLines(GlobalConfig.MatchupFile.FullFilePath(), lines);
        }
        public static void SaveEntryToFile(this MatchupEntryModel entry)
        {
            List<MatchupEntryModel> entries = GlobalConfig.MatchupEntryFile.FullFilePath().LoadFile().ConvertToMatchupEntryModels();

            int currentId = 1;

            if(entries.Count > 0)
            {
                currentId = entries.OrderByDescending(x => x.Id).First().Id + 1;
            }

            entry.Id = currentId;
            entries.Add(entry);

            // Save to file

            List<string> lines = new List<string>();

            foreach (MatchupEntryModel matchupE in entries)
            {
                string parent = string.Empty;

                if(matchupE.ParentMatchup != null)
                // Check if matchuEntry has parent matchup
                {
                    parent = matchupE.ParentMatchup.Id.ToString();
                }
                string teamCompeting = string.Empty;
                if(matchupE.TeamCompeting != null)
                {
                    teamCompeting = matchupE.TeamCompeting.Id.ToString();
                }
                lines.Add($"{matchupE.Id},{teamCompeting},{matchupE.Score},{parent}");
            }
            File.WriteAllLines(GlobalConfig.MatchupEntryFile.FullFilePath(), lines);


        }
        public static void UpdateEntryToFile(this MatchupEntryModel entry)
        {
            List<MatchupEntryModel> entries = GlobalConfig.MatchupEntryFile.FullFilePath().LoadFile().ConvertToMatchupEntryModels();

            // Removing old record of entry we are trying to update
            MatchupEntryModel oldEntry = new MatchupEntryModel();
            foreach (MatchupEntryModel entryModel in entries)
            {
                if(entryModel.Id == entry.Id)
                {
                    oldEntry = entryModel;
                }
            }
            entries.Remove(oldEntry);

            // Inserting new record
            entries.Add(entry);

            // Save to file

            List<string> lines = new List<string>();

            foreach (MatchupEntryModel matchupE in entries)
            {
                string parent = string.Empty;

                if (matchupE.ParentMatchup != null)
                // Check if matchuEntry has parent matchup
                {
                    parent = matchupE.ParentMatchup.Id.ToString();
                }
                string teamCompeting = string.Empty;
                if (matchupE.TeamCompeting != null)
                {
                    teamCompeting = matchupE.TeamCompeting.Id.ToString();
                }
                lines.Add($"{matchupE.Id},{teamCompeting},{matchupE.Score},{parent}");
            }
            File.WriteAllLines(GlobalConfig.MatchupEntryFile.FullFilePath(), lines);


        }
        public static void SaveToTournamentFile(this List<TournamentModel> models)
        {
            List<string> lines = new List<string>();
            foreach(TournamentModel tournament in models)
            {
                lines.Add($"{tournament.Id},{tournament.TournamentName},{tournament.EntryFee},{ConvertTeamListToString(tournament.EnteredTeams)},{ConvertPrizeListToString(tournament.Prizes)},{ConvertRoundListToString(tournament.Rounds)}");
            }

            File.WriteAllLines(GlobalConfig.TournamentFile.FullFilePath(), lines);
        }
        private static string ConvertRoundListToString(List<List<MatchupModel>> rounds)
        {
            string output = "";

            if (rounds.Count == 0)
            {
                return "";
            }

            foreach (List<MatchupModel> round in rounds)
            {
                output += $"{ ConvertMatchupListToString(round) }|";
            }

            output = output.Substring(0, output.Length - 1);
            return output;
        }
        private static string ConvertMatchupListToString(List<MatchupModel> matchups)
        {
            string output = "";

            if (matchups.Count == 0)
            {
                return "";
            }

            foreach (MatchupModel matchup in matchups)
            {
                output += $"{ matchup.Id }^";
            }

            output = output.Substring(0, output.Length - 1);
            return output;
        }
        private static string ConvertMatchupEntryListToString(List<MatchupEntryModel> entries)
        {
            string output = "";

            if (entries.Count == 0)
            {
                return "";
            }

            foreach (MatchupEntryModel entry in entries)
            {
                output += $"{ entry.Id }|";
            }

            output = output.Substring(0, output.Length - 1);
            return output;
        }
        private static string ConvertPrizeListToString(List<PrizeModel> prizes)
        {
            string output = "";

            if (prizes.Count == 0)
            {
                return "";
            }

            foreach (PrizeModel prize in prizes)
            {
                output += $"{ prize.Id }|";
            }

            output = output.Substring(0, output.Length - 1);
            return output;
        }
        private static string ConvertTeamListToString(List<TeamModel> teams)
        {
            string output = "";

            if (teams.Count == 0)
            {
                return "";
            }

            foreach (TeamModel team in teams)
            {
                output += $"{ team.Id }|";
            }

            output = output.Substring(0, output.Length - 1);
            return output;
        }
        private static string ConvertPeopleListToString(List<PersonModel> people)
        {
            string output = "";

            if(people.Count == 0)
            {
                return "";
            }

            foreach (PersonModel person in people)
            {
                output += $"{ person.Id }|";
            }

            output = output.Substring(0, output.Length - 1);
            return output;
        }
    }
}
