# Torunament Tracker
Application that handles organization of a tournament. Practice project from free version of tutorial offered by IAmTimCorey youtube channel. 

## Prerequisits  
First of all you should have MS SQL Server for working with database. Next, you should import Data-Tier Application with SSMS. That will load our database structure and set you all up.  
Then, you need to edit `App.config` file and change you app keys. FilePath represents path to directory that will hold you text files if you choose to save data in that format. GreatedWins provides you with possibility to change when will one team win, value 1, if it has greater score, or 2, if it has lower. Also there are options for you email information that you should change.  
When dealing with emails, i used Papercut from CodePlex for testing. Change mailSettings if you prefer some other application.
