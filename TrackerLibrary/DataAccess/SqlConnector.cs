using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackerLibrary.Models;

namespace TrackerLibrary.DataAccess
{
    public class SqlConnector : IDataConnection
    {
        private const string db = "Tournaments";

        public void CreatePrize(PrizeModel model)
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                var p = new DynamicParameters();
                p.Add("@PlaceNumber", model.PlaceNumber);
                p.Add("@PlaceName", model.PlaceName);
                p.Add("@PrizeAmount", model.PrizeAmount);
                p.Add("@PrizePercentage", model.PrizePercentage);
                p.Add("@id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spPrizes_Insert", p, commandType: CommandType.StoredProcedure);

                model.Id = p.Get<int>("@id");
            }
        }
        public void CreatePerson(PersonModel model)
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                var p = new DynamicParameters();
                p.Add("@FirstName", model.FirstName);
                p.Add("@LastName", model.LastName);
                p.Add("@EmailAddress", model.EmailAddress);
                p.Add("@CellphoneNumber", model.CellphoneNumber);
                p.Add("@id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spPeople_Insert", p, commandType: CommandType.StoredProcedure);

                model.Id = p.Get<int>("@id");
            }
        }
        public void CreateTeam(TeamModel model)
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                var p = new DynamicParameters();
                p.Add("@TeamName", model.TeamName);
                p.Add("@id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spTeams_Insert", p, commandType: CommandType.StoredProcedure);

                model.Id = p.Get<int>("@id");

                foreach(PersonModel pm in model.TeamMembers)
                {
                    p = new DynamicParameters();
                    p.Add("@TeamId", model.Id);
                    p.Add("@PersonId", pm.Id);

                    connection.Execute("dbo.spTeamMembers_Insert", p, commandType: CommandType.StoredProcedure);
                }
            }
        }
        public void CreateTournament(TournamentModel model)
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                SaveTournament(connection, model);

                SaveTournamentPrizes(connection, model);

                SaveTournamentEntries(connection, model);

                SaveTournamentRounds(connection, model);

                // Updating and moving byes in next round
                TournamentLogic.UpdateTournamentResults(model);
            }
        }

        /// <summary>
        /// Save primary tournament information to db
        /// </summary>
        /// <param name="connection">Connection to db.</param>
        /// <param name="model">Tournament information.</param>
        private void SaveTournament(IDbConnection connection, TournamentModel model)
        {
            var param = new DynamicParameters();
            param.Add("@TournamentName", model.TournamentName);
            param.Add("@EntryFee", model.EntryFee);
            param.Add("@id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

            connection.Execute("dbo.spTournaments_Insert", param, commandType: CommandType.StoredProcedure);

            model.Id = param.Get<int>("@id");
        }

        /// <summary>
        /// Save tournament relation to prizes offered in it.
        /// </summary>
        /// <param name="connection">Connection to db.</param>
        /// <param name="model">Tournament information.</param>
        private void SaveTournamentPrizes(IDbConnection connection, TournamentModel model)
        {
            foreach (PrizeModel prize in model.Prizes)
            {
                var param = new DynamicParameters();
                param.Add("@TournamentId", model.Id);
                param.Add("@PrizeId", prize.Id);
                param.Add("@id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spTournamentPrizes_Insert", param, commandType: CommandType.StoredProcedure);
            }
        }

        /// <summary>
        /// Save tournament relation to teams contesting in it.
        /// </summary>
        /// <param name="connection">Connection to db.</param>
        /// <param name="model">Tournament model.</param>
        private void SaveTournamentEntries(IDbConnection connection, TournamentModel model)
        { 
            foreach (TeamModel team in model.EnteredTeams)
            {
                var param = new DynamicParameters();
                param.Add("@TournamentId", model.Id);
                param.Add("@TeamId", team.Id);
                param.Add("@id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spTournamentEntries_Insert", param, commandType: CommandType.StoredProcedure);
            }
        }

        /// <summary>
        /// For each round in tournament
        /// Save tournament relation to matchup that will be played.
        /// And for each matchup save it's relation to teams (matchup entries) competing in it.
        /// </summary>
        /// <param name="connection">Connection to database.</param>
        /// <param name="model">Tournament information.</param>
        private void SaveTournamentRounds(IDbConnection connection, TournamentModel model)
        {
            // Loop through the rounds
            // Loop through the matchups
            // Save the matchup
            // Loop through the entries and save them

            foreach (List<MatchupModel> round in model.Rounds)
            {
                foreach (MatchupModel matchup in round)
                {
                    var param = new DynamicParameters();
                    param.Add("@TournamentId", model.Id);
                    param.Add("@MatchupRound", matchup.MatchupRound);
                    param.Add("@id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                    connection.Execute("dbo.spMatchups_Insert", param, commandType: CommandType.StoredProcedure);

                    matchup.Id = param.Get<int>("@id");

                    foreach (MatchupEntryModel entry in matchup.Entries)
                    {
                        param = new DynamicParameters();
                        param.Add("@MatchupId", matchup.Id);
                        if(entry.ParentMatchup == null)
                        {
                            param.Add("@ParentMatchupId", null);
                        }
                        else
                        {
                            param.Add("@ParentMatchupId", entry.ParentMatchup.Id);
                        }
                        if(entry.TeamCompeting == null)
                        {
                            param.Add("@TeamCompetingId", null);
                        }
                        else
                        {
                            param.Add("@TeamCompetingId", entry.TeamCompeting.Id);
                        }
                        param.Add("@id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                        connection.Execute("dbo.spMatchupEntries_Insert", param, commandType: CommandType.StoredProcedure);
                    }
                }
            }
        }

        public List<PersonModel> GetPerson_All()
        {
            List<PersonModel> output;

            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                output = connection.Query<PersonModel>("dbo.spPeople_GetAll").ToList();
            }

            return output;
        }

        public List<TeamModel> GetTeam_All()
        {
            List<TeamModel> output;

            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                output = connection.Query<TeamModel>("dbo.spTeams_GetAll").ToList();

                foreach (TeamModel team in output)
                {
                    var param = new DynamicParameters();
                    param.Add("TeamId", team.Id);
                    team.TeamMembers = connection
                        .Query<PersonModel>("dbo.spTeamMembers_GetByTeam", param, commandType: CommandType.StoredProcedure)
                        .ToList();
                }
            }

            return output;
        }

        public List<TournamentModel> GetTournament_All()
        {
            List<TournamentModel> output;

            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                output = connection.Query<TournamentModel>("dbo.spTournaments_GetAll").ToList();

                foreach (TournamentModel tournament in output)
                {
                    // Populate Prizes
                    DynamicParameters param = new DynamicParameters();
                    param.Add("@TournamentId", tournament.Id);
                    tournament.Prizes = connection.Query<PrizeModel>("dbo.spPrizes_GetByTournament", param, commandType:CommandType.StoredProcedure).ToList();

                    // Populate Teams
                    tournament.EnteredTeams = connection.Query<TeamModel>("dbo.spTeams_GetByTournament",param, commandType:CommandType.StoredProcedure).ToList();

                    foreach (TeamModel team in tournament.EnteredTeams)
                    {
                        param = new DynamicParameters();
                        param.Add("TeamId", team.Id);
                        team.TeamMembers = connection
                            .Query<PersonModel>("dbo.spTeamMembers_GetByTeam", param, commandType: CommandType.StoredProcedure)
                            .ToList();
                    }

                    // Populate Rounds
                    param = new DynamicParameters();
                    param.Add("@TournamentId", tournament.Id);
                    List<MatchupModel> matchups = connection.Query<MatchupModel>("spMatchups_GetByTournament", param, commandType: CommandType.StoredProcedure)
                            .ToList();

                    foreach (MatchupModel matchup in matchups)
                    {
                        // Populate Entries
                        param = new DynamicParameters();
                        param.Add("@MatchupId", matchup.Id);
                        matchup.Entries = connection.Query<MatchupEntryModel>("spMatchupEntries_GetByMatchup", param, commandType: CommandType.StoredProcedure)
                                .ToList();

                        List<TeamModel> allTeams = GetTeam_All();

                        // Populate Winner in MatchupModel with winnerId we got from db
                        if(matchup.WinnerId > 0)
                        {
                            matchup.Winner = allTeams.Where(x => x.Id == matchup.WinnerId).First();
                        }

                        // Populate TeamCompeting and ParentMatchup in MatchupEntryModel with coresponding IDs we got from db
                        foreach (MatchupEntryModel entry in matchup.Entries)
                        {
                            if(entry.TeamCompetingId > 0)
                            {
                                entry.TeamCompeting = allTeams.Where(x => x.Id == entry.TeamCompetingId).First();
                            }
                            if(entry.ParentMatchupId > 0)
                            {
                                entry.ParentMatchup = matchups.Where(x => x.Id == entry.ParentMatchupId).First();
                            }
                        }
                    }

                    // List<List<MatchupModel>>
                    List<MatchupModel> currRow = new List<MatchupModel>();
                    int currRound = 1;
                    foreach (MatchupModel matchup in matchups)
                    {
                        if (matchup.MatchupRound > currRound)
                        {
                            tournament.Rounds.Add(currRow);
                            currRow = new List<MatchupModel>();
                            currRound += 1;
                        }

                        currRow.Add(matchup);
                    }
                    tournament.Rounds.Add(currRow);
                }

                
            }

            return output;
        }

        public void UpdateMatchup(MatchupModel model)
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                var param = new DynamicParameters();

                if (model.Winner != null)
                {
                    param.Add("@id", model.Id);
                    param.Add("@WinnerId", model.Winner.Id);

                    connection.Execute("dbo.spMatchups_Update", param, commandType: CommandType.StoredProcedure);
                }

                foreach (MatchupEntryModel matchupEntry in model.Entries)
                {
                    if (matchupEntry.TeamCompeting != null)
                    {
                        param = new DynamicParameters();
                        param.Add("@id", matchupEntry.Id);
                        param.Add("@TeamCompetingId", matchupEntry.TeamCompeting.Id);
                        param.Add("@Score", matchupEntry.Score);

                        connection.Execute("dbo.spMatchupEntries_Update", param, commandType: CommandType.StoredProcedure);
                    }
                }
            }
        }

        /// <summary>
        /// Complete tournament by setting Active column to 0.
        /// </summary>
        /// <param name="model">Tournament information.</param>
        public void CompleteTournament(TournamentModel model)
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                DynamicParameters param = new DynamicParameters();
                param.Add("@TournamentId", model.Id);

                connection.Execute("dbo.spTournaments_Complete", param, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
