using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TrackerLibrary;
using TrackerLibrary.Models;

namespace TrackerUI
{
    public partial class CreateTournamentForm : Form, IPrizeRequester, ITeamRequester
    {
        List<TeamModel> availableTeams = GlobalConfig.Connection.GetTeam_All();
        List<TeamModel> selectedTeams = new List<TeamModel>();
        List<PrizeModel> selectedPrizes = new List<PrizeModel>();
        public CreateTournamentForm()
        {
            InitializeComponent();
            WireUpLists();
        }

        private void WireUpLists()
        {
            selectTeamDropDown.DataSource = null;
            selectTeamDropDown.DataSource = availableTeams;
            selectTeamDropDown.DisplayMember = "TeamName";

            tournamentTeamsListbox.DataSource = null;
            tournamentTeamsListbox.DataSource = selectedTeams;
            tournamentTeamsListbox.DisplayMember = "TeamName";

            prizesListbox.DataSource = null;
            prizesListbox.DataSource = selectedPrizes;
            prizesListbox.DisplayMember = "PlaceName";
        }

        private void AddTeamButton_Click(object sender, EventArgs e)
        {
            TeamModel team = (TeamModel)selectTeamDropDown.SelectedItem;
            
            if(team != null)
            {
                selectedTeams.Add(team);
                availableTeams.Remove(team);

                WireUpLists();
            }

        }

        private void CreatePrizeButton_Click(object sender, EventArgs e)
        {
            // Call the CreatePrizeForm
            CreatePrizeForm prizeForm = new CreatePrizeForm(this);
            prizeForm.Show();
        }

        public void PrizeComplete(PrizeModel model)
        {
            // Get back from the form new Prize
            // Take the prize and put it into our list of selected prizes
            selectedPrizes.Add(model);
            WireUpLists();
        }

        public void TeamComplete(TeamModel model)
        {
            // Get back from the form new Team
            // Take the team and put it into our list of selected teams
            selectedTeams.Add(model);
            WireUpLists();
        }

        private void CreateNewTeamLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CreateTeamForm teamForm = new CreateTeamForm(this);
            teamForm.Show();
        }

        private void DeleteSelectedPrizeButton_Click(object sender, EventArgs e)
        {
            PrizeModel prize = (PrizeModel)prizesListbox.SelectedItem;

            if(prize != null)
            {
                selectedPrizes.Remove(prize);

                WireUpLists();
            }
        }

        private void RemoveSelectedTeamButton_Click(object sender, EventArgs e)
        {
            TeamModel team = (TeamModel)tournamentTeamsListbox.SelectedItem;

            if (team != null)
            {
                availableTeams.Add(team);
                selectedTeams.Remove(team);

                WireUpLists();
            }
        }

        private void CreateTournamentButton_Click(object sender, EventArgs e)
        {
            // Validate Data
            bool feeAcceptable = decimal.TryParse(entryFeeValue.Text, out decimal fee);

            if(!feeAcceptable)
            {
                MessageBox.Show("You need to enter a valid entry fee", "Invalid fee", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Create tournament model
            TournamentModel myTournament = new TournamentModel();

            myTournament.TournamentName = tournamentNameValue.Text;
            myTournament.EntryFee = fee;
            myTournament.Prizes = selectedPrizes;
            myTournament.EnteredTeams = selectedTeams;

            TournamentLogic.CreateRounds(myTournament);

            // Create Tournaments entry
            // Create all of the prizes entries relations
            // Create all of the team entries relations
            GlobalConfig.Connection.CreateTournament(myTournament);

            // Alret users by sending them mail and informing of new round start
            myTournament.AlertUsersToNewRound();

            TournamentViewer tvf = new TournamentViewer(myTournament);
            tvf.Show();
            this.Close();
        }
    }
}
