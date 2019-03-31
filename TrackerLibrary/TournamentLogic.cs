using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackerLibrary.Models;

namespace TrackerLibrary
{
    public static class TournamentLogic
    {
        // Order our list of teams randomly 
        // Check if it is big enough - if not, add in byes
        // Byes in sport refers to when organizers have to schedule a compertitor not to participate
        // in a given round. In our case that's because of lack of teams.
        // In our tournament 1st round has 2^n, 2nd 2^(n-1), 3rd 2^(n-2) teams competing and so on
        // Example: 
        //      If in first round enter 7 teams. there will be 3 matchups ( each matchup is played by 2 teams)
        //      Which leaves 1 team without proper pair. This matchup is refered as bye, and this team is automaticaly
        //      sent to round 2
        // Create first round of matchups
        // Create every round after that

        /// <summary>
        /// Set up rounds for given tournament.
        /// </summary>
        /// <param name="tournament">Tournament information.</param>
        public static void CreateRounds(TournamentModel tournament)
        {
            List<TeamModel> randomizedTeams = RandomizeTeamOrder(tournament.EnteredTeams);
            int rounds = FindNumberOfRounds(randomizedTeams.Count);
            int byes = NumberOfByes(rounds, randomizedTeams.Count);

            tournament.Rounds.Add(CreateFirstRound(byes, randomizedTeams));

            CreateOtherRounds(tournament, rounds);
        }

        /// <summary>
        /// Update matchup scores and send emails if new round is starting
        /// </summary>
        /// <param name="tournament">Tournament information.</param>
        public static void UpdateTournamentResults(TournamentModel tournament)
        {
            int startingRound = tournament.CheckCurrentRound();
            // Matchups that need updating of score
            List<MatchupModel> toScore = new List<MatchupModel>();

            foreach (List<MatchupModel> round in tournament.Rounds)
            {
                foreach (MatchupModel roundMatchup in round)
                {
                    // If winner is not null, that means that match has already been played out, so the score was set
                    // If both entries have score 0 that means that their score is not set yet => no need for updateing score
                    // Matchup with 1 entry is match with bye, where team 1 automaticaly wins without needing to input score
                    if(roundMatchup.Winner == null && (roundMatchup.Entries.Any(x => x.Score != 0) || roundMatchup.Entries.Count == 1))
                    {
                        toScore.Add(roundMatchup);
                    }
                }
            } 
            MarkWinnersInMatchups(toScore);

            AdvanceWinners(toScore, tournament);

            // Update every matchup who's score's been set
            toScore.ForEach(x => GlobalConfig.Connection.UpdateMatchup(x));

            // If there's been a round completed (all matchups in it were played out) alert winners of the new round start
            int endingRound = tournament.CheckCurrentRound();
            if(endingRound > startingRound)
            {
                // Alert users
                tournament.AlertUsersToNewRound();
            }
        }

        /// <summary>
        /// Notify all team members of all teams playing in the current round.
        /// </summary>
        /// <param name="tournament">Tournament information.</param>
        public static void AlertUsersToNewRound(this TournamentModel tournament)
        {
            int currentRoundNumber = tournament.CheckCurrentRound();
            List<MatchupModel> currentRound = tournament.Rounds.Where(x => x.First().MatchupRound == currentRoundNumber).First();

            foreach (MatchupModel matchup in currentRound)
            {
                foreach (MatchupEntryModel matchupEntry in matchup.Entries)
                {
                    foreach (PersonModel person in matchupEntry.TeamCompeting.TeamMembers)
                    {
                        AlertPersonToNewRound(
                            person, 
                            matchupEntry.TeamCompeting.TeamName, 
                            matchup.Entries
                                .Where(x => x.TeamCompeting != matchupEntry.TeamCompeting)
                                .FirstOrDefault()
                            );
                    }
                }
            }
        }

        /// <summary>
        /// Compose mail and send it.
        /// </summary>
        /// <param name="person">Person to whom will email be sent.</param>
        /// <param name="teamName">Name of the team he's playing for.</param>
        /// <param name="competitor">Team they are playing against.</param>
        private static void AlertPersonToNewRound(PersonModel person, string teamName, MatchupEntryModel competitor)
        {
            if(person.EmailAddress.Length == 0)
            {
                return;
            }

            string to = person.EmailAddress;
            string subject = "";
            StringBuilder body = new StringBuilder();

            if (competitor != null)
            { 
                subject = $"You have a new matchup with {competitor.TeamCompeting.TeamName}";

                body.AppendLine("<h1>You have a new matchup!</h1>");
                body.Append("<strong>Competitor: </strong>");
                body.Append(competitor.TeamCompeting.TeamName);
                body.AppendLine();
                body.AppendLine();
                body.AppendLine("Have A Great time!");
                body.AppendLine("Tournament Tracker");
            }
            else
            {
                subject = "You have a bye week this round";
                body.AppendLine("Enjoy your round off");
                body.AppendLine("Tournament Tracker");
            }

            EmailLogic.SendEmail(to, subject, body.ToString());
        }

