using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackerLibrary.Models
{
    /// <summary>
    /// Represents one match in the tournament.
    /// </summary>
    public class MatchupModel
    {
        /// <summary>
        /// The unique identifier for the Matchup.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// The set of teams that were involved in this match.
        /// </summary>
        public List<MatchupEntryModel> Entries { get; set; } = new List<MatchupEntryModel>();
        /// <summary>
        /// The ID from the database that will be used to identifu the winnner
        /// </summary>
        public int WinnerId { get; set; }
        /// <summary>
        /// The winner of the match.
        /// </summary>
        public TeamModel Winner { get; set; }
        /// <summary>
        /// Which round this match is part of.
        /// </summary>
        public int MatchupRound { get; set; }
        public string DisplayName
        { get
            {
                string output = "";
                foreach (MatchupEntryModel matchupEntry in Entries)
                {
                    if (matchupEntry.TeamCompeting!=null)
                    {
                        if (output.Length == 0)
                        {
                            output = matchupEntry.TeamCompeting.TeamName;
                        }
                        else
                        {
                            output += $" vs {matchupEntry.TeamCompeting.TeamName}";
                        }
                    }
                    else
                    {
                        output = "MatchupNotYetDetermined";
                        break; // We can not know 2 teams, we break so we dont have 2 x output
                    }
                }
                return output;
            }
        }
    }
}
