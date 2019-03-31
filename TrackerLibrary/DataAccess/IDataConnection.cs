using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackerLibrary.Models;

namespace TrackerLibrary.DataAccess
{
    public interface IDataConnection
    {
        /// <summary>
        /// Save new Prize to database.
        /// </summary>
        /// <param name="model">Prize to be saved.</param>
        void CreatePrize(PrizeModel model);

        /// <summary>
        /// Save new Person to database.
        /// </summary>
        /// <param name="model">Person to be saved.</param>
        void CreatePerson(PersonModel model);

        /// <summary>
        /// Save new Team to database and alse save it's relation to team members
        /// </summary>
        /// <param name="model">Team information</param>
        void CreateTeam(TeamModel model);

        /// <summary>
        /// Save new Tournament to database and save needed relations .
        /// </summary>
        /// <param name="model">Tournament information.</param>
        void CreateTournament(TournamentModel model);

        /// <summary>
        /// Update matchup. Set winner, and set scores of matchup entries
        /// </summary>
        /// <param name="model">Matchup information</param>
        void UpdateMatchup(MatchupModel model);

        /// <summary>
        /// If using sql db, set active column to 0 ( set to be inactive).
        /// Else if using file system delete all tournament information completely.
        /// </summary>
        /// <param name="model">Tournament information.</param>
        void CompleteTournament(TournamentModel model);

        /// <summary>
        /// Get all people from database.
        /// </summary>
        /// <returns>List of people.</returns>
        List<PersonModel> GetPerson_All();

        /// <summary>
        /// Get all teams.
        /// </summary>
        /// <returns>List of teams.</returns>
        List<TeamModel> GetTeam_All();

        /// <summary>
        /// Get all active tournaments. 
        /// </summary>
        /// <returns>List of tournaments.</returns>
        List<TournamentModel> GetTournament_All();

    }
}
