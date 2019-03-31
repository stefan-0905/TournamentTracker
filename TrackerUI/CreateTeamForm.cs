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
    public partial class CreateTeamForm : Form
    {
        private List<PersonModel> availableTeamMembers = GlobalConfig.Connection.GetPerson_All();
        private List<PersonModel> selectedTeamMembers = new List<PersonModel>();
        private ITeamRequester callingForm;

        public CreateTeamForm(ITeamRequester caller)
        {
            InitializeComponent();

            callingForm = caller;

            WireUpLists();
        }

        private void CreateSampleData()
        {
            availableTeamMembers.Add(new PersonModel { FirstName = "Stef", LastName = "Djordj" });
            availableTeamMembers.Add(new PersonModel { FirstName = "Nix", LastName = "Jox" });
            selectedTeamMembers.Add(new PersonModel { FirstName = "Jane", LastName = "Doe" });
            selectedTeamMembers.Add(new PersonModel { FirstName = "John", LastName = "Smith" });
        }

        private void WireUpLists()
        {
            selectTeamMemberDropDown.DataSource = null;

            selectTeamMemberDropDown.DataSource = availableTeamMembers;
            selectTeamMemberDropDown.DisplayMember = "FullName";

            teamMembersListbox.DataSource = null;

            teamMembersListbox.DataSource = selectedTeamMembers;
            teamMembersListbox.DisplayMember = "FullName";
        }

        private void CreateMemberButton_Click(object sender, EventArgs e)
        {
            if(ValidateForm())
            {
                PersonModel newPerson = new PersonModel()
                {
                    FirstName = firstNameValue.Text,
                    LastName = lastNameValue.Text,
                    EmailAddress = emailValue.Text,
                    CellphoneNumber = cellphoneValue.Text
                };

                GlobalConfig.Connection.CreatePerson(newPerson);

                selectedTeamMembers.Add(newPerson);
                WireUpLists();

                firstNameValue.Text = "";
                lastNameValue.Text = "";
                emailValue.Text = "";
                cellphoneValue.Text = "";
            }
            else
            {
                MessageBox.Show("You need to fill in all of the fields.");
            }
        }

        /// <summary>
        /// Check if all fields are filled.
        /// </summary>
        /// <returns>Validation success (true)</returns>
        private bool ValidateForm()
        {
            if (firstNameValue.Text.Length == 0) return false;
            if (lastNameValue.Text.Length == 0) return false;
            if (emailValue.Text.Length == 0) return false;
            if (cellphoneValue.Text.Length == 0) return false;

            return true;
        }

        /// <summary>
        /// Add member from dropdown to list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddMemberButton_Click(object sender, EventArgs e)
        {
            PersonModel person = (PersonModel)selectTeamMemberDropDown.SelectedItem;

            if (person != null && !selectedTeamMembers.Contains(person))
            {
                availableTeamMembers.Remove(person);
                selectedTeamMembers.Add(person);
            }

            WireUpLists();
        }

        /// <summary>
        /// Remove member from list and add it to dropdown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveSelectedMemberButton_Click(object sender, EventArgs e)
        {
            PersonModel p = (PersonModel)teamMembersListbox.SelectedItem;

            if(p != null)
            {
                selectedTeamMembers.Remove(p);
                availableTeamMembers.Add(p);
                WireUpLists();
            }
        }

        private void CreateTeamButton_Click(object sender, EventArgs e)
        {
            TeamModel myTeam = new TeamModel();
            myTeam.TeamName = teamNameValue.Text;
            myTeam.TeamMembers = selectedTeamMembers;

            GlobalConfig.Connection.CreateTeam(myTeam);

            // Send team to CreateTournamentForm
            callingForm.TeamComplete(myTeam);

            this.Close();
        }
    }
}
