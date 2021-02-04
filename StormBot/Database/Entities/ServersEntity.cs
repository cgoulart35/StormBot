using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StormBot.Database.Entities
{
	[Table("ServersEntity")]
	public class ServersEntity
	{
        [Key]
        public ulong ServerID { get; set; }

        public string ServerName { get; set; }

        public string PrefixUsed { get; set; }

		#region FLAGS
		public bool AllowServerPermissionBlackOpsColdWarTracking { get; set; } // dev use only (access in database)
		public bool ToggleBlackOpsColdWarTracking { get; set; }

		public bool AllowServerPermissionModernWarfareTracking { get; set; } // dev use only (access in database)
		public bool ToggleModernWarfareTracking { get; set; }

		public bool AllowServerPermissionWarzoneTracking { get; set; } // dev use only (access in database)
		public bool ToggleWarzoneTracking { get; set; }

		public bool AllowServerPermissionSoundpadCommands { get; set; } // dev use only (access in database)
		public bool ToggleSoundpadCommands { get; set; }
		#endregion

		#region CHANNELS
		public ulong CallOfDutyNotificationChannelID { get; set; }

		public ulong SoundboardNotificationChannelID { get; set; }
		#endregion

		#region ROLES

		public ulong AdminRoleID { get; set; }

		public ulong WarzoneWinsRoleID { get; set; }

		public ulong WarzoneKillsRoleID { get; set; }

		public ulong ModernWarfareKillsRoleID { get; set; }

		public ulong BlackOpsColdWarKillsRoleID { get; set; }
		#endregion
	}
}