        /// <summary>
        /// Get round that's currently playing on tournament.
        /// </summary>
        /// <param name="tournament">Tournament</param>
        /// <returns></returns>
        private static int CheckCurrentRound(this TournamentModel tournament)
        {
            // Tournament starts with round 1
            int currRound = 1;

            foreach (List<MatchupModel> round in tournament.Rounds)
            {
                if(round.All(x => x.Winner != null))
                {
                    currRound += 1;
                }
                else
                {
                    return currRound;
                }
            }

            // If it gets to here, then
            // Tournament is completed
            CompleteTournament(tournament);

            return currRound - 1;
        }

        /// <summary>
        /// Complete Tournament by sending emails to everyone about winners and prizes they got.
        /// Currently we are handling prizes for only first 2 places 
        /// </summary>
        /// <param name="tournament"></param>
        private static void CompleteTournament(TournamentModel tournament)
        {
            GlobalConfig.Connection.CompleteTournament(tournament);
            TeamModel winners = tournament.Rounds.Last().First().Winner;
            TeamModel loser = tournament.Rounds.Last().First().Entries.Where(x => x.TeamCompeting != winners).First().TeamCompeting;

            decimal winnerPrize = 0;
            decimal loserPrize = 0;

            if (tournament.Prizes.Count > 0)
            {
                decimal totalIncome = tournament.EnteredTeams.Count * tournament.EntryFee;

                PrizeModel firstPlacePrize = tournament.Prizes.Where(x => x.PlaceNumber == 1).FirstOrDefault();
                PrizeModel secondPlacePrize = tournament.Prizes.Where(x => x.PlaceNumber == 2).FirstOrDefault();

                if (firstPlacePrize != null)
                {
                    winnerPrize = firstPlacePrize.CalculatePrizePayout(totalIncome);
                }
                if (secondPlacePrize != null)
                {
                    loserPrize = secondPlacePrize.CalculatePrizePayout(totalIncome - winnerPrize);
                }
            }

            string subject = "";
            StringBuilder body = new StringBuilder();

            subject = $"In {tournament.TournamentName}, {winners.TeamName} has won";

            body.AppendLine("<h1>We have a winner!</h1>");
            body.AppendLine("<p>Congratulations to our winner on the great tournament.</p>");
            body.AppendLine();

            if(winnerPrize > 0)
            {
                body.AppendLine($"<p>{winners.TeamName} will receive ${winnerPrize}</p>");
            }
            if (loserPrize > 0)
            {
                body.AppendLine($"<p>{loser.TeamName} will receive ${loserPrize}</p>");
            }
            body.AppendLine();
            body.AppendLine("<p>Thanks for a great tournament!</p>");
            body.AppendLine("Tournament Tracker");

            List<string> bcc = new List<string>();
            foreach(TeamModel team in tournament.EnteredTeams)
            {
                foreach (PersonModel person in team.TeamMembers)
                {
                    if (person.EmailAddress.Length > 0)
                    {
                        bcc.Add(person.EmailAddress);
                    }
                }
            }

            EmailLogic.SendEmail(new List<string>(), bcc, subject, body.ToString());
            tournament.CompleteTournament();
        }

        /// <summary>
        /// Determine prize pay load
        /// </summary>
        /// <param name="prize"></param>
        /// <param name="totalIncome">Available money for prizes</param>
        /// <returns>Prize amount</returns>
        private static decimal CalculatePrizePayout(this PrizeModel prize, decimal totalIncome)
        {
            decimal output = 0;

            if (prize.PrizeAmount > 0)
            {
                output = prize.PrizeAmount;
            }
            else
            {
                output = Decimal.Multiply(totalIncome, Convert.ToDecimal(prize.PrizePercentage / 100));
            }

            return output;
        }

