# cg-bot

cg-bot is a Discord server bot that has functionality for Soundpad control (the soundboard program available on Steam), and Call of Duty statistic tracking (Black Ops Cold War, and Modern Warfare).

The latest master branch is a .NET Core application.

## Commands!

### Help: Help Commands

- **'.help'** to display information on all commands.

- **'.help [subject]'** to display information on all commands for a specific subject.

- **'.subjects'** to display the existing command subjects.

### Help: Soundboard Commands

- **'.add [YouTube video URL] [sound name]'** to add a YouTube to MP3 sound to the soundboard in the specified category.
The bot will then ask you to select a category to add the sound to.

- **'.approve [user]'** to approve a user's existing request to add to the soundboard if you are an administrator.

- **'.categories'** to display all categories.

- **'.delete [sound number]'** to delete the sound with the corresponding number from the soundboard if you are an administrator.

- **'.deny [user]'** to deny a user's existing request to add to the soundboard if you are an administrator.

- **'.pause'** to pause/resume the sound currently playing.

- **'.play [sound number]'** to play the sound with the corresponding number.

- **'.sounds'** to display all categories and their playable sounds.
The bot will then ask you to play a sound by entering the corresponding number.

- **'.sounds [category name]'** to display all playable sounds in the specified category.
The bot will then ask you to play a sound by entering the corresponding number.

- **'.stop'** to stop the sound currently playing.

### Help: Modern Warfare Commands

- **'.mw participants'** to list out the Call of Duty accounts participating in the Modern Warfare services.

- **'.mw add participant [user]'** to add an account to the list of Call of Duty accounts participating in the Modern Warfare services.
The bot will then ask you to enter the account name, tag, and platform.

- **'.mw rm participant [user]'** to remove an account from the list of Call of Duty accounts participating in the Modern Warfare services.

- **'.mw wz lifetime kills'** to display the lifetime total game kills (Modern Warfare + Warzone) of all participating Modern Warfare players from highest to lowest.

- **'.mw wz weekly kills'** to display the total game kills (Modern Warfare + Warzone) so far this week of all participating players from highest to lowest.
The bot will only assign the @__ role for Modern Warfare kills to the player in first place at the end of the week with the most multiplayer kills (not Warzone).

### Help: Warzone Commands

- **'.wz wins'** to display the total Warzone wins of all participating players from highest to lowest.
The bot will only assign the @__ role for Warzone wins to the player in first place at the end of the week.
The bot will only assign the @__ role for Warzone kills to the player in first place at the end of the week with the most Warzone kills (not multiplayer).

### Help: Black Ops Cold War Commands

- **'.bocw participants'** to list out the Call of Duty accounts participating in the Black Ops Cold War services.

- **'.bocw add participant [user]'** to add an account to the list of Call of Duty accounts participating in the Black Ops Cold War services.
The bot will then ask you to enter the account name, tag, and platform.

- **'.bocw rm participant [user]'** to remove an account from the list of Call of Duty accounts participating in the Black Ops Cold War services.

- **'.bocw lifetime kills'** to display the lifetime total game kills of all participating players from highest to lowest.

- **'.bocw weekly kills'** to display the total game kills so far this week of all participating players from highest to lowest.
The bot will only assign the @__ role to the player in first place at the end of the week.
