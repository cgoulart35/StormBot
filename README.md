# StormBot 2.0

StormBot is a Discord server bot that has functionality for Soundpad control (the soundboard program available on Steam), and Call of Duty statistic tracking (Black Ops Cold War, Modern Warfare, and Warzone).

The latest version 2.0 brings multi-server support, uses Entity Framework Core, and is a .NET Core application.

Future development is planned to add support for web API requests for remote Soundpad control hosted on another device (Raspberry Pi). This feature will enable the use of the Soundpad service without running the bot on the same device as the Soundpad application; instead, host one instance of the bot 24/7 on a Raspberry Pi. To use the Soundpad service as of today, the bot or a second instance of the bot needs to be running on the same machine to detect the Soundpad application.

## Commands!

### Help: Help Commands

- **'.help'** to display information on all commands.

- **'.help [subject]'** to display information on all commands for a specific subject.

- **'.subjects'** to display the existing command subjects.

### Help: Config Commands

- **'.config all'** to display all current set configurations if you are a StormBot administrator.

- **'.config prefix [prefix]'** to set the server's bot command prefix if you are a StormBot administrator.

- **'.config toggle bocw'** to enable/disable Black Ops Cold War commands and stat tracking on the server if you are a StormBot administrator.
Warning: If disabled, then re-enabled after a weekly data fetch, daily tracking for Black Ops Cold War participants will resume after the next weekly data fetch (Sundays, 1:00 AM EST).

- **'.config toggle mw'** to enable/disable Modern Warfare commands and stat tracking on the server if you are a StormBot administrator.
Warning: If disabled, then re-enabled after a weekly data fetch, daily tracking for Modern Warfare participants will resume after the next weekly data fetch (Sundays, 1:00 AM EST).

- **'.config toggle wz'** to enable/disable Warzone commands and stat tracking on the server if you are a StormBot administrator.
Warning: If disabled, then re-enabled after a weekly data fetch, daily tracking for Warzone participants will resume after the next weekly data fetch (Sundays, 1:00 AM EST).

- **'.config toggle sb'** to enable/disable Soundpad commands on the server if you are a StormBot administrator.

- **'.config channel cod [channel]'** to set the server's channel for Call of Duty notifications if you are a StormBot administrator.

- **'.config channel sb [channel]'** to set the server's channel for Soundboard notifications if you are a StormBot administrator.

- **'.config role admin [role]'** to set the server's admin role for special commands and configuration if you are a StormBot administrator.

- **'.config role bocw kills [role]'** to set the server's role for the most weekly Black Ops Cold War kills if you are a StormBot administrator.

- **'.config role mw kills [role]'** to set the server's role for the most weekly Modern Warfare kills if you are a StormBot administrator.

- **'.config role wz wins [role]'** to set the server's role for the most Warzone wins if you are a StormBot administrator.

- **'.config role wz kills [role]'** to set the server's role for the most weekly Warzone kills if you are a StormBot administrator.

### Help: Soundboard Commands

- **'.add [YouTube video URL] [sound name]'** to add a YouTube to MP3 sound to the soundboard in the specified category.
The bot will then ask you to select a category to add the sound to.

- **'.approve [user]'** to approve a user's existing request to add to the soundboard if you are a StormBot administrator.

- **'.categories'** to display all categories.

- **'.delete [sound number]'** to delete the sound with the corresponding number from the soundboard if you are a StormBot administrator.

- **'.deny [user]'** to deny a user's existing request to add to the soundboard if you are a StormBot administrator.

- **'.pause'** to pause/resume the sound currently playing.

- **'.play [sound number]'** to play the sound with the corresponding number.

- **'.sounds'** to display all categories and their playable sounds.
The bot will then ask you to play a sound by entering the corresponding number.

- **'.sounds [category name]'** to display all playable sounds in the specified category.
The bot will then ask you to play a sound by entering the corresponding number.

- **'.stop'** to stop the sound currently playing.

### Help: Modern Warfare Commands

- **'.mw participate'** to add your account to the list of Call of Duty accounts participating in the Modern Warfare services.
The bot will then ask you to enter the account name, tag, and platform.

- **'.mw leave'** to remove your account from the list of Call of Duty accounts participating in the Modern Warfare services.

- **'.mw participants'** to list out the Call of Duty accounts participating in the Modern Warfare services if you are a StormBot administrator.

- **'.mw add participant [user]'** to add an account to the list of Call of Duty accounts participating in the Modern Warfare services if you are a StormBot administrator.
The bot will then ask you to enter the account name, tag, and platform.

- **'.mw rm participant [user]'** to remove an account from the list of Call of Duty accounts participating in the Modern Warfare services if you are a StormBot administrator.

- **'.mw wz lifetime kills'** to display the lifetime total game kills (Modern Warfare + Warzone) of all participating Modern Warfare players from highest to lowest if you are a StormBot administrator.

- **'.mw wz weekly kills'** to display the total game kills (Modern Warfare + Warzone) so far this week of all participating Modern Warfare players from highest to lowest if you are a StormBot administrator.
The bot will only assign the @__ role for Modern Warfare kills to the player in first place at the end of the week with the most multiplayer kills (not Warzone).

### Help: Warzone Commands

- **'.wz participate'** to add your account to the list of Call of Duty accounts participating in the Warzone services.
The bot will then ask you to enter the account name, tag, and platform.

- **'.wz leave'** to remove your account from the list of Call of Duty accounts participating in the Warzone services.

- **'.wz participants'** to list out the Call of Duty accounts participating in the Warzone services if you are a StormBot administrator.

- **'.wz add participant [user]'** to add an account to the list of Call of Duty accounts participating in the Warzone services if you are a StormBot administrator.
The bot will then ask you to enter the account name, tag, and platform.

- **'.wz rm participant [user]'** to remove an account from the list of Call of Duty accounts participating in the Warzone services if you are a StormBot administrator.

- **'.wz lifetime wins'** to display the lifetime total Warzone wins of all participating players from highest to lowest if you are a StormBot administrator.

- **'.wz weekly wins'** to display the total Warzone wins so far this week of all participating players from highest to lowest if you are a StormBot administrator.
The bot will only assign the @__ role for Warzone wins to the player in first place at the end of the week with the most Warzone wins.
The bot will only assign the @__ role for Warzone kills to the player in first place at the end of the week with the most Warzone kills (not multiplayer).

### Help: Black Ops Cold War Commands

- **'.bocw participate'** to add your account to the list of Call of Duty accounts participating in the Black Ops Cold War services.
The bot will then ask you to enter the account name, tag, and platform.

- **'.bocw leave'** to remove your account from the list of Call of Duty accounts participating in the Black Ops Cold War services.

- **'.bocw participants'** to list out the Call of Duty accounts participating in the Black Ops Cold War services if you are a StormBot administrator.

- **'.bocw add participant [user]'** to add an account to the list of Call of Duty accounts participating in the Black Ops Cold War services if you are a StormBot administrator.
The bot will then ask you to enter the account name, tag, and platform.

- **'.bocw rm participant [user]'** to remove an account from the list of Call of Duty accounts participating in the Black Ops Cold War services if you are a StormBot administrator.

- **'.bocw lifetime kills'** to display the lifetime total game kills of all participating Black Ops Cold War players from highest to lowest if you are a StormBot administrator.

- **'.bocw weekly kills'** to display the total game kills so far this week of all participating Black Ops Cold War players from highest to lowest if you are a StormBot administrator.
The bot will only assign the @__ role to the player in first place at the end of the week.