        /// <summary>
        /// Pass winners to the next round. Set up their new matchups.
        /// </summary>
        /// <param name="matchups">New Winners.</param>
        /// <param name="tournament">Tournament playing.</param>
        private static void AdvanceWinners(List<MatchupModel> matchups, TournamentModel tournament)
        {
            foreach (MatchupModel matchup in matchups)
            {
                foreach (List<MatchupModel> round in tournament.Rounds)
                {
                    foreach (MatchupModel roundMatchup in round)
                    {
                        foreach (MatchupEntryModel matchupEntry in roundMatchup.Entries)
                        {
                            if (matchupEntry.ParentMatchup != null)
                            {
                                if (matchupEntry.ParentMatchup.Id == matchup.Id)
                                {
                                    matchupEntry.TeamCompeting = matchup.Winner;
                                    GlobalConfig.Connection.UpdateMatchup(roundMatchup);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set matchup winner.
        /// </summary>
        /// <param name="matchups">Teams with new scores</param>
        private static void MarkWinnersInMatchups(List<MatchupModel> matchups)
        {
            string greaterWins = ConfigurationManager.AppSettings["greaterWins"];

            foreach (MatchupModel matchup in matchups)
            {
                // Check for bye week entry
                if(matchup.Entries.Count == 1)
                {
                    matchup.Winner = matchup.Entries[0].TeamCompeting;
                    continue;
                }

                // 0 means false, or low score wins 
                if (greaterWins == "0")
                {
                    if (matchup.Entries[0].Score < matchup.Entries[1].Score)
                    {
                        // Team 1 wins because it has lower score
                        matchup.Winner = matchup.Entries[0].TeamCompeting;
                    }
                    else if (matchup.Entries[1].Score < matchup.Entries[0].Score)
                    {
                        // Team 2 wins because it has lower score
                        matchup.Winner = matchup.Entries[1].TeamCompeting;
                    }
                    else
                    {
                        throw new Exception("We do not allow ties in this application.");
                    }
                }
                else
                // 1 means true, or high score wins
                {
                    if (matchup.Entries[0].Score > matchup.Entries[1].Score)
                    {
                        // Team 1 wins because it has lower score
                        matchup.Winner = matchup.Entries[0].TeamCompeting;
                    }
                    else if (matchup.Entries[1].Score > matchup.Entries[0].Score)
                    {
                        // Team 2 wins because it has lower score
                        matchup.Winner = matchup.Entries[1].TeamCompeting;
                    }
                    else
                    {
                        throw new Exception("We do not allow ties in this application.");
                    }
                }
            }
        }

        /// <summary>
        /// Create rounds after first round.
        /// </summary>
        /// <param name="tournament">Tournament information.</param>
        /// <param name="rounds">Number of rounds.</param>
        private static void CreateOtherRounds(TournamentModel tournament, int rounds)
        {
            int round = 2;
            List<MatchupModel> previousRound = tournament.Rounds[0];
            List<MatchupModel> currentRound = new List<MatchupModel>();
            MatchupModel currMatchup = new MatchupModel();

            while(round <= rounds)
            {
                foreach (MatchupModel matchup in previousRound)
                {
                    currMatchup.Entries.Add(new MatchupEntryModel { ParentMatchup = matchup });

                    if(currMatchup.Entries.Count > 1)
                    {
                        currMatchup.MatchupRound = round;
                        currentRound.Add(currMatchup);
                        currMatchup = new MatchupModel(); 
                    }
                }
                tournament.Rounds.Add(currentRound);
                previousRound = currentRound;
                currentRound = new List<MatchupModel>();
                round += 1;
            }
        }

        /// <summary>
        /// Create matchups for first round.
        /// </summary>
        /// <param name="numberOfByes">Number of byes.</param>
        /// <param name="teams">List of entered teams.</param>
        /// <returns></returns>
        private static List<MatchupModel> CreateFirstRound(int numberOfByes, List<TeamModel> teams)
        {
            List<MatchupModel> output = new List<MatchupModel>();
            MatchupModel curr = new MatchupModel();

            foreach (TeamModel team in teams)
            {
                curr.Entries.Add(new MatchupEntryModel { TeamCompeting = team });

                // We are first creating matchups with byes.
                // If number of byes > 0 that means we have to create matchup with bye.
                // If entries cound > 1, that means that matchup is alredy populated with 2 teams so we need to create new matchup
                if(numberOfByes > 0 || curr.Entries.Count > 1)
                {
                    curr.MatchupRound = 1;
                    output.Add(curr);
                    curr = new MatchupModel();

                    if (numberOfByes > 0) numberOfByes--;
                }
            }

            return output;
        }

        /// <summary>
        /// Determine number of matchups with byes
        /// </summary>
        /// <param name="rounds">Number of rounds in tournament.</param>
        /// <param name="numberOfTeams">Number if entered teams in tournament.</param>
        /// <returns>Number of needed byes.</returns>
        private static int NumberOfByes(int rounds, int numberOfTeams)
        {
            int output = 0;

            // Supposed number of team for specified number of rounds. 
            // For 3 rounds it should be 8 teams
            int totalTeams = 1;

            for (int i = 1; i <= rounds; i++)
            {
                totalTeams *= 2;
            }

            output = totalTeams - numberOfTeams;

            return output;
        }

        /// <summary>
        /// Determine number of rounds depending of number of entered teams.
        /// </summary>
        /// <param name="teamCount">Number of teams entered.</param>
        /// <returns></returns>
        private static int FindNumberOfRounds(int teamCount)
        {
            int output = 1;
            int val = 2;

            while(val < teamCount)
            {
                output += 1;
                val *= 2;
            }

            return output;
        }

        /// <summary>
        /// Order teams by newly created guid.
        /// </summary>
        /// <param name="teams">Team that needs ordering.</param>
        /// <returns></returns>
        private static List<TeamModel> RandomizeTeamOrder(List<TeamModel> teams)
        {
            return teams.OrderBy(x => Guid.NewGuid()).ToList();
        }

    }
}
