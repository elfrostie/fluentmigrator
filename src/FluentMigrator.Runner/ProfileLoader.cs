using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentMigrator.Infrastructure.Extensions;
using FluentMigrator.Runner.Initialization;

namespace FluentMigrator.Runner
{
	public class ProfileLoader : IProfileLoader
	{
		public ProfileLoader(IRunnerContext runnerContext, IMigrationRunner runner, IMigrationConventions conventions, bool isBeforeProfile)
		{
			Runner = runner;
			Assembly = runner.MigrationAssembly;
			Profile = isBeforeProfile ? runnerContext.BeforeProfile : runnerContext.Profile;
			Conventions = conventions;

			Initialize();
		}

		private Assembly Assembly { get; set; }
		private string Profile { get; set; }
		protected IMigrationConventions Conventions { get; set; }
		private IMigrationRunner Runner { get; set; }

		private IEnumerable<IMigration> _profiles;

		private void Initialize()
		{
			_profiles = new List<IMigration>();

			if (!string.IsNullOrEmpty(Profile))
				_profiles = FindProfilesIn(Assembly, Profile);
		}

		public IEnumerable<IMigration> FindProfilesIn(Assembly assembly, string profileString)
		{
            List<Type> matchedTypes = new List<Type>();

            string[] profileList = profileString.ToLower().Split(',');
            foreach (var profile in profileList)
            {
                matchedTypes.AddRange(assembly.GetExportedTypes()
                     .Where(t => Conventions.TypeIsProfile(t) && t.GetOneAttribute<ProfileAttribute>().ProfileName.ToLower() == profile.Trim()));
            }

			foreach (Type type in matchedTypes)
			{
				yield return type.Assembly.CreateInstance(type.FullName) as IMigration;
			}
		}

		public IEnumerable<IMigration> Profiles
		{
			get
			{
				return _profiles;
			}
		}

		public void ApplyProfiles()
		{
			// Run Profile if applicable
			foreach (var profile in Profiles)
			{
				Runner.Up(profile);
			}
		}
	}

}